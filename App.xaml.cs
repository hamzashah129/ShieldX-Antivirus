using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ShieldX.Services;
using ShieldX.Views;
using Microsoft.Win32;

namespace ShieldX
{
    public partial class App : Application
    {
        public static RealTimeProtectionService
            RealTimeProtection { get; } = new();
        public static UsbSecurityService?
            UsbSecurity { get; private set; }

        private MainWindow? _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Load saved theme FIRST - before anything else renders
            ThemeService.LoadSavedTheme();

            // Log startup
            try
            {
                string logFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ShieldX", "startup.log");
                Directory.CreateDirectory(Path.GetDirectoryName(logFile));
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] App startup beginning\n");
            }
            catch { }

            // Clean duplicate entries on every launch (BUG 11)
            try
            {
                Registry.LocalMachine.DeleteSubKeyTree(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ShieldX",
                    throwOnMissingSubKey: false);
                Registry.LocalMachine.DeleteSubKeyTree(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" +
                    @"{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}_is1",
                    throwOnMissingSubKey: false);
            }
            catch { }

            // Global error handlers
            AppDomain.CurrentDomain.UnhandledException +=
                (s, ex) => LogError(ex.ExceptionObject as Exception);
            DispatcherUnhandledException += (s, ex) =>
            {
                LogError(ex.Exception);
                ex.Handled = true;
            };
            System.Threading.Tasks.TaskScheduler
                .UnobservedTaskException += (s, ex) =>
            {
                ex.SetObserved();
            };

            // Initialize the database before any log or module state access
            try
            {
                DatabaseService.Instance.InitializeDatabase();
                Serilog.Log.Information("Database initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] Database initialization failed: {ex.Message}");
                LogStartupError($"Database initialization failed: {ex.Message}");
                Serilog.Log.Error(ex, "Database initialization failed");
            }

            // Load saved module states before starting services
            try
            {
                Task.Run(async () =>
                    await ModuleManager.Instance.LoadModuleStatesAsync())
                    .GetAwaiter().GetResult();
                Serilog.Log.Information("Module states loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[App] Failed to load module states: {ex.Message}");
                LogStartupError($"Module states load failed: {ex.Message}");
            }

