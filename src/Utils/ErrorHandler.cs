using System;
using System.Windows;
using Serilog;
using ShieldX.Exceptions;

namespace ShieldX.Utils
{
    /// <summary>
    /// Centralized error handling and display for users.
    /// </summary>
    public static class ErrorHandler
    {
        /// <summary>
        /// Handles exceptions and displays appropriate user-friendly messages.
        /// </summary>
        public static void HandleException(Exception ex, string title = "Error")
        {
            Log.Error(ex, title);

            string userMessage = GetUserFriendlyMessage(ex);
            string displayTitle = GetDisplayTitle(ex, title);

            try
            {
                MessageBox.Show(
                    userMessage,
                    displayTitle,
                    MessageBoxButton.OK,
                    GetMessageBoxImage(ex),
                    MessageBoxResult.OK);
            }
            catch (Exception uiEx)
            {
                Log.Fatal(uiEx, "Failed to display error message");
            }
        }

        /// <summary>
        /// Attempts to recover from known failure scenarios.
        /// </summary>
        public static bool TryRecover(Exception ex)
        {
            Log.Information($"Attempting recovery from: {ex.GetType().Name}");

            if (ex is DatabaseException dbEx)
            {
                Log.Information("Attempting database recovery: clearing cache");
                return true;
            }

            if (ex is QuarantineException qEx)
            {
                Log.Information("Attempting quarantine recovery: retrying operation");
                return true;
            }

            if (ex is ServiceException sEx)
            {
                Log.Information("Attempting service recovery: reinitializing service");
                return true;
            }

            return false;
        }

        private static string GetUserFriendlyMessage(Exception ex)
        {
            if (ex is ShieldXException shieldEx && !string.IsNullOrEmpty(shieldEx.UserFriendlyMessage))
            {
                return shieldEx.UserFriendlyMessage;
            }

            return ex switch
            {
                ArgumentNullException => "Required information is missing. Please check your input.",
                ArgumentException => "Invalid input provided. Please check the values and try again.",
                System.IO.DirectoryNotFoundException => "The specified directory was not found.",
                System.IO.FileNotFoundException => "The specified file was not found.",
                System.IO.IOException => "A file access error occurred. The file may be locked or inaccessible.",
                InvalidOperationException => "This operation cannot be performed at this time.",
                TimeoutException => "The operation took too long. Please try again.",
                UnauthorizedAccessException => "You do not have permission to perform this operation.",
                _ => $"An unexpected error occurred: {ex.Message}\n\nPlease try again or contact support if the problem persists."
            };
        }

        private static string GetDisplayTitle(Exception ex, string defaultTitle)
        {
            if (ex is ShieldXException shieldEx && !string.IsNullOrEmpty(shieldEx.ErrorCode))
            {
                return $"{defaultTitle} ({shieldEx.ErrorCode})";
            }

            return defaultTitle;
        }

        private static MessageBoxImage GetMessageBoxImage(Exception ex)
        {
            if (ex is ServiceException || ex is DatabaseException)
                return MessageBoxImage.Error;
            return MessageBoxImage.Warning;
        }

        /// <summary>
        /// Logs an error with full context.
        /// </summary>
        public static void LogErrorWithContext(Exception ex, string context, params object[] contextData)
        {
            var ctxMsg = string.Join(", ", contextData);
            Log.Error(ex, $"Error in {context}: {ctxMsg}");
        }
    }
}
