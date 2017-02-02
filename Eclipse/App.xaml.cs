using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace Eclipse
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private TaskbarIcon notifyicon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            notifyicon = (TaskbarIcon)FindResource("NotifyIcon");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            notifyicon.Dispose();
            base.OnExit(e);
        }
    }
}