            // Initialize CVE feed manager and start auto-updates
            try
            {
                var cveFeedManager = ShieldX.Services.CVEFeedManager.Instance;
                cveFeedManager.StartAutoUpdate(TimeSpan.FromHours(24)); // Update every 24 hours
                Debug.WriteLine("[App] CVE feed manager started with 24-hour auto-update");
                Serilog.Log.Information("CVE feed manager initialized with auto-update enabled");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] CVE feed manager initialization failed: {ex.Message}");
                LogStartupError($"CVE feed manager initialization failed: {ex.Message}");
                Serilog.Log.Error(ex, "Failed to initialize CVE feed manager");
            }

            // Start protection services for enabled modules
            try
            {
                if (ModuleManager.Instance.IsActive("RealTimeProtection"))
                {
                    RealTimeProtection.Start();
                    Debug.WriteLine("[App] RealTime protection started");
                    Serilog.Log.Information("Real-time protection started");
                }
                else
                {
                    Debug.WriteLine("[App] RealTime protection disabled by module state");
                    Serilog.Log.Warning("Real-time protection is disabled");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[App] RealTime failed: {ex.Message}");
                LogStartupError($"RealTime protection failed: {ex.Message}");
                Serilog.Log.Error(ex, "Failed to start real-time protection");
            }

            try
            {
                UsbSecurity = new UsbSecurityService(
                    RealTimeProtection);
                UsbSecurity.Start();
                Debug.WriteLine("[App] USB security started");
                Serilog.Log.Information("USB security started");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[App] USB failed: {ex.Message}");
                LogStartupError($"USB security failed: {ex.Message}");
                Serilog.Log.Error(ex, "Failed to start USB security");
            }

            try
            {
                BackgroundServiceManager.StartServices();
                Serilog.Log.Information("Background services started");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[App] Background services failed: {ex.Message}");
                LogStartupError($"Background services failed: {ex.Message}");
                Serilog.Log.Error(ex, "Failed to start background services");
            }

            // Register ShieldX for auto-start on Windows boot
            try
            {
                StartupService.RegisterShieldXStartup();
                Serilog.Log.Information("ShieldX auto-start registration completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] Auto-start registration failed: {ex.Message}");
                Serilog.Log.Warning(ex, "Failed to register ShieldX for auto-start");
            }

            // Tray icon will be created by MainWindow via TrayIconManager
            bool background = e.Args.Any(a =>
                a.Equals("--background",
                    StringComparison.OrdinalIgnoreCase));

            bool hasScan = e.Args.Length >= 2 &&
                e.Args[0].Equals("--scan",
                    StringComparison.OrdinalIgnoreCase);

            try
            {
                _mainWindow = new MainWindow();

                // CRITICAL: Handle window close → hide to tray (BUG 1)
                _mainWindow.Closing += MainWindow_Closing;

                if (background)
                {
                    // Start minimized — protection runs in background
                    _mainWindow.WindowState = WindowState.Minimized;
                    _mainWindow.ShowInTaskbar = true;
                    _mainWindow.Show();

                    NotificationService.ShowInfo(
                        "ShieldX is protecting your PC",
                        "Real-time protection active. " +
                        "Click tray icon to open.",
                        duration: 4);
                    Serilog.Log.Information("ShieldX started in background mode");
                }
                else if (hasScan)
                {
                    string path = e.Args[1].Trim('"');
                    _mainWindow.Show();
                    _mainWindow.Dispatcher.BeginInvoke(
                        System.Windows.Threading
                            .DispatcherPriority.Loaded,
                        new Action(() =>
                        {
                            if (_mainWindow.DataContext is
                                ViewModels.MainViewModel vm)
                                vm.NavigateToScanWithPath(path);
                        }));
                    Serilog.Log.Information($"ShieldX started with custom scan: {path}");
                }
                else
                {
                    _mainWindow.Show();
                    Serilog.Log.Information("ShieldX started normally");
                }

                LogStartupError("App started successfully");
                Serilog.Log.Information("ShieldX Professional Antivirus started successfully");
            }
            catch (Exception ex)
            {
                LogStartupError($"CRITICAL: MainWindow initialization failed: {ex.Message}\n{ex.StackTrace}");
                Debug.WriteLine($"[App] CRITICAL: {ex}");
                Serilog.Log.Fatal(ex, "ShieldX startup failed");
                // Show error message box as last resort
                try
                {
                    System.Windows.MessageBox.Show(
                        $"ShieldX failed to start:\n\n{ex.Message}\n\nPlease check the logs in:\n{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShieldX")}",
                        "ShieldX Startup Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
                catch { }
            }
        }



        private void ShowMainWindow()
        {
            Dispatcher.Invoke(() =>
            {
                if (_mainWindow == null) return;
                _mainWindow.ShowInTaskbar = true;
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
                _mainWindow.Focus();
            });
        }

        // ── Window close → hide to tray ──────────────────────
        private void MainWindow_Closing(object? sender,
            System.ComponentModel.CancelEventArgs e)
        {
            // CANCEL the close — hide instead
            e.Cancel = true;
            _mainWindow!.ShowInTaskbar = false;
            _mainWindow!.Hide();

            // Show modern toast notification
            try
            {
                ToastNotificationService.ShowProtectionStatus(
                    true,
                    "Double-click the tray icon to open ShieldX.");
            }
            catch { }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                BackgroundServiceManager.StopServices();
            }
            catch { }

            try { RealTimeProtection.Stop(); } catch { }
            try { UsbSecurity?.Stop(); } catch { }

            base.OnExit(e);
        }

        private static void LogError(Exception? ex)
        {
            if (ex == null) return;
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "ShieldX", "crash.log");
                Directory.CreateDirectory(
                    Path.GetDirectoryName(logPath)!);
                File.AppendAllText(logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                    $"{ex}\n\n");
            }
            catch { }
        }

        private static void LogStartupError(string message)
        {
            try
            {
                string logFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ShieldX", "startup.log");
                Directory.CreateDirectory(Path.GetDirectoryName(logFile));
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\n");
            }
            catch { }
        }
    }
}
