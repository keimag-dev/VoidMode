using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;

namespace VoidMode
{
    public partial class SettingsWindow : Window
    {
        private AppConfig _config;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            _config = ConfigManager.Load();
            AppListbox.ItemsSource = _config.AppPaths;
            ChkBlackScreen.IsChecked = _config.EnableBlackScreen;
            ChkDisplayOff.IsChecked = _config.EnableDisplayOff;
            ChkMute.IsChecked = _config.EnableMute;
            ChkAutoKill.IsChecked = _config.EnableAutoKill;
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*";
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
            // Refresh ListBox binding
            var items = new List<string>(_config.AppPaths);
            AppListbox.ItemsSource = items;
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            _config.EnableBlackScreen = ChkBlackScreen.IsChecked ?? false;
            _config.EnableDisplayOff = ChkDisplayOff.IsChecked ?? false;
            _config.EnableMute = ChkMute.IsChecked ?? false;
            _config.EnableAutoKill = ChkAutoKill.IsChecked ?? false;
            
            ConfigManager.Save(_config);
            
            // Launch Server Mode
            var voidWindow = new VoidModeWindow();
            voidWindow.Show();
            this.Close();
        }
    }
}
