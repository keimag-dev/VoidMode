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
        private float _originalVolume = 1.0f;
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
            _debugAutoExitSeconds = debugAutoExitSeconds;

            Closing += (s, e) =>
            {
                if (!_isUserInteracting)
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

        private void ExitVoidMode()
        {
            AppLogger.Info("Exiting VoidMode.");
            _focusTimer?.Stop();
            _inputTimer?.Stop();
            _debugAutoExitTimer?.Stop();

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
                List<Process> processesToKill;
                lock (_processLock)
                {
                    processesToKill = new List<Process>(_startedProcesses);
                }

                foreach (var process in processesToKill)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            AppLogger.Info($"Killing process: {process.ProcessName} ({process.Id})");
                            process.Kill();
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error("Failed to kill started process.", ex);
                    }
                }
            }

            Application.Current.Shutdown();
        }

        private async Task StartVoidModeAsync()
        {
            AppLogger.Info("Entering VoidMode.");

            // Root cause of the installer crash was config.json being written under Program Files.
            // This remains asynchronous only to keep the WPF UI responsive while starting apps,
            // talking to the audio endpoint, and broadcasting monitor power messages.
            await Task.Run(() =>
            {
                StartConfiguredApplications();

                if (_config.EnableMute)
                {
                    MuteAudio();
                }

                if (_config.EnableDisplayOff)
                {
                    TurnDisplayOff();
                }
            });

            AppLogger.Info("VoidMode startup actions completed.");
        }

        private void StartConfiguredApplications()
        {
            foreach (var path in _config.AppPaths)
            {
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

        private void MuteAudio()
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                _originalVolume = device.AudioEndpointVolume.MasterVolumeLevelScalar;
                device.AudioEndpointVolume.MasterVolumeLevelScalar = 0;
                AppLogger.Info($"Muted audio endpoint. Previous volume={_originalVolume:0.00}");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Failed to mute audio.", ex);
            }
        }

        private void RestoreVolumeWithRetry()
        {
            const int maxAttempts = 10;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using var enumerator = new MMDeviceEnumerator();
                    var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    device.AudioEndpointVolume.MasterVolumeLevelScalar = _originalVolume;
                    AppLogger.Info($"Restored audio volume to {_originalVolume:0.00} on attempt {attempt}.");
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
            try
            {
                AppLogger.Info("Sending monitor power-on message before restore.");
                var result = SendMessageTimeout(
                    HWND_BROADCAST,
                    WM_SYSCOMMAND,
                    (IntPtr)SC_MONITORPOWER,
                    (IntPtr)MONITOR_ON,
                    SMTO_ABORTIFHUNG,
                    1000,
                    out _);

                AppLogger.Info($"Monitor power-on message result: {result}");
                System.Threading.Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Failed to turn display on.", ex);
            }
        }

        private void TurnDisplayOff()
        {
            try
            {
                AppLogger.Info("Sending monitor power-off message.");
                var result = SendMessageTimeout(
                    HWND_BROADCAST,
                    WM_SYSCOMMAND,
                    (IntPtr)SC_MONITORPOWER,
                    (IntPtr)MONITOR_OFF,
                    SMTO_ABORTIFHUNG,
                    1000,
                    out _);

                AppLogger.Info($"Monitor power-off message result: {result}");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Failed to turn display off.", ex);
            }
        }
    }
}
