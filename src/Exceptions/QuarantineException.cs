using System;

namespace ShieldX.Exceptions
{
    /// <summary>
    /// Exception thrown when quarantine operations fail.
    /// </summary>
    [Serializable]
    public class QuarantineException : ShieldXException
    {
        public QuarantineException(string message) 
            : base("QUARANTINE_ERROR", message, $"Quarantine operation failed: {message}") { }

        public QuarantineException(string message, Exception innerException)
            : base("QUARANTINE_ERROR", message, innerException, 
                $"Quarantine operation failed: {message}") { }

        public QuarantineException(string operation, string fileName, Exception innerException = null)
            : base("QUARANTINE_ERROR", $"Failed to {operation} file '{fileName}'", innerException,
                $"Could not {operation} the affected file. The file may be in use or access was denied.") { }
    }
}
