using System;
using System.Collections.Generic;
using System.Linq;
using ShieldX.Models;

namespace ShieldX.Services
{
    public class AIGuardAnalyzer
    {
        private readonly Dictionary<int, BehaviorProfile> _behaviorProfiles = new();
        private readonly object _profileLock = new();

        // Scoring weights
        private const float NetworkBeaconWeight = 0.25f;
        private const float FileSystemMutationWeight = 0.20f;
        private const float PrivilegeEscalationWeight = 0.20f;
        private const float HidingWeight = 0.15f;
        private const float ResourceAbuseWeight = 0.10f;
        private const float CredentialAccessWeight = 0.10f;

        private const int BehaviorWindowSeconds = 60;

        public AIGuardResult Analyze(ProcessSnapshot snapshot)
        {
            if (snapshot == null)
                return new AIGuardResult { ThreatScore = 0, ThreatClass = "Benign" };

            // Update behavior profile for this process
            UpdateBehaviorProfile(snapshot);

            // Calculate individual threat scores
            float networkBeaconScore = CalculateNetworkBeaconScore(snapshot);
            float fileSystemScore = CalculateFileSystemMutationScore(snapshot);
            float privilegeScore = CalculatePrivilegeEscalationScore(snapshot);
            float hidingScore = CalculateHidingScore(snapshot);
            float resourceScore = CalculateResourceAbuseScore(snapshot);
            float credentialScore = CalculateCredentialAccessScore(snapshot);

            // Weighted average to final threat score
            float finalScore = (networkBeaconScore * NetworkBeaconWeight) +
                              (fileSystemScore * FileSystemMutationWeight) +
                              (privilegeScore * PrivilegeEscalationWeight) +
                              (hidingScore * HidingWeight) +
                              (resourceScore * ResourceAbuseWeight) +
                              (credentialScore * CredentialAccessWeight);

            // Clamp score to 0.0-1.0
            finalScore = Math.Max(0, Math.Min(1, finalScore));

            // Determine threat class based on highest scoring indicators
            string threatClass = DetermineThreatClass(
                networkBeaconScore, fileSystemScore, privilegeScore,
                hidingScore, resourceScore, credentialScore);

            // Generate reason
            string reason = GenerateReason(threatClass, new[] {
                networkBeaconScore, fileSystemScore, privilegeScore,
                hidingScore, resourceScore, credentialScore
            });

            return new AIGuardResult
            {
                ThreatScore = finalScore,
                ThreatClass = threatClass,
                Reason = reason,
                ShouldBlock = finalScore > 0.85f,
                ShouldSuspend = finalScore > 0.65f,
                Process = snapshot,
                DetectedAt = DateTime.Now,
                Indicators = new[] {
                    networkBeaconScore, fileSystemScore, privilegeScore,
                    hidingScore, resourceScore, credentialScore
                }
            };
        }

        private float CalculateNetworkBeaconScore(ProcessSnapshot snapshot)
        {
            if (snapshot.TcpConnections.Count == 0)
                return 0;

            lock (_profileLock)
            {
                if (!_behaviorProfiles.TryGetValue(snapshot.Pid, out var profile))
                    return 0;

                // Check for periodic connection pattern
                if (profile.TcpConnectionCounts.Count < 3)
                    return 0;

                // Analyze periodicity - consistent connections at regular intervals
                var recentCounts = profile.TcpConnectionCounts.TakeLast(5).ToList();
                if (recentCounts.Count < 3)
                    return 0;

                // Look for consistency in connection counts
                var variance = CalculateVariance(recentCounts.Cast<double>().ToList());
                if (variance < 0.3) // Low variance suggests beacon pattern
                    return (float)Math.Min(1, recentCounts.Average() / 10.0); // Normalize

                return 0.3f; // Elevated score for any periodic connections
            }
        }

        private float CalculateFileSystemMutationScore(ProcessSnapshot snapshot)
        {
            lock (_profileLock)
            {
                if (!_behaviorProfiles.TryGetValue(snapshot.Pid, out var profile))
                    return 0;

                // High file write rate (>20/sec) indicates potential ransomware or malware
                float writeRate = profile.FileWriteCount / Math.Max(1, (float)(DateTime.Now - profile.FirstSeen).TotalSeconds);

                if (writeRate > 20)
                    return 1.0f;
                else if (writeRate > 10)
                    return 0.7f;
                else if (writeRate > 5)
                    return 0.4f;

                return 0;
            }
        }

        private float CalculatePrivilegeEscalationScore(ProcessSnapshot snapshot)
        {
            // Check if non-system process running as SYSTEM or with special privileges
            bool isSystemProcess = snapshot.Name.Equals("svchost.exe", StringComparison.OrdinalIgnoreCase) ||
                                 snapshot.Name.Equals("System", StringComparison.OrdinalIgnoreCase) ||
                                 snapshot.Name.Equals("services.exe", StringComparison.OrdinalIgnoreCase) ||
                                 snapshot.Name.Equals("lsass.exe", StringComparison.OrdinalIgnoreCase);

            if (isSystemProcess)
                return 0; // Expected to run as SYSTEM

            // Non-system process running as SYSTEM = suspicious
            if (snapshot.UserAccount.Contains("SYSTEM", StringComparison.OrdinalIgnoreCase) ||
                snapshot.UserAccount.Contains("LOCAL SYSTEM", StringComparison.OrdinalIgnoreCase))
            {
                return 0.9f;
            }

            // Check for indicators in path or name
            if (snapshot.FullPath.Contains("System32", StringComparison.OrdinalIgnoreCase) &&
                !snapshot.IsSigned)
            {
                return 0.7f;
            }

            return 0;
        }

