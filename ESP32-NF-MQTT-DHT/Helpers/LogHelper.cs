namespace ESP32_NF_MQTT_DHT.Helpers
{
    using System;

    using Microsoft.Extensions.Logging;

    using nanoFramework.Logging.Debug;

    /// <summary>
    /// Helper class for logging messages with timestamps.
    /// </summary>
    public static class LogHelper
    {
        private static ILoggerFactory _loggerFactory;

        private static ILogger _logger;

        static LogHelper()
        {
            _loggerFactory = new DebugLoggerFactory();
            _logger = _loggerFactory.CreateLogger("GlobalLogger");
        }

        public static ILogger Instance => _logger;

        /// <summary>
        ///  Logs an informational message.
        /// </summary>
        /// <param name="message"></param>
        public static void LogInformation(string message)
        {
            _logger.LogInformation(FormatMessage("INFO", message));
        }

        /// <summary>
        /// Log an error message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public static void LogError(string message, Exception ex = null)
        {
            string formattedMessage = ex != null
                                          ? $"{FormatMessage("ERROR", message)} | Exception: {ex}"
                                          : FormatMessage("ERROR", message);

            _logger.LogError(formattedMessage);
        }

        /// <summary>
        /// Log a warning message.
        /// </summary>
        /// <param name="message"></param>
        public static void LogWarning(string message)
        {
            _logger.LogWarning(FormatMessage("WARNING", message));
        }

        /// <summary>
        /// Log a debug message.
        /// </summary>
        /// <param name="message"></param>
        public static void LogDebug(string message)
        {
            _logger.LogDebug(FormatMessage("DEBUG", message));
        }

        private static string FormatMessage(string level, string message)
        {
            string color = level switch
                {
                    "INFO" => "\u001b[32m",
                    "WARNING" => "\u001b[33m",
                    "ERROR" => "\u001b[31m",
                    _ => "\u001b[0m"
                };

            return $"[{TimeHelper.GetCurrentTimestamp()}]{color} [{level}]\u001b[0m {message}";
        }
    }
}