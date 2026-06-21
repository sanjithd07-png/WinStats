using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace WinStats
{
    public partial class App : Application
    {
        private TaskbarIcon? _taskbarIcon;
        private MainWindow? _mainWindow;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            _mainWindow = new MainWindow();
            _mainWindow.Show();

            _taskbarIcon = new TaskbarIcon();
            _taskbarIcon.Icon = System.Drawing.SystemIcons.Information;
            _taskbarIcon.ToolTipText = "WinStats Monitor";
            _taskbarIcon.ContextMenu = (System.Windows.Controls.ContextMenu)FindResource("TrayMenu");

            // Re-appear when double clicking the tray icon
            _taskbarIcon.TrayMouseDoubleClick += (s, args) => MenuShow_Click(null, null);
        }

        private void MenuShow_Click(object? sender, RoutedEventArgs? e)
        {
            if (_mainWindow != null)
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
                _mainWindow.Topmost = true;
            }
        }

        private void MenuExit_Click(object? sender, RoutedEventArgs e)
        {
            _taskbarIcon?.Dispose();
            Current.Shutdown();
        }
    }
}