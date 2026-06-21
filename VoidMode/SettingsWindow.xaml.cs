using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;

namespace VoidMode
{
    public partial class SettingsWindow : Window
    {
        private readonly AppConfig _config;

        public SettingsWindow() : this(ConfigManager.Load())
        {
        }

        public SettingsWindow(AppConfig config)
        {
            InitializeComponent();
            _config = config;
            _config.Normalize();
            LoadSettings();
        }

        private void LoadSettings()
        {
            RefreshList();
            ChkBlackScreen.IsChecked = _config.EnableBlackScreen;
            ChkDisplayOff.IsChecked = _config.EnableDisplayOff;
            ChkMute.IsChecked = _config.EnableMute;
            ChkAutoKill.IsChecked = _config.EnableAutoKill;
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _config.AppPaths.Add(openFileDialog.FileName);
                RefreshList();
            }
        }

        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AppListbox.SelectedItem is string path)
            {
                _config.AppPaths.Remove(path);
                RefreshList();
            }
        }

        private void RefreshList()
        {
            AppListbox.ItemsSource = new List<string>(_config.AppPaths);
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _config.EnableBlackScreen = ChkBlackScreen.IsChecked ?? false;
                _config.EnableDisplayOff = ChkDisplayOff.IsChecked ?? false;
                _config.EnableMute = ChkMute.IsChecked ?? false;
                _config.EnableAutoKill = ChkAutoKill.IsChecked ?? false;

                ConfigManager.Save(_config);

                var voidWindow = new VoidModeWindow(_config);
                Application.Current.MainWindow = voidWindow;
                voidWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Failed to enter VoidMode from settings window.", ex);
                MessageBox.Show(
                    $"VoidModeへの移行に失敗しました。\n\n{ex.Message}\n\nログ: {AppPaths.LogPath}",
                    "VoidMode",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