        private float CalculateHidingScore(ProcessSnapshot snapshot)
        {
            // Process with no window, no console, not a service, and no valid signature
            int hidingIndicators = 0;

            // No visible window = hiding
            if (snapshot.Name != "explorer.exe" && snapshot.ThreadCount < 2)
                hidingIndicators++;

            // Not signed = suspicious
            if (!snapshot.IsSigned)
                hidingIndicators++;

            // Running from unusual location
            if (!snapshot.FullPath.StartsWith("C:\\Program Files", StringComparison.OrdinalIgnoreCase) &&
                !snapshot.FullPath.StartsWith("C:\\Windows", StringComparison.OrdinalIgnoreCase) &&
                !snapshot.FullPath.StartsWith("C:\\Users", StringComparison.OrdinalIgnoreCase))
            {
                hidingIndicators++;
            }

            return hidingIndicators switch
            {
                3 => 1.0f,
                2 => 0.6f,
                1 => 0.3f,
                _ => 0
            };
        }

        private float CalculateResourceAbuseScore(ProcessSnapshot snapshot)
        {
            // CPU > 80% sustained or RAM > 500MB for unknown process
            if (snapshot.CpuPercent > 80 || snapshot.RamMb > 500)
            {
                if (!snapshot.IsSigned)
                    return 0.9f;
                else if (snapshot.SignerName.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
                    return 0.2f; // Less suspicious if signed by Microsoft
                else
                    return 0.5f;
            }

            if (snapshot.CpuPercent > 50 || snapshot.RamMb > 300)
            {
                return !snapshot.IsSigned ? 0.4f : 0.1f;
            }

            return 0;
        }

        private float CalculateCredentialAccessScore(ProcessSnapshot snapshot)
        {
            // Check for access to LSASS or credential-related paths
            if (snapshot.FullPath.Contains("lsass", StringComparison.OrdinalIgnoreCase))
                return 0.95f;

            // Check command line for credential access patterns
            if (snapshot.CommandLine != null)
            {
                var suspiciousPatterns = new[] { "lsass", "credential", "password", "token", "kerberos" };
                int patternMatches = suspiciousPatterns.Count(p =>
                    snapshot.CommandLine.Contains(p, StringComparison.OrdinalIgnoreCase));

                if (patternMatches > 0 && !snapshot.IsSigned)
                    return 0.7f;
            }

            return 0;
        }

        private void UpdateBehaviorProfile(ProcessSnapshot snapshot)
        {
            lock (_profileLock)
            {
                if (!_behaviorProfiles.TryGetValue(snapshot.Pid, out var profile))
                {
                    profile = new BehaviorProfile
                    {
                        Pid = snapshot.Pid,
                        Name = snapshot.Name,
                        FirstSeen = DateTime.Now,
                        BaselineCpu = snapshot.CpuPercent,
                        BaselineRam = snapshot.RamMb
                    };
                    _behaviorProfiles[snapshot.Pid] = profile;
                }

                // Add CPU sample
                profile.CpuSamples.Add(snapshot.CpuPercent);
                if (profile.CpuSamples.Count > 120) // Keep last 120 samples (~60 seconds at 2 samples/sec)
                    profile.CpuSamples.RemoveAt(0);

                // Add TCP connection count
                profile.TcpConnectionCounts.Add(snapshot.TcpConnections.Count);
                if (profile.TcpConnectionCounts.Count > 120)
                    profile.TcpConnectionCounts.RemoveAt(0);

                // Clean old profiles (older than 10 minutes)
                var now = DateTime.Now;
                var oldPids = _behaviorProfiles.Where(x => (now - x.Value.FirstSeen).TotalSeconds > 600).Select(x => x.Key).ToList();
                foreach (var pid in oldPids)
                    _behaviorProfiles.Remove(pid);
            }
        }

        private string DetermineThreatClass(float network, float fileSystem, float privilege,
            float hiding, float resource, float credential)
        {
            // Determine threat class based on highest scoring indicators
            var indicators = new Dictionary<string, float>
            {
                { "Spyware", network },
                { "Ransomware", fileSystem },
                { "Trojan", Math.Max(privilege, Math.Max(hiding, Math.Max(network, fileSystem))) },
                { "Keylogger", credential },
                { "Cryptominer", resource },
                { "Suspicious", Math.Max(hiding, resource) }
            };

            var maxEntry = indicators.OrderByDescending(x => x.Value).First();
            return maxEntry.Value > 0 ? maxEntry.Key : "Benign";
        }

        private string GenerateReason(string threatClass, float[] indicators)
        {
            return threatClass switch
            {
                "Spyware" => "Process exhibits beacon communication patterns typical of spyware.",
                "Ransomware" => "Elevated file system mutation rate detected - possible ransomware behavior.",
                "Trojan" => "Multiple suspicious indicators detected - possible trojan activity.",
                "Keylogger" => "Process accessing credential/sensitive authentication interfaces.",
                "Cryptominer" => "Sustained high resource utilization by unsigned process.",
                "Suspicious" => "Process exhibits suspicious behavioral patterns.",
                _ => "Suspicious process behavior detected."
            };
        }

        private double CalculateVariance(List<double> values)
        {
            if (values.Count == 0)
                return 0;

            double mean = values.Average();
            double variance = values.Sum(x => Math.Pow(x - mean, 2)) / values.Count;
            return Math.Sqrt(variance); // Return standard deviation instead
        }
    }
}
