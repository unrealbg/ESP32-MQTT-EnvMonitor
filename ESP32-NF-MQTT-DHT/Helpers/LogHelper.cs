namespace ESP32_NF_MQTT_DHT.Helpers
{
#if DEBUG
    using Microsoft.Extensions.Logging;
    using nanoFramework.Logging.Debug;
#endif
    using System;

    /// <summary>
    /// Helper class for logging messages.
    /// </summary>
    public static class LogHelper
    {
#if DEBUG
        private static DebugLogger _logger = new DebugLogger("GlobalLogger");
#endif

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogInformation(string message)
        {
#if DEBUG
            _logger.LogInformation(FormatMessage("INFO", message));
#endif
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">The exception, if any.</param>
        public static void LogError(string message, Exception ex = null)
        {
#if DEBUG
            string formattedMessage = ex != null
                ? $"{FormatMessage("ERROR", message)} | Exception: {ex}"
                : FormatMessage("ERROR", message);

            _logger.LogError(formattedMessage);
#endif
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogWarning(string message)
        {
#if DEBUG
            _logger.LogWarning(FormatMessage("WARNING", message));
#endif
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogDebug(string message)
        {
#if DEBUG
            _logger.LogDebug(FormatMessage("DEBUG", message));
#endif
        }

#if DEBUG
        private static string FormatMessage(string level, string message)
        {
            string color = level switch
            {
                "INFO" => "\u001b[32m",
                "WARNING" => "\u001b[33m",
                "ERROR" => "\u001b[31m",
                _ => "\u001b[0m"
            };

            return $"[{DateTime.UtcNow:dd-MM-yyyy HH:mm:ss}] {color}[{level}]\u001b[0m {message}";
        }
#endif
    }
}
