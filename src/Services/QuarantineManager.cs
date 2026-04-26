using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ShieldX.Models;
using ShieldX.Services;

namespace ShieldX.Services
{
    public class QuarantineManager
    {
        private const string VaultPath = @"C:\ProgramData\ShieldX\Vault\";
        private static readonly byte[] EncryptionKey = GenerateOrLoadKey();
        private static readonly HashSet<string> LoggedErrors = new(StringComparer.OrdinalIgnoreCase);

        // Exclusion list to prevent self-scanning
        private static readonly string[] DefaultExclusions =
        {
            Process.GetCurrentProcess().MainModule?.FileName ?? "",
            Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName ?? "") ?? "",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShieldX"),
            Path.GetTempPath()
        };

        private static byte[] GenerateOrLoadKey()
        {
            // In production, use DPAPI or secure key storage
            // For demo, generate a fixed key from machine info
            using var sha256 = SHA256.Create();
            var machineId = Environment.MachineName + Environment.UserName;
            return sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(machineId));
        }

        public static async Task QuarantineAsync(string filePath, string threatName)
        {
            try
            {
                // Check for file existence and exclusions first
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    Serilog.Log.Information($"File no longer exists, skipping quarantine: {filePath}");
                    return;
                }

                if (ShouldExclude(filePath))
                {
                    Serilog.Log.Warning($"File is excluded from quarantine: {filePath}");
                    return;
                }

                // Attempt quarantine with retry logic
                int maxRetries = 3;
                for (int i = 0; i < maxRetries; i++)
                {
                    if (IsFileLocked(filePath))
                    {
                        if (i < maxRetries - 1)
                        {
                            await Task.Delay(500 * (i + 1)); // Exponential backoff: 500ms, 1s, 1.5s
                            continue;
                        }
                        else
                        {
                            LogErrorOnce(filePath, $"File is locked and cannot be quarantined: {filePath}");
                            return; // Don't throw, just skip
                        }
                    }

                    try
                    {
                        Directory.CreateDirectory(VaultPath);
                        string quarantineId = Guid.NewGuid().ToString();
                        string vaultFile = Path.Combine(VaultPath, $"{quarantineId}.qvault");

                        // Calculate hash before quarantining
                        string sha256 = await ComputeSHA256Async(filePath);

                        // Encrypt file with AES-256
                        using var aes = Aes.Create();
                        aes.Key = EncryptionKey;
                        aes.GenerateIV();

                        using var outputStream = File.Create(vaultFile);
                        await outputStream.WriteAsync(aes.IV); // Store IV first

                        using var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
                        using var inputStream = File.OpenRead(filePath);
                        await inputStream.CopyToAsync(cryptoStream);

                        // Record in database
                        var item = new QuarantineItem
                        {
                            Id = quarantineId,
                            ThreatName = threatName,
                            OriginalPath = filePath,
                            VaultPath = vaultFile,
                            DateIsolated = DateTime.Now,
                            FileSize = new FileInfo(vaultFile).Length,
                            SHA256Hash = sha256,
                            Status = "Quarantined"
                        };

                        await DatabaseService.Instance.InsertQuarantineItemAsync(item);
                        AppState.Instance.QuarantinedCount++;

                        // Clean up hash set after successful quarantine
                        LoggedErrors.Remove(filePath);

                        // Delete original file
                        if (File.Exists(filePath))
                        {
                            try
                            {
                                File.Delete(filePath);
                            }
                            catch
                            {
                                // Original file deletion failed, but file is in vault (safe)
                            }
                        }

                        LogService.Instance.AddWarning($"File quarantined: {filePath}", threatName);
                        Serilog.Log.Information($"File quarantined: {filePath} -> {threatName}");
                        return; // Success, exit retry loop
                    }
                    catch (IOException) when (i < maxRetries - 1)
                    {
                        await Task.Delay(500 * (i + 1)); // Retry with backoff
                    }
                    catch (UnauthorizedAccessException)
                    {
                        LogErrorOnce(filePath, $"No permission to quarantine: {filePath}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                LogErrorOnce(filePath, $"Failed to quarantine: {filePath} - {ex.Message}");
            }
        }

        private static bool IsFileLocked(string filePath)
        {
            try
            {
                // Try to open the file for exclusive access
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        private static bool ShouldExclude(string path)
        {
            return DefaultExclusions.Any(exclusion =>
                !string.IsNullOrEmpty(exclusion) &&
                path.StartsWith(exclusion, StringComparison.OrdinalIgnoreCase));
        }

        private static void LogErrorOnce(string filePath, string message)
        {
            if (LoggedErrors.Add(filePath)) // HashSet.Add returns false if already exists
            {
                LogService.Instance.AddError(message, "Quarantine");
                Serilog.Log.Error(message);
            }
        }


        public static async Task RestoreAsync(string quarantineId)
        {
            try
            {
                var items = await DatabaseService.Instance.GetQuarantineItemsAsync();
                var item = items.FirstOrDefault(i => i.Id == quarantineId);

                if (item == null)
                    throw new InvalidOperationException("Quarantine item not found");

                string vaultFile = item.VaultPath;

                if (!File.Exists(vaultFile))
                    throw new FileNotFoundException("Vault file not found");

                // Decrypt and restore
                using var inputStream = File.OpenRead(vaultFile);
                byte[] iv = new byte[16];
                await inputStream.ReadAsync(iv);

                using var aes = Aes.Create();
                aes.Key = EncryptionKey;
                aes.IV = iv;

                using var cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var outputStream = File.Create(item.OriginalPath);
                await cryptoStream.CopyToAsync(outputStream);

                // Update database
                item.Status = "Restored";
                item.RestoredAt = DateTime.Now;

                // Delete vault file
                if (File.Exists(vaultFile))
                    File.Delete(vaultFile);

                AppState.Instance.QuarantinedCount--;
                LogService.Instance.AddInfo($"Quarantine restored: {item.OriginalPath}");
                Serilog.Log.Information($"File restored: {item.OriginalPath}");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, $"Failed to restore {quarantineId}");
                throw;
            }
        }

        public static async Task DeleteAsync(string quarantineId)
        {
            try
            {
                var items = await DatabaseService.Instance.GetQuarantineItemsAsync();
                var item = items.FirstOrDefault(i => i.Id == quarantineId);

                if (item == null)
                    throw new InvalidOperationException("Quarantine item not found");

                // Delete vault file
                if (File.Exists(item.VaultPath))
                    File.Delete(item.VaultPath);

                // Update database
                item.Status = "Deleted";
                item.DeletedAt = DateTime.Now;

                AppState.Instance.QuarantinedCount--;
                LogService.Instance.AddInfo($"Quarantine deleted: {item.OriginalPath}");
                Serilog.Log.Information($"Quarantined file deleted: {item.OriginalPath}");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, $"Failed to delete {quarantineId}");
                throw;
            }
        }

        private static async Task<string> ComputeSHA256Async(string path)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(path);
            var hash = await Task.Run(() => sha256.ComputeHash(stream));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}