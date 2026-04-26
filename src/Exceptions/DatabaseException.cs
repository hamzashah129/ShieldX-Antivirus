using System;

namespace ShieldX.Exceptions
{
    /// <summary>
    /// Exception thrown when database operations fail.
    /// </summary>
    [Serializable]
    public class DatabaseException : ShieldXException
    {
        public DatabaseException(string message) 
            : base("DB_ERROR", message, $"Database operation failed: {message}") { }

        public DatabaseException(string message, Exception innerException)
            : base("DB_ERROR", message, innerException, 
                $"Database operation failed: {message}") { }

        public DatabaseException(string operation, string message, Exception innerException = null)
            : base("DB_ERROR", $"Database {operation} failed: {message}", innerException,
                $"A database error occurred during {operation}. Please try again or contact support.") { }
    }
}
