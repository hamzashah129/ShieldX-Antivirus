using System;

namespace ShieldX.Exceptions
{
    /// <summary>
    /// Exception thrown when scan operations fail.
    /// </summary>
    [Serializable]
    public class ScanException : ShieldXException
    {
        public ScanException(string message) 
            : base("SCAN_ERROR", message, $"Scan operation failed: {message}") { }

        public ScanException(string message, Exception innerException)
            : base("SCAN_ERROR", message, innerException, 
                $"Scan operation failed: {message}") { }

        public ScanException(string scanType, string message, Exception innerException = null)
            : base("SCAN_ERROR", $"{scanType} scan failed: {message}", innerException,
                $"The {scanType} scan could not be completed. Please ensure sufficient disk space and try again.") { }
    }
}
