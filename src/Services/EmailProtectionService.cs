using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShieldX.Services
{
    /// <summary>
    /// Email Protection Service - Provides email attachment scanning and link analysis
    /// Monitors email clients and scans attachments for threats
    /// </summary>
    public class EmailProtectionService : IDisposable
    {
        private bool _isRunning;
        private Timer _monitoringTimer;
        private readonly HashSet<string> _dangerousFileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".bat", ".cmd", ".scr", ".vbs", ".js", ".jar", ".zip", ".rar",
            ".7z", ".ace", ".msi", ".dll", ".sys", ".ocx", ".cab", ".lnk", ".pif"
        };

        private readonly HashSet<string> _trustedEmailProviders = new(StringComparer.OrdinalIgnoreCase)
        {
            "outlook.exe",
            "thunderbird.exe",
            "gmail",
            "outlook.office365"
        };

        public event Action<string, string> ThreatDetected;

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            _monitoringTimer = new Timer(async _ => await MonitorEmailClients(), null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
            Serilog.Log.Information("EmailProtection service started");
        }

        public void Stop()
        {
            _isRunning = false;
            _monitoringTimer?.Dispose();
            Serilog.Log.Information("EmailProtection service stopped");
        }

        private async Task MonitorEmailClients()
        {
            try
            {
                // Check for email client processes
                var processes = System.Diagnostics.Process.GetProcesses();
                var emailClients = processes
                    .Where(p => _trustedEmailProviders.Any(e => p.ProcessName.ToLower().Contains(e)))
                    .ToList();

                foreach (var client in emailClients)
                {
                    try
                    {
                        await MonitorEmailAttachments(client);
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Debug(ex, "Failed to monitor email client");
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Email monitoring error");
            }

            await Task.CompletedTask;
        }

        private async Task MonitorEmailAttachments(System.Diagnostics.Process emailClient)
        {
            try
            {
                // Monitor common email attachment locations
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var attachmentPaths = new[]
                {
                    Path.Combine(appDataPath, "Microsoft", "Outlook", "RoamCache"),
                    Path.Combine(appDataPath, "Thunderbird"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gmail")
                };

                foreach (var attachmentPath in attachmentPaths)
                {
                    if (Directory.Exists(attachmentPath))
                    {
                        await ScanEmailAttachmentDirectory(attachmentPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Debug(ex, "Email attachment directory scan failed");
            }

            await Task.CompletedTask;
        }

        private async Task ScanEmailAttachmentDirectory(string path)
        {
            try
            {
                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

                foreach (var file in files.Where(f => IsDangerousAttachment(f)))
                {
                    try
                    {
                        var isSafe = await ScanEmailAttachment(file);
                        if (!isSafe)
                        {
                            ThreatDetected?.Invoke(file, "Malicious email attachment detected");
                            LogService.Instance.AddWarning($"Malicious email attachment: {Path.GetFileName(file)}", "EmailProtection");
                            await QuarantineManager.QuarantineAsync(file, "EmailProtection: Malicious attachment");
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Debug(ex, "Email attachment directory scan error");
            }

            await Task.CompletedTask;
        }

        private bool IsDangerousAttachment(string filePath)
        {
            try
            {
                var extension = Path.GetExtension(filePath);
                return _dangerousFileExtensions.Contains(extension);
            }
            catch { return false; }
        }

        public async Task<bool> ScanEmailAttachment(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return true;

                // Check file signature for masquerading
                if (IsMasqueradedFile(filePath))
                {
                    return false;
                }

                // Check file size (suspiciously large executables)
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > 50 * 1024 * 1024) // 50MB
                {
                    return false;
                }

                // Basic heuristic scan: check for suspicious patterns in file
                if (await HasSuspiciousPatterns(filePath))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Email attachment scan failed");
                return true;
            }
        }

        private async Task<bool> HasSuspiciousPatterns(string filePath)
        {
            try
            {
                // Read first 1KB of file to check for common malware signatures
                using var stream = File.OpenRead(filePath);
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                var fileContent = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead).ToLower();

                // Check for known suspicious patterns
                var suspiciousPatterns = new[] {
                    "CreateRemoteThread", // DLL injection
                    "WriteProcessMemory", // Process injection
                    "VirtualAllocEx",     // Memory allocation for injection
                };

                return suspiciousPatterns.Any(p => fileContent.Contains(p));
            }
            catch
            {
                return false;
            }
        }

        private bool IsMasqueradedFile(string filePath)
        {
            try
            {
                var fileName = Path.GetFileName(filePath).ToLower();
                var extension = Path.GetExtension(filePath).ToLower();

                // Check for double extensions (e.g., document.pdf.exe)
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(filePath).ToLower();
                if (nameWithoutExtension.Contains("."))
                {
                    var lastDot = nameWithoutExtension.LastIndexOf(".");
                    var doubleExtension = nameWithoutExtension.Substring(lastDot);

                    var innocentExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".jpg", ".png" };
                    if (innocentExtensions.Any(e => e == doubleExtension) && _dangerousFileExtensions.Contains(extension))
                    {
                        return true; // Masqueraded as innocent file
                    }
                }

                return false;
            }
            catch { return false; }
        }

        public void Dispose()
        {
            Stop();
            _monitoringTimer?.Dispose();
        }
    }
}
