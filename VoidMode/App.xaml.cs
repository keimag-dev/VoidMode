using System.Windows;

namespace VoidMode
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);
            var settings = new SettingsWindow();
            settings.Show();
        }
    }
}
