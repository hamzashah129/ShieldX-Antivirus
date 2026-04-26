using System;
using System.Security.Cryptography;
using System.Text;

namespace ShieldX.Services
{
    /// <summary>
    /// Provides AES-256-GCM encryption and PBKDF2 password hashing for vault security
    /// </summary>
    public class EncryptionService
    {
        private const int PBKDF2_ITERATIONS = 100000;
        private const int PBKDF2_HASH_SIZE = 32; // 256 bits
        private const int SALT_SIZE = 16; // 128 bits
        private const int GCM_TAG_SIZE = 16; // 128 bits (16 bytes)
        private const int GCM_NONCE_SIZE = 12; // 96 bits (12 bytes) - standard for GCM

        /// <summary>
        /// Derives a master key from password using PBKDF2
        /// </summary>
        public static byte[] DeriveKeyFromPassword(string password, byte[] salt)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));
            if (salt == null || salt.Length == 0)
                throw new ArgumentNullException(nameof(salt));

            using (var pbkdf2 = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(password), salt, PBKDF2_ITERATIONS, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(PBKDF2_HASH_SIZE);
            }
        }

        /// <summary>
        /// Hashes a password for storage verification (PBKDF2 with random salt)
        /// Returns: salt (16 bytes) + hash (32 bytes) = 48 bytes total
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            // Generate random salt
            byte[] salt = new byte[SALT_SIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Derive hash
            byte[] hash = DeriveKeyFromPassword(password, salt);

            // Combine salt + hash and encode as Base64
            byte[] combined = new byte[salt.Length + hash.Length];
            Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
            Buffer.BlockCopy(hash, 0, combined, salt.Length, hash.Length);

            return Convert.ToBase64String(combined);
        }

        /// <summary>
        /// Verifies a password against a stored hash
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            try
            {
                // Decode stored hash
                byte[] combined = Convert.FromBase64String(storedHash);

                // Extract salt and hash
                byte[] salt = new byte[SALT_SIZE];
                byte[] storedHashBytes = new byte[PBKDF2_HASH_SIZE];

                Buffer.BlockCopy(combined, 0, salt, 0, SALT_SIZE);
                Buffer.BlockCopy(combined, SALT_SIZE, storedHashBytes, 0, PBKDF2_HASH_SIZE);

                // Compute hash for provided password
                byte[] computedHash = DeriveKeyFromPassword(password, salt);

                // Constant-time comparison
                return ConstantTimeComparison(computedHash, storedHashBytes);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Encrypts data using AES-256-GCM
        /// Returns: nonce (12 bytes) + ciphertext + tag (16 bytes)
        /// </summary>
        public static byte[] EncryptAesGcm(string plaintext, byte[] key)
        {
            if (string.IsNullOrEmpty(plaintext))
                throw new ArgumentNullException(nameof(plaintext));
            if (key == null || key.Length != 32)
                throw new ArgumentException("Key must be 32 bytes (256 bits)", nameof(key));

            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

            // Generate random nonce
            byte[] nonce = new byte[GCM_NONCE_SIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonce);
            }

            // Encrypt using AES-GCM
            byte[] ciphertext = new byte[plaintextBytes.Length];
            byte[] tag = new byte[GCM_TAG_SIZE];

            using (var aesGcm = new AesGcm(key, GCM_TAG_SIZE))
            {
                aesGcm.Encrypt(nonce, plaintextBytes, null, ciphertext, tag);
            }

            // Combine nonce + ciphertext + tag
            byte[] result = new byte[nonce.Length + ciphertext.Length + tag.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length + ciphertext.Length, tag.Length);

            return result;
        }

        /// <summary>
        /// Decrypts data encrypted with EncryptAesGcm
        /// </summary>
        public static string DecryptAesGcm(byte[] encryptedData, byte[] key)
        {
            if (encryptedData == null || encryptedData.Length < GCM_NONCE_SIZE + GCM_TAG_SIZE)
                throw new ArgumentException("Invalid encrypted data length", nameof(encryptedData));
            if (key == null || key.Length != 32)
                throw new ArgumentException("Key must be 32 bytes (256 bits)", nameof(key));

            try
            {
                // Extract nonce, ciphertext, and tag
                byte[] nonce = new byte[GCM_NONCE_SIZE];
                int ciphertextLength = encryptedData.Length - GCM_NONCE_SIZE - GCM_TAG_SIZE;
                byte[] ciphertext = new byte[ciphertextLength];
                byte[] tag = new byte[GCM_TAG_SIZE];

                Buffer.BlockCopy(encryptedData, 0, nonce, 0, GCM_NONCE_SIZE);
                Buffer.BlockCopy(encryptedData, GCM_NONCE_SIZE, ciphertext, 0, ciphertextLength);
                Buffer.BlockCopy(encryptedData, GCM_NONCE_SIZE + ciphertextLength, tag, 0, GCM_TAG_SIZE);

                // Decrypt using AES-GCM
                byte[] plaintext = new byte[ciphertext.Length];

                using (var aesGcm = new AesGcm(key, GCM_TAG_SIZE))
                {
                    aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
                }

                return Encoding.UTF8.GetString(plaintext);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Decryption failed. Password may be incorrect or data may be corrupted.", ex);
            }
        }

        /// <summary>
        /// Constant-time comparison to prevent timing attacks
        /// </summary>
        private static bool ConstantTimeComparison(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
        }
    }
}
