using System;

namespace ShieldX.Exceptions
{
    /// <summary>
    /// Exception thrown when encryption/decryption operations fail.
    /// </summary>
    [Serializable]
    public class EncryptionException : ShieldXException
    {
        public EncryptionException(string message) 
            : base("ENCRYPTION_ERROR", message, "Security operation failed. Your data may be protected.") { }

        public EncryptionException(string message, Exception innerException, string userMessage)
            : base("ENCRYPTION_ERROR", message, innerException, userMessage) { }
    }
}
