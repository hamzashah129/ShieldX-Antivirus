using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ShieldX.Services;

namespace ShieldX.Views
{
    public partial class NetworkPage : Page
    {
        private readonly ObservableCollection<NetworkInterfaceEntry> _interfaces = new();
        private readonly ObservableCollection<NetworkConnectionEntry> _connections = new();
        private readonly DispatcherTimer _refreshTimer;

        public NetworkPage()
        {
            InitializeComponent();
            NetworkGrid.ItemsSource = _interfaces;
            ConnectionsGrid.ItemsSource = _connections;

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _refreshTimer.Tick += (s, e) => LoadNetworkData();
            _refreshTimer.Start();

            LoadNetworkData();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadNetworkData();
        }

        private void LoadNetworkData()
        {
            _interfaces.Clear();
            _connections.Clear();

            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnections = properties.GetActiveTcpConnections();

            foreach (var ni in networkInterfaces.OrderByDescending(i => i.OperationalStatus == OperationalStatus.Up))
            {
                var ipProps = ni.GetIPProperties();
                var ipv4 = ipProps.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address?.ToString() ?? "-";
                var ipv6 = ipProps.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6)?.Address?.ToString() ?? "-";

                _interfaces.Add(new NetworkInterfaceEntry
                {
                    Name = string.IsNullOrWhiteSpace(ni.Name) ? ni.Description : ni.Name,
                    Status = ni.OperationalStatus.ToString(),
                    IPv4 = ipv4,
                    IPv6 = ipv6,
                    Speed = ni.Speed > 0 ? $"{ni.Speed / 1_000_000} Mbps" : "Unknown"
                });
            }

            foreach (var connection in tcpConnections.OrderBy(c => c.State.ToString()).ThenBy(c => c.LocalEndPoint?.ToString()))
            {
                var remoteIp = connection.RemoteEndPoint?.Address?.ToString() ?? "N/A";
                var remotePort = connection.RemoteEndPoint?.Port ?? 0;
                var state = connection.State.ToString();
                
                // Use NetworkSecurityService for risk assessment
                var risk = NetworkSecurityService.Instance.AssessConnectionRisk(remoteIp, remotePort, state);
                
                _connections.Add(new NetworkConnectionEntry
                {
                    LocalEndpoint = connection.LocalEndPoint?.ToString() ?? "N/A",
                    RemoteEndpoint = connection.RemoteEndPoint?.ToString() ?? "N/A",
                    RemoteIp = remoteIp,
                    State = state,
                    Risk = risk
                });
            }
        }

        private static bool IsLoopbackOrPrivateIp(IPAddress ip)
        {
            if (ip == null) return false;
            if (ip.AddressFamily != AddressFamily.InterNetwork) return false;
            var bytes = ip.GetAddressBytes();
            // Loopback: 127.x.x.x
            if (bytes[0] == 127) return true;
            // Private ranges: 10.x.x.x, 172.16-31.x.x, 192.168.x.x
            if (bytes[0] == 10) return true;
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            return false;
        }

        private static bool IsTrustedIp(IPAddress ip)
        {
            if (ip == null) return false;
            var ipStr = ip.ToString();
            // Known trusted services
            var trustedIPs = new[] { "8.8.8.8", "8.8.4.4", "1.1.1.1", "1.0.0.1", "208.67.222.123", "208.67.220.123" };
            return trustedIPs.Contains(ipStr);
        }

        private static bool IsSuspiciousConnection(TcpConnectionInformation connection)
        {
            if (connection?.RemoteEndPoint?.Address == null) return false;

            var remoteIp = connection.RemoteEndPoint.Address.ToString();
            var remotePort = connection.RemoteEndPoint.Port;
            var state = connection.State.ToString();

            // Use NetworkSecurityService for risk assessment
            var riskLevel = NetworkSecurityService.Instance.AssessConnectionRisk(remoteIp, remotePort, state);
            
            // Consider "High", "Critical", or "Blocked" as suspicious
            return riskLevel == "Critical" || riskLevel == "High" || riskLevel == "Blocked";
        }

        private async void BlockIpButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string remoteIp && !string.IsNullOrWhiteSpace(remoteIp))
            {
                try
                {
                    var result = await NetworkSecurityService.Instance.BlockIpAsync(remoteIp);
                    
                    if (result)
                    {
                        MessageBox.Show($"IP {remoteIp} has been blocked successfully.", "IP Blocked", MessageBoxButton.OK, MessageBoxImage.Information);
                        // Refresh to update display
                        LoadNetworkData();
                    }
                    else
                    {
                        MessageBox.Show($"Failed to block IP {remoteIp}. Please ensure you have administrator privileges.", "Block Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to block IP: {ex.Message}", "Block IP Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class NetworkInterfaceEntry
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string IPv4 { get; set; }
        public string IPv6 { get; set; }
        public string Speed { get; set; }
    }

    public class NetworkConnectionEntry
    {
        public string LocalEndpoint { get; set; }
        public string RemoteEndpoint { get; set; }
        public string RemoteIp { get; set; }
        public string State { get; set; }
        public string Risk { get; set; }
    }
}