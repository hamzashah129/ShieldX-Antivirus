using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ShieldX.Services;

namespace ShieldX.Views
{
    public partial class SettingsView : UserControl
    {
        private ApiKeyManagerService _keyManager;

        public SettingsView()
        {
            InitializeComponent();
            _keyManager = ApiKeyManagerService.Instance;
            LoadSavedApiKeys();
        }

        private void LoadSavedApiKeys()
        {
            try
            {
                // Load saved API keys (masked for display)
                var vtKey = _keyManager.GetApiKey("VirusTotal");
                if (!string.IsNullOrEmpty(vtKey))
                {
                    VirusTotalKeyBox.Password = vtKey;
                    VirusTotalStatus.Text = "✓ Configured";
                    VirusTotalStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
                }

                var abuseKey = _keyManager.GetApiKey("AbuseIPDB");
                if (!string.IsNullOrEmpty(abuseKey))
                {
                    AbuseIPDBKeyBox.Password = abuseKey;
                    AbuseIPDBStatus.Text = "✓ Configured";
                    AbuseIPDBStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
                }

                var googleKey = _keyManager.GetApiKey("GoogleSafeBrowsing");
                if (!string.IsNullOrEmpty(googleKey))
                {
                    GoogleKeyBox.Password = googleKey;
                    GoogleStatus.Text = "✓ Configured";
                    GoogleStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading API keys: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save API keys
                if (!string.IsNullOrEmpty(VirusTotalKeyBox.Password))
                    _keyManager.SetApiKey("VirusTotal", VirusTotalKeyBox.Password);

                if (!string.IsNullOrEmpty(AbuseIPDBKeyBox.Password))
                    _keyManager.SetApiKey("AbuseIPDB", AbuseIPDBKeyBox.Password);

                if (!string.IsNullOrEmpty(GoogleKeyBox.Password))
                    _keyManager.SetApiKey("GoogleSafeBrowsing", GoogleKeyBox.Password);

                MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TestVirusTotalKey_Click(object sender, RoutedEventArgs e)
        {
            await TestApiKey("VirusTotal", VirusTotalKeyBox.Password, VirusTotalStatus);
        }

        private async void TestAbuseIPDBKey_Click(object sender, RoutedEventArgs e)
        {
            await TestApiKey("AbuseIPDB", AbuseIPDBKeyBox.Password, AbuseIPDBStatus);
        }

        private async void TestGoogleKey_Click(object sender, RoutedEventArgs e)
        {
            await TestApiKey("GoogleSafeBrowsing", GoogleKeyBox.Password, GoogleStatus);
        }

        private async Task TestApiKey(string service, string apiKey, TextBlock statusBlock)
        {
            try
            {
                statusBlock.Text = "Testing...";
                statusBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 152, 0));

                bool isValid = await _keyManager.ValidateApiKey(service, apiKey);

                if (isValid)
                {
                    statusBlock.Text = "✓ Valid API Key";
                    statusBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
                }
                else
                {
                    statusBlock.Text = "✗ Invalid API Key";
                    statusBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54));
                }
            }
            catch (Exception ex)
            {
                statusBlock.Text = $"✗ Error: {ex.Message}";
                statusBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54));
            }
        }

        private void ClearVirusTotalKey_Click(object sender, RoutedEventArgs e)
        {
            ClearApiKey("VirusTotal", VirusTotalKeyBox, VirusTotalStatus);
        }

        private void ClearAbuseIPDBKey_Click(object sender, RoutedEventArgs e)
        {
            ClearApiKey("AbuseIPDB", AbuseIPDBKeyBox, AbuseIPDBStatus);
        }

        private void ClearGoogleKey_Click(object sender, RoutedEventArgs e)
        {
            ClearApiKey("GoogleSafeBrowsing", GoogleKeyBox, GoogleStatus);
        }

        private void ClearApiKey(string service, PasswordBox keyBox, TextBlock statusBlock)
        {
            try
            {
                _keyManager.ClearApiKey(service);
                keyBox.Clear();
                statusBlock.Text = "Cleared";
                statusBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 102, 102));
                MessageBox.Show($"{service} API key cleared", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing API key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
