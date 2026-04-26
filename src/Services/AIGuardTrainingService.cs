using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ShieldX.Models;

namespace ShieldX.Services
{
    public class AIGuardTrainingService
    {
        private static readonly Lazy<AIGuardTrainingService> _instance = new(() => new AIGuardTrainingService());
        public static AIGuardTrainingService Instance => _instance.Value;

        private readonly string _trainingPath;
        private readonly object _lock = new();

        private AIGuardTrainingService()
        {
            _trainingPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ShieldX", "TrainingSamples");

            try
            {
                Directory.CreateDirectory(_trainingPath);
            }
            catch { }
        }

        public void SaveFalsePositive(ProcessSnapshot snapshot)
        {
            try
            {
                lock (_lock)
                {
                    var falsePositiveFile = Path.Combine(_trainingPath, "false_positives.json");
                    var falsePositives = LoadFalsePositives();

                    falsePositives.Add(new
                    {
                        timestamp = DateTime.Now,
                        processName = snapshot.Name,
                        fullPath = snapshot.FullPath,
                        commandLine = snapshot.CommandLine,
                        signerName = snapshot.SignerName,
                        isSigned = snapshot.IsSigned
                    });

                    var json = JsonSerializer.Serialize(falsePositives, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(falsePositiveFile, json);

                    LogService.Instance.AddInfo($"Marked {snapshot.Name} as false positive", "AIGuard");
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.AddError($"Error saving false positive: {ex.Message}", "AIGuard");
            }
        }

        public void SaveConfirmedThreat(ProcessSnapshot snapshot, string label)
        {
            try
            {
                lock (_lock)
                {
                    var threatSampleFile = Path.Combine(_trainingPath,
                        $"threat_{label}_{DateTime.Now:yyyyMMdd_HHmmss}.json");

                    var sampleData = new
                    {
                        timestamp = DateTime.Now,
                        label = label,
                        processName = snapshot.Name,
                        fullPath = snapshot.FullPath,
                        commandLine = snapshot.CommandLine,
                        signerName = snapshot.SignerName,
                        isSigned = snapshot.IsSigned,
                        cpuPercent = snapshot.CpuPercent,
                        ramMb = snapshot.RamMb,
                        threadCount = snapshot.ThreadCount,
                        tcpConnectionCount = snapshot.TcpConnections.Count
                    };

                    var json = JsonSerializer.Serialize(sampleData, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(threatSampleFile, json);

                    LogService.Instance.AddInfo($"Saved confirmed threat sample: {label}", "AIGuard");
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.AddError($"Error saving confirmed threat: {ex.Message}", "AIGuard");
            }
        }

        public int GetTrainingSampleCount()
        {
            try
            {
                lock (_lock)
                {
                    if (!Directory.Exists(_trainingPath))
                        return 0;

                    var files = Directory.GetFiles(_trainingPath, "threat_*.json");
                    return files.Length;
                }
            }
            catch
            {
                return 0;
            }
        }

        public void ExportTrainingData(string exportPath)
        {
            try
            {
                lock (_lock)
                {
                    if (!Directory.Exists(_trainingPath))
                    {
                        LogService.Instance.AddWarning("No training data to export", "AIGuard");
                        return;
                    }

                    var files = Directory.GetFiles(_trainingPath, "threat_*.json");
                    var csvLines = new List<string>
                    {
                        "Timestamp,Label,ProcessName,FullPath,CommandLine,SignerName,IsSigned,CPU%,RAM(MB),ThreadCount,TCPConnections"
                    };

                    foreach (var file in files)
                    {
                        try
                        {
                            var json = File.ReadAllText(file);
                            var doc = JsonDocument.Parse(json);
                            var root = doc.RootElement;

                            string timestamp = root.GetProperty("timestamp").GetString();
                            string label = root.GetProperty("label").GetString();
                            string processName = root.GetProperty("processName").GetString();
                            string fullPath = root.GetProperty("fullPath").GetString();
                            string commandLine = root.GetProperty("commandLine").GetString();
                            string signerName = root.GetProperty("signerName").GetString();
                            bool isSigned = root.GetProperty("isSigned").GetBoolean();
                            double cpu = root.GetProperty("cpuPercent").GetDouble();
                            long ram = root.GetProperty("ramMb").GetInt64();
                            int threads = root.GetProperty("threadCount").GetInt32();
                            int tcp = root.GetProperty("tcpConnectionCount").GetInt32();

                            // Escape CSV fields
                            string EscapeCsv(string value) => $"\"{value?.Replace("\"", "\"\"")}\"";

                            csvLines.Add(
                                $"{timestamp},{EscapeCsv(label)},{EscapeCsv(processName)},{EscapeCsv(fullPath)}," +
                                $"{EscapeCsv(commandLine)},{EscapeCsv(signerName)},{isSigned},{cpu:F2},{ram},{threads},{tcp}");
                        }
                        catch { }
                    }

                    File.WriteAllLines(exportPath, csvLines);
                    LogService.Instance.AddInfo($"Exported training data to: {exportPath}", "AIGuard");
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.AddError($"Error exporting training data: {ex.Message}", "AIGuard");
            }
        }

        private List<object> LoadFalsePositives()
        {
            try
            {
                var falsePositiveFile = Path.Combine(_trainingPath, "false_positives.json");
                if (!File.Exists(falsePositiveFile))
                    return new List<object>();

                var json = File.ReadAllText(falsePositiveFile);
                return JsonSerializer.Deserialize<List<object>>(json) ?? new List<object>();
            }
            catch
            {
                return new List<object>();
            }
        }
    }
}
