using System;

namespace ShieldX.Exceptions
{
    /// <summary>
    /// Exception thrown when service initialization or operations fail.
    /// </summary>
    [Serializable]
    public class ServiceException : ShieldXException
    {
        public ServiceException(string serviceName, string message) 
            : base("SERVICE_ERROR", $"Service '{serviceName}' error: {message}",
                $"A critical service failed. Please restart the application.") { }

        public ServiceException(string serviceName, string message, Exception innerException)
            : base("SERVICE_ERROR", $"Service '{serviceName}' error: {message}", innerException,
                $"The {serviceName} service encountered an error. Please restart the application.") { }
    }
}
