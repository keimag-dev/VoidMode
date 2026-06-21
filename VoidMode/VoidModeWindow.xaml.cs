using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NAudio.CoreAudioApi;

namespace VoidMode
{
    public partial class VoidModeWindow : Window
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);

        private const uint WM_SYSCOMMAND = 0x0112;
        private const int SC_MONITORPOWER = 0xF170;
        private const int MONITOR_OFF = 2;
        private const int MONITOR_ON = -1;
        private const uint SMTO_ABORTIFHUNG = 0x0002;
        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);

        private readonly AppConfig _config;
        private readonly int? _debugAutoExitSeconds;
        private readonly List<Process> _startedProcesses = new List<Process>();
        private readonly object _processLock = new object();
        private float? _originalVolume;
        private bool _audioMuted;
        private bool _hasExited;
        private bool _isExiting;
        private bool _isUserInteracting;
        private DispatcherTimer? _focusTimer;
        private DispatcherTimer? _inputTimer;
        private DispatcherTimer? _debugAutoExitTimer;

        public VoidModeWindow() : this(ConfigManager.Load())
        {
        }

        public VoidModeWindow(AppConfig config, int? debugAutoExitSeconds = null)
        {
            InitializeComponent();
            _config = config;
            _config.Normalize();
            _debugAutoExitSeconds = debugAutoExitSeconds;

            Closing += (s, e) =>
            {
                if (!_isUserInteracting && !_hasExited)
                {
                    e.Cancel = true;
                }
            };

            SetupFocusTimer();
            SetupInputTimer();
            SetupDebugAutoExitTimer();

            Loaded += async (s, e) => await StartVoidModeAsync();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            Topmost = false;
            Topmost = true;
        }

        private void SetupFocusTimer()
        {
            _focusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _focusTimer.Tick += (s, e) =>
            {
                Activate();
                Topmost = true;
            };
            _focusTimer.Start();
        }

        private void SetupInputTimer()
        {
            _inputTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _inputTimer.Tick += (s, e) =>
            {
                if (Mouse.LeftButton == MouseButtonState.Pressed || Keyboard.IsKeyDown(Key.Escape))
                {
                    ShowReturnMessage();
                }
            };
            _inputTimer.Start();
        }

        private void SetupDebugAutoExitTimer()
        {
            if (_debugAutoExitSeconds is not > 0)
            {
                return;
            }

            AppLogger.Info($"Debug auto-exit enabled: {_debugAutoExitSeconds.Value} seconds.");
            _debugAutoExitTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_debugAutoExitSeconds.Value)
            };
            _debugAutoExitTimer.Tick += (s, e) =>
            {
                AppLogger.Info("Debug auto-exit timer elapsed. Exiting VoidMode.");
                _isUserInteracting = true;
                ExitVoidMode();
            };
            _debugAutoExitTimer.Start();
        }

        private void ShowReturnMessage()
        {
            _isUserInteracting = true;
            TxtMessage.Visibility = Visibility.Visible;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                HandleEscape();
            }
            base.OnKeyDown(e);
        }

        private void HandleEscape()
        {
            var result = MessageBox.Show("通常モードに復帰しますか？", "VoidMode", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _isUserInteracting = true;
                ExitVoidMode();
            }
            else
            {
                _isUserInteracting = false;
                TxtMessage.Visibility = Visibility.Collapsed;
            }
        }

        private async void ExitVoidMode()
        {
            if (_hasExited)
            {
                return;
            }

            _hasExited = true;
            _isExiting = true;
            _isUserInteracting = true;
            AppLogger.Info("Exiting VoidMode.");
            StopTimers();

            await Task.Run(() =>
            {
                if (_config.EnableDisplayOff)
                {
                    TurnDisplayOn();
                }

                if (_config.EnableMute)
                {
                    RestoreVolumeWithRetry();
                }

                if (_config.EnableAutoKill)
                {
                    KillStartedProcesses();
                }
            });

            Application.Current.Shutdown();
        }

        private void StopTimers()
        {
            _focusTimer?.Stop();
            _inputTimer?.Stop();
            _debugAutoExitTimer?.Stop();
        }

        private async Task StartVoidModeAsync()
        {
            AppLogger.Info("Entering VoidMode.");

            try
            {
                await Task.Run(() =>
                {
                    if (_isExiting)
                    {
                        return;
                    }

                    StartConfiguredApplications();

                    if (_isExiting)
                    {
                        return;
                    }

                    if (_config.EnableMute)
                    {
                        MuteAudio();
                    }

                    if (_isExiting)
                    {
                        return;
                    }

                    if (_config.EnableDisplayOff)
                    {
                        TurnDisplayOff();
                    }
                });

                if (!_isExiting)
                {
                    AppLogger.Info("VoidMode startup actions completed.");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("VoidMode startup actions failed.", ex);
            }
        }

        private void StartConfiguredApplications()
        {
            foreach (var path in _config.AppPaths)
            {
                if (_isExiting)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                try
                {
                    AppLogger.Info($"Starting configured app: {path}");
                    var process = Process.Start(path);
                    if (process != null)
                    {
                        lock (_processLock)
                        {
                            _startedProcesses.Add(process);
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"Failed to start configured app: {path}", ex);
                }
            }
        }

        private void KillStartedProcesses()
        {
            List<Process> processesToKill;
            lock (_processLock)
            {
                processesToKill = new List<Process>(_startedProcesses);
                _startedProcesses.Clear();
            }

            foreach (var process in processesToKill)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        AppLogger.Info($"Closing process: {GetProcessDescription(process)}");
                        if (!process.CloseMainWindow() || !process.WaitForExit(3000))
                        {
                            AppLogger.Info($"Killing process tree: {GetProcessDescription(process)}");
                            process.Kill(entireProcessTree: true);
                            process.WaitForExit(3000);
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Failed to stop started process.", ex);
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        private static string GetProcessDescription(Process process)
        {
            try
            {
                return $"{process.ProcessName} ({process.Id})";
            }
            catch
            {
                return "unknown process";
            }
        }

        private void MuteAudio()
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                _originalVolume = device.AudioEndpointVolume.MasterVolumeLevelScalar;
                device.AudioEndpointVolume.MasterVolumeLevelScalar = 0;
                _audioMuted = true;
                AppLogger.Info($"Muted audio endpoint. Previous volume={_originalVolume:0.00}");
            }
            catch (Exception ex)
            {
                _audioMuted = false;
                _originalVolume = null;
                AppLogger.Error("Failed to mute audio.", ex);
            }
        }

        private void RestoreVolumeWithRetry()
        {
            if (!_audioMuted || !_originalVolume.HasValue)
            {
                AppLogger.Info("Skipping audio restore because mute did not complete successfully.");
                return;
            }

            const int maxAttempts = 10;
            var targetVolume = _originalVolume.Value;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using var enumerator = new MMDeviceEnumerator();
                    using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    device.AudioEndpointVolume.MasterVolumeLevelScalar = targetVolume;
                    _audioMuted = false;
                    AppLogger.Info($"Restored audio volume to {targetVolume:0.00} on attempt {attempt}.");
                    return;
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"Failed to restore audio volume on attempt {attempt}/{maxAttempts}.", ex);
                    if (attempt < maxAttempts)
                    {
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }
        }

        private void TurnDisplayOn()
        {
            SendMonitorPowerMessage(MONITOR_ON, "power-on");
            System.Threading.Thread.Sleep(500);
        }

        private void TurnDisplayOff()
        {
            SendMonitorPowerMessage(MONITOR_OFF, "power-off");
        }

        private void SendMonitorPowerMessage(int monitorState, string action)
        {
            try
            {
                AppLogger.Info($"Sending monitor {action} message.");
                var result = SendMessageTimeout(
                    HWND_BROADCAST,
                    WM_SYSCOMMAND,
                    (IntPtr)SC_MONITORPOWER,
                    (IntPtr)monitorState,
                    SMTO_ABORTIFHUNG,
                    1000,
                    out _);

                AppLogger.Info($"Monitor {action} message result: {result}");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to send monitor {action} message.", ex);
            }
        }
    }
}
