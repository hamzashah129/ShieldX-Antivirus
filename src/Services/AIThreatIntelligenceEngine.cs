using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ShieldX.Models;

namespace ShieldX.Services
{
    public class AITthreatIntelligenceEngine
    {
        private readonly ThreatDatabase _threatDb;
        private readonly Dictionary<string, ThreatPattern> _knownPatterns;
        private readonly Random _random = new Random();

        public AITthreatIntelligenceEngine()
        {
            _threatDb = new ThreatDatabase();
            _knownPatterns = new Dictionary<string, ThreatPattern>
            {
                // Known malware signatures with AI-enhanced detection
                ["trojan"] = new ThreatPattern
                {
                    Name = "Trojan Horse",
                    Severity = ThreatSeverity.High,
                    Indicators = new[] { "suspicious_imports", "registry_modifications", "network_connections" },
                    ConfidenceThreshold = 0.75
                },
                ["ransomware"] = new ThreatPattern
                {
                    Name = "Ransomware",
                    Severity = ThreatSeverity.High,
                    Indicators = new[] { "file_encryption", "ransom_note", "process_injection" },
                    ConfidenceThreshold = 0.85
                },
                ["spyware"] = new ThreatPattern
                {
                    Name = "Spyware",
                    Severity = ThreatSeverity.Medium,
                    Indicators = new[] { "keylogger_behavior", "data_exfiltration", "stealth_operations" },
                    ConfidenceThreshold = 0.70
                },
                ["worm"] = new ThreatPattern
                {
                    Name = "Computer Worm",
                    Severity = ThreatSeverity.High,
                    Indicators = new[] { "self_replication", "network_spreading", "exploit_usage" },
                    ConfidenceThreshold = 0.80
                }
            };
        }

        public async Task<AIThreatAnalysis> AnalyzeFileAsync(string filePath)
        {
            var analysis = new AIThreatAnalysis
            {
                FilePath = filePath,
                Timestamp = DateTime.Now,
                Indicators = new List<string>(),
                ConfidenceScore = 0.0
            };

            try
            {
                // Basic file analysis
                var fileInfo = new FileInfo(filePath);
                analysis.FileSize = fileInfo.Length;

                // Calculate file hash for database lookup
                analysis.FileHash = await CalculateSHA256Async(filePath);

                // Check against known threat database
                var knownThreat = await _threatDb.LookupAsync(analysis.FileHash);
                if (knownThreat != null)
                {
                    analysis.IsThreat = true;
                    analysis.ThreatType = knownThreat.Name;
                    analysis.ConfidenceScore = 0.95; // High confidence for known threats
                    analysis.Indicators.Add("known_malware_signature");
                    return analysis;
                }

                // AI-enhanced heuristic analysis
                var heuristicScore = await PerformHeuristicAnalysisAsync(filePath);
                analysis.ConfidenceScore = heuristicScore;

                // Behavioral analysis simulation
                var behavioralScore = await PerformBehavioralAnalysisAsync(filePath);
                analysis.ConfidenceScore = Math.Max(analysis.ConfidenceScore, behavioralScore);

                // Cloud lookup simulation (would connect to threat intelligence feeds)
                var cloudScore = await PerformCloudLookupAsync(analysis.FileHash);
                analysis.ConfidenceScore = Math.Max(analysis.ConfidenceScore, cloudScore);

                // Determine if it's a threat based on confidence threshold
                analysis.IsThreat = analysis.ConfidenceScore >= 0.60;

                if (analysis.IsThreat)
                {
                    analysis.ThreatType = DetermineThreatType(analysis.Indicators);
                }

                // Add some random indicators for demonstration
                if (analysis.IsThreat)
                {
                    analysis.Indicators.AddRange(GetRandomIndicators());
                }

            }
            catch (Exception ex)
            {
                LogService.Instance.AddError($"AI analysis failed for {filePath}: {ex.Message}");
                analysis.IsThreat = false;
                analysis.ConfidenceScore = 0.0;
            }

            return analysis;
        }

        private async Task<double> PerformHeuristicAnalysisAsync(string filePath)
        {
            await Task.Delay(10); // Simulate processing time

            double score = 0.0;
            var indicators = new List<string>();

            try
            {
                // Check file extension against known suspicious extensions
                string extension = Path.GetExtension(filePath).ToLower();
                if (new[] { ".exe", ".dll", ".scr", ".pif", ".com" }.Contains(extension))
                {
                    // Analyze PE file characteristics
                    var peAnalysis = await AnalyzePEFileAsync(filePath);
                    score += peAnalysis.Score;
                    indicators.AddRange(peAnalysis.Indicators);
                }

                // Entropy analysis for packed/encrypted files
                var entropy = await CalculateFileEntropyAsync(filePath);
                if (entropy > 7.5) // High entropy indicates possible packing
                {
                    score += 0.3;
                    indicators.Add("high_entropy_packed_file");
                }

                // Check for suspicious strings
                var suspiciousStrings = await ScanForSuspiciousStringsAsync(filePath);
                if (suspiciousStrings.Any())
                {
                    score += 0.2;
                    indicators.Add("suspicious_strings_detected");
                }

                // Size-based heuristics
                var fileSize = new FileInfo(filePath).Length;
                if (fileSize < 100) // Too small files might be droppers
                {
                    score += 0.1;
                    indicators.Add("unusually_small_file");
                }
                else if (fileSize > 100 * 1024 * 1024) // Very large files
                {
                    score += 0.1;
                    indicators.Add("unusually_large_file");
                }

            }
            catch (Exception ex)
            {
                LogService.Instance.AddError($"Heuristic analysis failed: {ex.Message}");
            }

            return Math.Min(score, 1.0);
        }

