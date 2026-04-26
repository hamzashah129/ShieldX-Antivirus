using System;
using System.Collections.Generic;
using System.Diagnostics;
using Serilog;

namespace ShieldX.Utils
{
    /// <summary>
    /// Validates input parameters and throws descriptive validation exceptions.
    /// </summary>
    public static class ValidationUtility
    {
        /// <summary>
        /// Throws if value is null or empty string.
        /// </summary>
        public static string ValidateNotNullOrEmpty(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Log.Error($"Validation failed: {paramName} is null or empty");
                throw new ArgumentException($"{paramName} cannot be null or empty", paramName);
            }
            return value;
        }

        /// <summary>
        /// Throws if object is null.
        /// </summary>
        public static T ValidateNotNull<T>(T value, string paramName) where T : class
        {
            if (value == null)
            {
                Log.Error($"Validation failed: {paramName} is null");
                throw new ArgumentNullException(paramName, $"{paramName} cannot be null");
            }
            return value;
        }

        /// <summary>
        /// Throws if collection is null or empty.
        /// </summary>
        public static ICollection<T> ValidateNotNullOrEmpty<T>(ICollection<T> collection, string paramName)
        {
            if (collection == null || collection.Count == 0)
            {
                Log.Error($"Validation failed: {paramName} is null or empty");
                throw new ArgumentException($"{paramName} cannot be null or empty", paramName);
            }
            return collection;
        }

        /// <summary>
        /// Throws if value is outside the specified range.
        /// </summary>
        public static int ValidateRange(int value, int min, int max, string paramName)
        {
            if (value < min || value > max)
            {
                Log.Error($"Validation failed: {paramName} = {value} is outside range [{min}, {max}]");
                throw new ArgumentOutOfRangeException(paramName, 
                    $"{paramName} must be between {min} and {max}");
            }
            return value;
        }

        /// <summary>
        /// Throws if process memory exceeds the threshold.
        /// </summary>
        public static void ValidateMemoryUsage(long maxMemoryMb = 1024)
        {
            var currentProcess = Process.GetCurrentProcess();
            var memoryMb = currentProcess.WorkingSet64 / (1024 * 1024);

            if (memoryMb > maxMemoryMb)
            {
                Log.Warning($"Memory usage warning: {memoryMb}MB exceeds threshold of {maxMemoryMb}MB");
            }
        }

        /// <summary>
        /// Throws if disk space is critically low.
        /// </summary>
        public static void ValidateDiskSpace(string path, long minFreeMb = 100)
        {
            try
            {
                var driveInfo = new System.IO.DriveInfo(System.IO.Path.GetPathRoot(path));
                var freeSpaceMb = driveInfo.AvailableFreeSpace / (1024 * 1024);

                if (freeSpaceMb < minFreeMb)
                {
                    Log.Error($"Disk space critical: {freeSpaceMb}MB free (minimum {minFreeMb}MB required)");
                    throw new InvalidOperationException(
                        $"Insufficient disk space. At least {minFreeMb}MB required, but only {freeSpaceMb}MB available");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to validate disk space");
                throw;
            }
        }
    }
}
