using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using ShieldX.Models;

namespace ShieldX.Services
{
    /// <summary>
    /// Manages password vault file operations with encryption
    /// </summary>
    public class VaultService
    {
        private static readonly string VaultDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ShieldX");

        private static readonly string VaultFilePath = Path.Combine(VaultDirectory, "vault.shx");

        private byte[] _masterKey;
        private bool _isUnlocked = false;

        public bool IsUnlocked => _isUnlocked;
        public string VaultPath => VaultFilePath;

        /// <summary>
        /// Initializes the vault directory if it doesn't exist
        /// </summary>
        public static void InitializeVaultDirectory()
        {
            if (!Directory.Exists(VaultDirectory))
            {
                Directory.CreateDirectory(VaultDirectory);
            }
        }

        /// <summary>
        /// Checks if vault exists and has a master password set
        /// </summary>
        public static bool VaultExists()
        {
            return File.Exists(VaultFilePath);
        }

        /// <summary>
        /// Creates a new vault with master password
        /// </summary>
        public bool CreateVault(string masterPassword)
        {
            if (string.IsNullOrEmpty(masterPassword))
                throw new ArgumentNullException(nameof(masterPassword));

            if (VaultExists())
                throw new InvalidOperationException("Vault already exists");

            InitializeVaultDirectory();

            try
            {
                // Generate salt and hash the master password
                byte[] salt = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }

                string passwordHash = EncryptionService.HashPassword(masterPassword);
                _masterKey = EncryptionService.DeriveKeyFromPassword(masterPassword, salt);

                // Create empty vault structure
                var vaultData = new VaultFileData
                {
                    Version = 1,
                    MasterPasswordHash = passwordHash,
                    Salt = Convert.ToBase64String(salt),
                    Entries = new List<EncryptedVaultEntry>(),
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                };

                // Save empty vault
                string json = JsonSerializer.Serialize(vaultData);
                File.WriteAllText(VaultFilePath, json);

                _isUnlocked = true;
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create vault", ex);
            }
        }

        /// <summary>
        /// Unlocks the vault with master password
        /// </summary>
        public bool UnlockVault(string masterPassword)
        {
            if (string.IsNullOrEmpty(masterPassword))
                throw new ArgumentNullException(nameof(masterPassword));

            if (!VaultExists())
                throw new FileNotFoundException("Vault file not found");

            try
            {
                string json = File.ReadAllText(VaultFilePath);
                var vaultData = JsonSerializer.Deserialize<VaultFileData>(json);

                // Verify master password
                if (!EncryptionService.VerifyPassword(masterPassword, vaultData.MasterPasswordHash))
                    return false;

                // Derive key from password and stored salt
                byte[] salt = Convert.FromBase64String(vaultData.Salt);
                _masterKey = EncryptionService.DeriveKeyFromPassword(masterPassword, salt);

                _isUnlocked = true;
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to unlock vault", ex);
            }
        }

        /// <summary>
        /// Locks the vault (clears master key)
        /// </summary>
        public void LockVault()
        {
            _isUnlocked = false;
            if (_masterKey != null)
            {
                Array.Clear(_masterKey, 0, _masterKey.Length);
                _masterKey = null;
            }
        }

        /// <summary>
        /// Gets all vault entries (decrypted)
        /// </summary>
        public List<VaultEntry> GetAllEntries()
        {
            if (!_isUnlocked)
                throw new InvalidOperationException("Vault is locked");

            try
            {
                string json = File.ReadAllText(VaultFilePath);
                var vaultData = JsonSerializer.Deserialize<VaultFileData>(json);

                var entries = new List<VaultEntry>();
                foreach (var encrypted in vaultData.Entries)
                {
                    var entry = DecryptEntry(encrypted);
                    entries.Add(entry);
                }

                return entries.OrderByDescending(e => e.ModifiedDate).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to read vault entries", ex);
            }
        }

        /// <summary>
        /// Adds a new entry to the vault
        /// </summary>
        public void AddEntry(VaultEntry entry)
        {
            if (!_isUnlocked)
                throw new InvalidOperationException("Vault is locked");

            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (string.IsNullOrEmpty(entry.Id))
                entry.Id = Guid.NewGuid().ToString();

            entry.CreatedDate = DateTime.UtcNow;
            entry.ModifiedDate = DateTime.UtcNow;

            try
            {
                string json = File.ReadAllText(VaultFilePath);
                var vaultData = JsonSerializer.Deserialize<VaultFileData>(json);

                var encrypted = EncryptEntry(entry);
                vaultData.Entries.Add(encrypted);
                vaultData.LastModifiedDate = DateTime.UtcNow;

                string updatedJson = JsonSerializer.Serialize(vaultData);
                File.WriteAllText(VaultFilePath, updatedJson);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to add entry to vault", ex);
            }
        }

        /// <summary>
        /// Updates an existing entry
        /// </summary>
        public void UpdateEntry(VaultEntry entry)
        {
            if (!_isUnlocked)
                throw new InvalidOperationException("Vault is locked");

            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            entry.ModifiedDate = DateTime.UtcNow;

            try
            {
                string json = File.ReadAllText(VaultFilePath);
                var vaultData = JsonSerializer.Deserialize<VaultFileData>(json);

                var index = vaultData.Entries.FindIndex(e => e.Id == entry.Id);
                if (index < 0)
                    throw new InvalidOperationException("Entry not found");

                vaultData.Entries[index] = EncryptEntry(entry);
                vaultData.LastModifiedDate = DateTime.UtcNow;

                string updatedJson = JsonSerializer.Serialize(vaultData);
                File.WriteAllText(VaultFilePath, updatedJson);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to update entry", ex);
            }
        }

