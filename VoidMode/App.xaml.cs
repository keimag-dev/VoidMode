using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using NAudio.CoreAudioApi;

namespace VoidMode
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppLogger.Initialize();
            RegisterGlobalExceptionLogging();

            var options = StartupOptions.Parse(e.Args);
            AppLogger.Info($"Startup args: {string.Join(" ", e.Args)}");

            if (options.SelfTest)
            {
                Shutdown(RunSelfTest());
                return;
            }

            var config = ConfigManager.Load();
            if (options.DebugSafeMode)
            {
                config = CreateSafeModeConfig(config);
            }

            if (options.StartDirectly)
            {
                ShowVoidMode(config, options.DebugAutoExitSeconds);
                return;
            }

            var settings = new SettingsWindow(config);
            MainWindow = settings;
            settings.Show();
        }

        private static AppConfig CreateSafeModeConfig(AppConfig source)
        {
            AppLogger.Info("Debug safe mode enabled. Disabling app launch, audio mute, display-off, and auto-kill for this run.");
            return new AppConfig
            {
                AppPaths = new List<string>(),
                EnableBlackScreen = source.EnableBlackScreen,
                EnableDisplayOff = false,
                EnableMute = false,
                EnableSleepPrevention = false,
                EnableAutoKill = false
            };
        }

        private void ShowVoidMode(AppConfig config, int? debugAutoExitSeconds = null)
        {
            AppLogger.Info("Starting VoidMode directly from command line.");
            var voidWindow = new VoidModeWindow(config, debugAutoExitSeconds);
            MainWindow = voidWindow;
            voidWindow.Show();
        }

        private static int RunSelfTest()
        {
            try
            {
                AppLogger.Info("Running self-test.");
                AppLogger.Info($"Executable base directory: {AppContext.BaseDirectory}");
                AppLogger.Info($"Expected user config path: {AppPaths.ConfigPath}");

                var config = ConfigManager.Load();
                ConfigManager.Save(config);
                ProbeAudioEndpoint();
                ProbePowerRequest();

                AppLogger.Info("Self-test completed successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Self-test failed.", ex);
                return 1;
            }
        }

        private static void ProbeAudioEndpoint()
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                AppLogger.Info($"Audio endpoint detected: {device.FriendlyName}, volume={device.AudioEndpointVolume.MasterVolumeLevelScalar:0.00}");
            }
            catch (Exception audioEx)
            {
                AppLogger.Error("Audio endpoint probe failed during self-test.", audioEx);
            }
        }

        private static void ProbePowerRequest()
        {
            using var powerRequestManager = new PowerRequestManager();
            powerRequestManager.Enable();
            powerRequestManager.Disable();
        }

        private void RegisterGlobalExceptionLogging()
        {
            DispatcherUnhandledException += (_, e) =>
            {
                AppLogger.Error("Unhandled dispatcher exception.", e.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    AppLogger.Error("Unhandled app domain exception.", ex);
                }
                else
                {
                    AppLogger.Error($"Unhandled app domain exception object: {e.ExceptionObject}");
                }
            };
        }
    }
}
