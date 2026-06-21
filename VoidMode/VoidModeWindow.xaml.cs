using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;

namespace VoidMode
{
    public partial class VoidModeWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_SYSCOMMAND = 0x0112;
        private const int SC_MONITORPOWER = 0xF170;
        private const int MONITOR_OFF = 2;

        private AppConfig _config;
        private List<Process> _startedProcesses = new List<Process>();
        private float _originalVolume = 1.0f;
        private bool _isUserInteracting = false;
        private DispatcherTimer _focusTimer;
        private DispatcherTimer _inputTimer;

        public VoidModeWindow()
        {
            InitializeComponent();
            _config = ConfigManager.Load();

            this.Closing += (s, e) => {
                if (!_isUserInteracting) e.Cancel = true;
            };

            SetupFocusTimer();
            SetupInputTimer();

            this.Loaded += (s, e) => StartVoidMode();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            this.Topmost = false;
            this.Topmost = true;
        }

        private void SetupFocusTimer()
        {
            _focusTimer = new DispatcherTimer();
            _focusTimer.Interval = TimeSpan.FromMilliseconds(500);
            _focusTimer.Tick += (s, e) => {
                this.Activate();
                this.Topmost = true;
            };
            _focusTimer.Start();
        }

        private void SetupInputTimer()
        {
            _inputTimer = new DispatcherTimer();
            _inputTimer.Interval = TimeSpan.FromMilliseconds(200);
            _inputTimer.Tick += (s, e) => {
                if (Mouse.LeftButton == MouseButtonState.Pressed || Keyboard.IsKeyDown(Key.Escape))
                {
                    ShowReturnMessage();
                }
            };
            _inputTimer.Start();
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
            _focusTimer.Stop();
            _inputTimer.Stop();

            if (_config.EnableMute)
            {
                RestoreVolume();
            }

            if (_config.EnableAutoKill)
            {
                foreach (var p in _startedProcesses)
                {
                    try { p.Kill(); } catch { }
                }
            }

            Application.Current.Shutdown();
        }

        private async void StartVoidMode()
        {
            // UIスレッドをブロックしてクラッシュさせるのを防ぐため、
            // OSへの重いリクエストはすべてバックグラウンドスレッドで実行する
            await Task.Run(() =>
            {
                try
                {
                    // 1. Start Apps
                    foreach (var path in _config.AppPaths)
                    {
                        try {
                            var proc = Process.Start(path);
                            if (proc != null)
                            {
                                lock(_startedProcesses) {
                                    _startedProcesses.Add(proc);
                                }
                            }
                        } catch (Exception ex) {
                            Debug.WriteLine($"Failed to start {path}: {ex.Message}");
                        }
                    }

                    // 2. Mute Audio
                    if (_config.EnableMute)
                    {
                        MuteAudio();
                    }

                    // 3. Display Off
                    if (_config.EnableDisplayOff)
                    {
                        SendMessage((IntPtr)0xffff, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MONITOR_OFF);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Critical error in StartVoidMode: {ex.Message}");
                }
            });
        }

        private void MuteAudio()
        {
            try {
                var device = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                _originalVolume = device.AudioEndpointVolume.MasterVolumeLevelScalar;
                device.AudioEndpointVolume.MasterVolumeLevelScalar = 0;
            } catch { }
        }

        private void RestoreVolume()
        {
            try {
                var device = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                device.AudioEndpointVolume.MasterVolumeLevelScalar = _originalVolume;
            } catch { }
        }
    }
}