        /// <summary>
        /// Deletes an entry from the vault
        /// </summary>
        public void DeleteEntry(string entryId)
        {
            if (!_isUnlocked)
                throw new InvalidOperationException("Vault is locked");

            if (string.IsNullOrEmpty(entryId))
                throw new ArgumentNullException(nameof(entryId));

            try
            {
                string json = File.ReadAllText(VaultFilePath);
                var vaultData = JsonSerializer.Deserialize<VaultFileData>(json);

                vaultData.Entries.RemoveAll(e => e.Id == entryId);
                vaultData.LastModifiedDate = DateTime.UtcNow;

                string updatedJson = JsonSerializer.Serialize(vaultData);
                File.WriteAllText(VaultFilePath, updatedJson);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to delete entry", ex);
            }
        }

        /// <summary>
        /// Encrypts a vault entry
        /// </summary>
        private EncryptedVaultEntry EncryptEntry(VaultEntry entry)
        {
            if (_masterKey == null)
                throw new InvalidOperationException("Master key not available");

            var plainData = new VaultEntryData
            {
                Title = entry.Title,
                Username = entry.Username,
                Password = entry.Password,
                Website = entry.Website,
                Notes = entry.Notes
            };

            string json = JsonSerializer.Serialize(plainData);
            byte[] encryptedData = EncryptionService.EncryptAesGcm(json, _masterKey);

            return new EncryptedVaultEntry
            {
                Id = entry.Id,
                EncryptedData = Convert.ToBase64String(encryptedData),
                CreatedDate = entry.CreatedDate,
                ModifiedDate = entry.ModifiedDate
            };
        }

        /// <summary>
        /// Decrypts a vault entry
        /// </summary>
        private VaultEntry DecryptEntry(EncryptedVaultEntry encrypted)
        {
            if (_masterKey == null)
                throw new InvalidOperationException("Master key not available");

            byte[] encryptedData = Convert.FromBase64String(encrypted.EncryptedData);
            string json = EncryptionService.DecryptAesGcm(encryptedData, _masterKey);

            var plainData = JsonSerializer.Deserialize<VaultEntryData>(json);

            return new VaultEntry
            {
                Id = encrypted.Id,
                Title = plainData.Title,
                Username = plainData.Username,
                Password = plainData.Password,
                Website = plainData.Website,
                Notes = plainData.Notes,
                CreatedDate = encrypted.CreatedDate,
                ModifiedDate = encrypted.ModifiedDate
            };
        }

        /// <summary>
        /// Generates a random password
        /// </summary>
        public static string GeneratePassword(int length = 16, bool useUppercase = true, 
            bool useLowercase = true, bool useNumbers = true, bool useSymbols = true)
        {
            if (length < 1 || length > 256)
                throw new ArgumentException("Length must be between 1 and 256", nameof(length));

            string charset = "";
            if (useLowercase) charset += "abcdefghijklmnopqrstuvwxyz";
            if (useUppercase) charset += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (useNumbers) charset += "0123456789";
            if (useSymbols) charset += "!@#$%^&*()-_=+[]{}|;:,.<>?";

            if (string.IsNullOrEmpty(charset))
                throw new InvalidOperationException("Must select at least one character type");

            byte[] buffer = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }

            var result = new System.Text.StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                result.Append(charset[buffer[i] % charset.Length]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Internal structure for vault file storage
        /// </summary>
        private class VaultFileData
        {
            [JsonPropertyName("version")]
            public int Version { get; set; }

            [JsonPropertyName("masterPasswordHash")]
            public string MasterPasswordHash { get; set; }

            [JsonPropertyName("salt")]
            public string Salt { get; set; }

            [JsonPropertyName("entries")]
            public List<EncryptedVaultEntry> Entries { get; set; }

            [JsonPropertyName("createdDate")]
            public DateTime CreatedDate { get; set; }

            [JsonPropertyName("lastModifiedDate")]
            public DateTime LastModifiedDate { get; set; }
        }

        /// <summary>
        /// Internal structure for encrypted vault entry
        /// </summary>
        private class EncryptedVaultEntry
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("encryptedData")]
            public string EncryptedData { get; set; }

            [JsonPropertyName("createdDate")]
            public DateTime CreatedDate { get; set; }

            [JsonPropertyName("modifiedDate")]
            public DateTime ModifiedDate { get; set; }
        }

        /// <summary>
        /// Internal structure for unencrypted entry data
        /// </summary>
        private class VaultEntryData
        {
            [JsonPropertyName("title")]
            public string Title { get; set; }

            [JsonPropertyName("username")]
            public string Username { get; set; }

            [JsonPropertyName("password")]
            public string Password { get; set; }

            [JsonPropertyName("website")]
            public string Website { get; set; }

            [JsonPropertyName("notes")]
            public string Notes { get; set; }
        }
    }
}
