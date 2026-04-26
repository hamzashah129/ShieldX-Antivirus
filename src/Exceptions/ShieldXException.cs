using System;
using System.Runtime.Serialization;

namespace ShieldX.Exceptions
{
    /// <summary>
    /// Base exception class for all ShieldX application exceptions.
    /// </summary>
    [Serializable]
    public class ShieldXException : Exception
    {
        /// <summary>
        /// Gets the error code associated with this exception.
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets additional context information about the error.
        /// </summary>
        public string ErrorContext { get; set; }

        /// <summary>
        /// Gets or sets a user-friendly message for display in UI.
        /// </summary>
        public string UserFriendlyMessage { get; set; }

        public ShieldXException() : base() { }

        public ShieldXException(string message) : base(message) { }

        public ShieldXException(string message, Exception innerException) 
            : base(message, innerException) { }

        public ShieldXException(string errorCode, string message, string userFriendlyMessage = null)
            : base(message)
        {
            ErrorCode = errorCode;
            UserFriendlyMessage = userFriendlyMessage ?? message;
        }

        public ShieldXException(string errorCode, string message, Exception innerException, 
            string userFriendlyMessage = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            UserFriendlyMessage = userFriendlyMessage ?? message;
        }

        protected ShieldXException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
            ErrorCode = info?.GetString(nameof(ErrorCode));
            ErrorContext = info?.GetString(nameof(ErrorContext));
            UserFriendlyMessage = info?.GetString(nameof(UserFriendlyMessage));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info?.AddValue(nameof(ErrorCode), ErrorCode);
            info?.AddValue(nameof(ErrorContext), ErrorContext);
            info?.AddValue(nameof(UserFriendlyMessage), UserFriendlyMessage);
        }

        public override string ToString()
        {
            var msg = base.ToString();
            if (!string.IsNullOrEmpty(ErrorCode))
                msg = $"[{ErrorCode}] {msg}";
            if (!string.IsNullOrEmpty(ErrorContext))
                msg = $"{msg}\nContext: {ErrorContext}";
            return msg;
        }
    }
}