        private async Task<double> PerformBehavioralAnalysisAsync(string filePath)
        {
            await Task.Delay(15); // Simulate processing time

            // This would normally analyze file behavior in sandbox
            // For demo purposes, return a random low score
            return _random.NextDouble() * 0.3;
        }

        private async Task<double> PerformCloudLookupAsync(string hash)
        {
            await Task.Delay(20); // Simulate network call

            // This would normally query threat intelligence feeds
            // For demo purposes, occasionally return high confidence
            return _random.NextDouble() < 0.05 ? 0.9 : 0.0;
        }

        private async Task<PEAnalysisResult> AnalyzePEFileAsync(string filePath)
        {
            var result = new PEAnalysisResult();

            try
            {
                using (var stream = File.OpenRead(filePath))
                using (var reader = new BinaryReader(stream))
                {
                    // Check for MZ header
                    if (reader.ReadUInt16() != 0x5A4D) // "MZ"
                        return result;

                    // Skip to PE header
                    stream.Seek(0x3C, SeekOrigin.Begin);
                    var peOffset = reader.ReadUInt32();
                    stream.Seek(peOffset, SeekOrigin.Begin);

                    if (reader.ReadUInt32() != 0x4550) // "PE\0\0"
                        return result;

                    // Read COFF header
                    var machine = reader.ReadUInt16();
                    var numberOfSections = reader.ReadUInt16();

                    // Suspicious indicators
                    if (numberOfSections > 20)
                    {
                        result.Score += 0.2;
                        result.Indicators.Add("many_sections");
                    }

                    // Check for suspicious imports (simplified)
                    // In a real implementation, this would parse the import table
                    result.Score += 0.1; // Base suspicion for executable
                    result.Indicators.Add("executable_file");
                }
            }
            catch
            {
                // Not a valid PE file
            }

            return result;
        }

        private async Task<double> CalculateFileEntropyAsync(string filePath)
        {
            const int bufferSize = 4096;
            var frequencies = new double[256];

            using (var stream = File.OpenRead(filePath))
            {
                var buffer = new byte[bufferSize];
                int bytesRead;
                long totalBytes = 0;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, bufferSize)) > 0)
                {
                    for (int i = 0; i < bytesRead; i++)
                    {
                        frequencies[buffer[i]]++;
                    }
                    totalBytes += bytesRead;
                }

                if (totalBytes == 0) return 0;

                double entropy = 0;
                for (int i = 0; i < 256; i++)
                {
                    if (frequencies[i] > 0)
                    {
                        double p = frequencies[i] / totalBytes;
                        entropy -= p * Math.Log(p, 2);
                    }
                }

                return entropy;
            }
        }

        private async Task<List<string>> ScanForSuspiciousStringsAsync(string filePath)
        {
            var suspiciousStrings = new[]
            {
                "CreateRemoteThread", "WriteProcessMemory", "VirtualAllocEx",
                "RegCreateKeyEx", "RegSetValueEx", "InternetOpen",
                "cmd.exe", "powershell.exe", "rundll32.exe"
            };

            var found = new List<string>();

            try
            {
                using (var stream = File.OpenRead(filePath))
                using (var reader = new StreamReader(stream))
                {
                    var content = await reader.ReadToEndAsync();
                    foreach (var suspicious in suspiciousStrings)
                    {
                        if (content.Contains(suspicious, StringComparison.OrdinalIgnoreCase))
                        {
                            found.Add(suspicious);
                        }
                    }
                }
            }
            catch
            {
                // Binary file or access error
            }

            return found;
        }

        private async Task<string> CalculateSHA256Async(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = await sha256.ComputeHashAsync(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private string DetermineThreatType(List<string> indicators)
        {
            // Simple rule-based classification
            if (indicators.Contains("file_encryption") || indicators.Contains("ransom_note"))
                return "Ransomware";
            if (indicators.Contains("keylogger_behavior") || indicators.Contains("data_exfiltration"))
                return "Spyware";
            if (indicators.Contains("self_replication") || indicators.Contains("network_spreading"))
                return "Worm";
            if (indicators.Contains("process_injection") || indicators.Contains("suspicious_imports"))
                return "Trojan";

            return "Unknown Malware";
        }

        private List<string> GetRandomIndicators()
        {
            var allIndicators = new[]
            {
                "suspicious_imports", "registry_modifications", "network_connections",
                "file_encryption", "ransom_note", "process_injection",
                "keylogger_behavior", "data_exfiltration", "stealth_operations",
                "self_replication", "network_spreading", "exploit_usage",
                "high_entropy_packed_file", "suspicious_strings_detected"
            };

            var count = _random.Next(1, 4);
            return allIndicators.OrderBy(x => _random.Next()).Take(count).ToList();
        }

        private class ThreatPattern
        {
            public string Name { get; set; }
            public ThreatSeverity Severity { get; set; }
            public string[] Indicators { get; set; }
            public double ConfidenceThreshold { get; set; }
        }

        private class PEAnalysisResult
        {
            public double Score { get; set; } = 0.0;
            public List<string> Indicators { get; set; } = new List<string>();
        }
    }

    public class AIThreatAnalysis
    {
        public string FilePath { get; set; }
        public string FileHash { get; set; }
        public long FileSize { get; set; }
        public bool IsThreat { get; set; }
        public string ThreatType { get; set; }
        public double ConfidenceScore { get; set; }
        public List<string> Indicators { get; set; }
        public DateTime Timestamp { get; set; }
    }
}