using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace WinStats
{
    public partial class MainWindow : Window
    {
        private HardwareMonitor _monitor;
        private DispatcherTimer _hardwareTimer;
        private DispatcherTimer _uiTimer;
        private bool _isPinned = false;

        // Native Win32 configurations
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;

        // Win32 structs and imports for Fullscreen Detection
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();

            _monitor = new HardwareMonitor();

            // TIMER 1: Hardware fetching (Runs every 1 second to save CPU power)
            _hardwareTimer = new DispatcherTimer();
            _hardwareTimer.Interval = TimeSpan.FromSeconds(1);
            _hardwareTimer.Tick += HardwareTimer_Tick;
            _hardwareTimer.Start();

            // TIMER 2: UI & Topmost enforcement (Runs every 100ms for instant snapping)
            _uiTimer = new DispatcherTimer();
            _uiTimer.Interval = TimeSpan.FromMilliseconds(100);
            _uiTimer.Tick += UiTimer_Tick;
            _uiTimer.Start();

            this.SizeChanged += MainWindow_SizeChanged;
            this.StateChanged += MainWindow_StateChanged;

            // Trigger an initial layout check
            UpdateStackPanelOrientation();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);
        }

        private void HardwareTimer_Tick(object? sender, EventArgs e)
        {
            _monitor.Update();
            CpuText.Text = _monitor.GetCpuStats();
            GpuText.Text = _monitor.GetGpuStats();
            RamText.Text = _monitor.GetRamStats();
        }

        private void UiTimer_Tick(object? sender, EventArgs e)
        {
            // Check if user is playing a fullscreen game
            if (IsForegroundFullScreen())
            {
                this.Opacity = 0;
            }
            else
            {
                this.Opacity = 1;

                // Force Topmost instantly
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    SetWindowPos(helper.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
            }
        }

        private bool IsForegroundFullScreen()
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero) return false;

            IntPtr desktopWindow = GetDesktopWindow();
            IntPtr shellWindow = GetShellWindow();

            if (foregroundWindow == desktopWindow || foregroundWindow == shellWindow)
            {
                return false;
            }

            GetWindowRect(foregroundWindow, out RECT rect);

            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            return width >= (int)SystemParameters.PrimaryScreenWidth &&
                   height >= (int)SystemParameters.PrimaryScreenHeight;
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }
        }

        private void Border_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (_isPinned) return;

            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
                SaveSettings();
            }
        }

        private void MainWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            SaveSettings();
            UpdateStackPanelOrientation();
        }

        private void UpdateStackPanelOrientation()
        {
            // If the window is taller than it is wide, stack the text vertically. Otherwise, lay it out horizontally.
            if (MainStackPanel != null)
            {
                MainStackPanel.Orientation = this.Height > this.Width ? Orientation.Vertical : Orientation.Horizontal;
            }
        }

        private void MenuPin_Click(object? sender, RoutedEventArgs e)
        {
            _isPinned = !_isPinned;
            ApplyPinState();
            SaveSettings();
        }

        private void ApplyPinState()
        {
            if (_isPinned)
            {
                PinMenu.Header = "Unpin Overlay";
                this.ResizeMode = ResizeMode.NoResize;
            }
            else
            {
                PinMenu.Header = "Pin Overlay";
                this.ResizeMode = ResizeMode.CanResize;
            }
        }

        private void MenuHide_Click(object? sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void SaveSettings()
        {
            if (_isPinned && this.IsLoaded)
            {
                Properties.Settings.Default.IsPinned = _isPinned;
                Properties.Settings.Default.Save();
                return;
            }

            Properties.Settings.Default.Top = this.Top;
            Properties.Settings.Default.Left = this.Left;
            Properties.Settings.Default.Width = this.Width;
            Properties.Settings.Default.Height = this.Height;
            Properties.Settings.Default.IsPinned = _isPinned;
            Properties.Settings.Default.Save();
        }

        private void LoadSettings()
        {
            if (Properties.Settings.Default.Top != 0) this.Top = Properties.Settings.Default.Top;
            if (Properties.Settings.Default.Left != 0) this.Left = Properties.Settings.Default.Left;
            if (Properties.Settings.Default.Width > 50) this.Width = Properties.Settings.Default.Width;
            if (Properties.Settings.Default.Height > 10) this.Height = Properties.Settings.Default.Height;

            _isPinned = Properties.Settings.Default.IsPinned;
            ApplyPinState();
        }
    }
}