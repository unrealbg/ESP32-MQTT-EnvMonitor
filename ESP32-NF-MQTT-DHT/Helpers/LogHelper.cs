namespace ESP32_NF_MQTT_DHT.Helpers
{
    using System;
    using System.Diagnostics;

    using static TimeHelper;

    /// <summary>
    /// Helper class for logging messages with timestamps.
    /// </summary>
    public class LogHelper
    {
        /// <summary>
        /// Logs a message with the current timestamp.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogWithTimestamp(string message)
        {
            Debug.WriteLine($"[{GetCurrentTimestamp()}] {message}");
        }

        /// <summary>
        /// Logs an exception and a message with the current timestamp.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        public void LogWithTimestamp(Exception ex, string message)
        {
            Debug.WriteLine($"{GetCurrentTimestamp()} - {ex} - {message}");
        }
    }
}
