using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ShieldX.Models;
using ShieldX.Services;
using ShieldX.ViewModels;
using ShieldX.Views;

namespace ShieldX.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private DispatcherTimer _clockTimer;
        private HwndSource _hwndSource;
        private TrayIconManager _trayIconManager;
        private bool _isSidebarExpanded = true;

        // Routed Commands for keyboard shortcuts
        public static readonly RoutedCommand QuickScanCommand = new RoutedCommand();
        public static readonly RoutedCommand FullScanCommand = new RoutedCommand();
        public static readonly RoutedCommand LogsCommand = new RoutedCommand();
        public static readonly RoutedCommand NetworkCommand = new RoutedCommand();
        public static readonly RoutedCommand SettingsCommand = new RoutedCommand();

        // PInvoke declarations for window management
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;

        public MainWindow()
        {
            InitializeComponent();
            InitializeNavigation();
            InitializeClock();
            InitializeCommandBindings();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Navigate to dashboard by default - custom scan is handled by App.xaml.cs via command line args
            NavigateToPage("Dashboard");

            // Subscribe to theme changes to update icon
            ThemeService.ThemeChanged += theme =>
            {
                Dispatcher.Invoke(() =>
                {
                    ThemeToggleButton.Content = theme == AppTheme.Dark ? "☀️" : "🌙";
                });
            };

            // Handle window messages for proper maximize behavior
            SourceInitialized += MainWindow_SourceInitialized;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize system tray icon
            _trayIconManager = new TrayIconManager();
            _trayIconManager.Initialize(this);

            // Wire up tray icon events
            _trayIconManager.OpenRequested += (s, args) =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            };

            _trayIconManager.QuickScanRequested += (s, args) =>
            {
                NavigateToPage("Scan");
            };

            _trayIconManager.PauseProtectionRequested += (s, args) =>
            {
                ToggleProtectionPause();
            };

            _trayIconManager.ExitRequested += (s, args) =>
            {
                Application.Current.Shutdown();
            };
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            CloseButton_Click(this, new RoutedEventArgs());
        }

        protected override void OnClosed(EventArgs e)
        {
            _trayIconManager?.Dispose();
            _clockTimer?.Stop();
            // DON'T stop background services here - they should continue running
            // Services only stop when user clicks "Exit" from the exit dialog
            base.OnClosed(e);
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            _hwndSource = (HwndSource)PresentationSource.FromVisual(this);
            _hwndSource.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_GETMINMAXINFO = 0x0024;

            if (msg == WM_GETMINMAXINFO)
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            // Get the monitor that contains the window
            IntPtr hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (hMonitor != IntPtr.Zero)
            {
                var monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfo);

                if (GetMonitorInfo(hMonitor, ref monitorInfo))
                {
                    // Adjust the maximized size to not cover the taskbar
                    mmi.ptMaxPosition.x = monitorInfo.rcWork.Left - monitorInfo.rcMonitor.Left;
                    mmi.ptMaxPosition.y = monitorInfo.rcWork.Top - monitorInfo.rcMonitor.Top;
                    mmi.ptMaxSize.x = monitorInfo.rcWork.Right - monitorInfo.rcWork.Left;
                    mmi.ptMaxSize.y = monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top;
                }
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch { }
            }
        }

        private void TitleBar_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MaximizeButton_Click(sender, e);
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (ThemeService.IsDark)
            {
                ThemeService.ApplyTheme(AppTheme.Light);
                ThemeToggleButton.Content = "🌙";
            }
            else
            {
                ThemeService.ApplyTheme(AppTheme.Dark);
                ThemeToggleButton.Content = "☀️";
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeButton.Content = "□";
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaximizeButton.Content = "❐";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ExitConfirmDialog { Owner = this };
            dialog.ShowDialog();
            switch (dialog.Result)
            {
                case ExitDialogResult.Tray:
                    // Hide to tray - services continue running
                    ShowInTaskbar = false;
                    Hide();
                    WindowState = WindowState.Minimized;
                    _trayIconManager?.ShowBalloon("ShieldX", "Protection running in background", System.Windows.Forms.ToolTipIcon.Info);
                    break;
                case ExitDialogResult.Exit:
                    // Actually exit - stop everything
                    _trayIconManager?.Dispose();
                    Application.Current.Shutdown();
                    break;
            }
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button button && button != CloseButton)
            {
                button.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0x2A, 0x3A));
            }
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button button && button != CloseButton)
            {
                button.Background = System.Windows.Media.Brushes.Transparent;
            }
        }

        private void CloseButton_MouseEnter(object sender, MouseEventArgs e)
        {
            CloseButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC4, 0x2B, 0x1C));
            CloseButton.Foreground = System.Windows.Media.Brushes.White;
        }

        private void CloseButton_MouseLeave(object sender, MouseEventArgs e)
        {
            CloseButton.Background = System.Windows.Media.Brushes.Transparent;
            CloseButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x88, 0x99, 0xAA));
        }

        private void InitializeNavigation()
        {
            var navigationItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem { Name = "Dashboard", Icon = "📊" },
                new NavigationItem { Name = "Scan", Icon = "🔍" },
                new NavigationItem { Name = "Protection", Icon = "🛡️" },
                new NavigationItem { Name = "Quarantine", Icon = "🔒" },
                new NavigationItem { Name = "Network", Icon = "🌐" },
                new NavigationItem { Name = "Processes", Icon = "⚙️" },
                new NavigationItem { Name = "Startup", Icon = "⏱️" },
                new NavigationItem { Name = "VulnerabilityScanner", Icon = "⚠️" },
                new NavigationItem { Name = "AIGuard", Icon = "🤖" },
                new NavigationItem { Name = "Vault", Icon = "🔑" },
                new NavigationItem { Name = "DarkWeb", Icon = "🌐" },
                new NavigationItem { Name = "ThreatScanner", Icon = "🔬" },
                new NavigationItem { Name = "ThreatHistory", Icon = "📋" },
                new NavigationItem { Name = "Updates", Icon = "📥" },
                new NavigationItem { Name = "Settings", Icon = "⚙️" },
                new NavigationItem { Name = "Logs", Icon = "📝" },
                new NavigationItem { Name = "About", Icon = "ℹ️" }
            };

            NavigationListBox.ItemsSource = navigationItems;
        }

        private void InitializeClock()
        {
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, e) =>
            {
                ClockText.Text = DateTime.Now.ToString("h:mm:ss tt");
            };
            _clockTimer.Start();
        }

        private void NavigationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavigationListBox.SelectedItem is NavigationItem item)
            {
                NavigateToPage(item.Name);
            }
        }

        private void NavigateToPage(string pageName)
        {
            Page page = pageName switch
            {
                "Dashboard" => new DashboardPage(),
                "Scan" => new ScanPage(),
                "Protection" => new ProtectionPage { DataContext = new ProtectionViewModel() },
                "Quarantine" => new QuarantinePage { DataContext = new QuarantineViewModel() },
                "Network" => new NetworkPage(),
                "Processes" => new ProcessesPage(),
                "Startup" => new StartupView(),
                "Updates" => new UpdatePage(),
                "Settings" => new SettingsPage { DataContext = new SettingsViewModel() },
                "Logs" => new LogsPage { DataContext = new LogsPageViewModel() },
                "About" => new AboutPage(),
                "Vault" => new VaultPage(),
                "DarkWeb" => new DarkWebView(),
                "ThreatScanner" => new ThreatScannerPage(),
                "ThreatHistory" => new ThreatHistoryPage { DataContext = new ThreatHistoryViewModel() },
                "VulnerabilityScanner" => new VulnerabilityPage { DataContext = new VulnerabilityViewModel() },
                "AIGuard" => new AIGuardPage { DataContext = new AIGuardViewModel() },
                _ => new DashboardPage()
            };

            ContentFrame.Navigate(page);
            PageTitleText.Text = pageName;
        }

        /// <summary>
        /// Public method to navigate to a page from child controls.
        /// Called by pages like DashboardPage to navigate to other pages.
        /// </summary>
        public void NavigateToDashboardPage(string pageName)
        {
            NavigateToPage(pageName);
        }

        private void ApplyPageTransitionAnimation()
        {
            // Apply fade in animation to the frame content
            try
            {
                if (ContentFrame.Content is FrameworkElement element)
                {
                    element.Opacity = 0;
                    var fadeIn = (Storyboard)Resources["PageTransitionIn"];
                    if (fadeIn != null)
                    {
                        Storyboard.SetTarget(fadeIn, element);
                        fadeIn.Begin();
                    }
                }
            }
            catch { }
        }

        private void InitializeCommandBindings()
        {
            // Add command bindings for keyboard shortcuts
            CommandBinding quickScanBinding = new CommandBinding(QuickScanCommand, QuickScan_Executed);
            CommandBinding fullScanBinding = new CommandBinding(FullScanCommand, FullScan_Executed);
            CommandBinding logsBinding = new CommandBinding(LogsCommand, Logs_Executed);
            CommandBinding networkBinding = new CommandBinding(NetworkCommand, Network_Executed);
            CommandBinding settingsBinding = new CommandBinding(SettingsCommand, Settings_Executed);

            CommandBindings.Add(quickScanBinding);
            CommandBindings.Add(fullScanBinding);
            CommandBindings.Add(logsBinding);
            CommandBindings.Add(networkBinding);
            CommandBindings.Add(settingsBinding);
        }

        private void QuickScan_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            NavigateToPage("Scan");
        }

        private void FullScan_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            NavigateToPage("Scan");
        }

        private void Logs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            NavigateToPage("Logs");
        }

        private void Network_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            NavigateToPage("Network");
        }

        private void Settings_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            NavigateToPage("Settings");
        }

        private void ToggleProtectionPause()
        {
            try
            {
                if (App.RealTimeProtection == null)
                {
                    _trayIconManager?.ShowBalloon("Protection", "Real-time protection not initialized", System.Windows.Forms.ToolTipIcon.Warning);
                    return;
                }

                var isPaused = App.RealTimeProtection._isPaused;
                
                if (isPaused)
                {
                    App.RealTimeProtection.Start();
                    _trayIconManager?.ShowBalloon("Protection", "Real-time protection resumed", System.Windows.Forms.ToolTipIcon.Info);
                    Serilog.Log.Information("Real-time protection resumed");
                }
                else
                {
                    App.RealTimeProtection.Stop();
                    _trayIconManager?.ShowBalloon("Protection", "Real-time protection paused", System.Windows.Forms.ToolTipIcon.Warning);
                    Serilog.Log.Information("Real-time protection paused");
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to toggle protection pause");
                _trayIconManager?.ShowBalloon("Error", "Failed to toggle protection", System.Windows.Forms.ToolTipIcon.Error);
            }
        }

        private void SidebarToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleSidebar();
        }

        private void ToggleSidebar()
        {
            _isSidebarExpanded = !_isSidebarExpanded;

            Storyboard animation = _isSidebarExpanded
                ? (Storyboard)Resources["SidebarExpand"]
                : (Storyboard)Resources["SidebarCollapse"];

            // Update toggle button appearance
            SidebarToggleButton.Content = _isSidebarExpanded ? "◄" : "►";

            if (animation != null)
            {
                // Clone the storyboard to avoid issues
                animation = animation.Clone();
                
                // Set the target and begin animation
                Storyboard.SetTarget(animation, SidebarBorder);
                animation.Completed += (s, e) =>
                {
                    // Update margins after animation completes
                    MainContentArea.Margin = new Thickness(_isSidebarExpanded ? 250 : 48, 0, 0, 0);
                };
                animation.Begin();
            }
        }

        private void StartCustomScanWithPath(string path)
        {
            NavigateToPage("Scan");
        }
    }

    public class NavigationItem
    {
        public string Name { get; set; }
        public string Icon { get; set; }
    }
}