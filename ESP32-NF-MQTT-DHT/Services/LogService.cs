namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;

    public static class LogService
    {
        private const string LogFilePath = "I:\\critical_errors.log";
        private const int MaxLogFileSize = 10 * 1024;
        private const int MaxLogFiles = 3;

        /// <summary>
        /// Logs a critical error message, optionally including details of an exception. The log entry is timestamped
        /// and saved.
        /// </summary>
        /// <param name="message">The main text describing the critical error to be logged.</param>
        /// <param name="ex">An optional exception that provides additional context about the error.</param>
        public static void LogCritical(string message, Exception ex = null)
        {
            try
            {
                string fullMessage = ex != null ? $"{message} | Exception: {ex.Message}" : message;
                string logEntry = $"{DateTime.UtcNow:u} [CRITICAL] {fullMessage}";

                Debug.WriteLine(logEntry);

                SaveLog(logEntry);
            }
            catch (Exception logEx)
            {
                Debug.WriteLine($"[LogService] Failed to log critical error: {logEx.Message}");
            }
        }

        /// <summary>
        /// Reads the latest logs from a specified log file. If the file does not exist, it returns a message indicating
        /// no logs are available.
        /// </summary>
        /// <returns>Returns the content of the log file or an error message if an exception occurs.</returns>
        public static string ReadLatestLogs()
        {
            try
            {
                return File.Exists(LogFilePath) ? File.ReadAllText(LogFilePath) : "No critical logs available.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LogService] Error reading logs: {ex.Message}");
                return "Error reading logs.";
            }
        }

        /// <summary>
        /// Clears log files by deleting a series of log files named 'critical_errors_i.log' and a main log file.
        /// Handles exceptions and logs errors.
        /// </summary>
        public static void ClearLogs()
        {
            try
            {
                for (int i = 1; i <= MaxLogFiles; i++)
                {
                    string file = $"I:\\critical_errors_{i}.log";
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                        Debug.WriteLine($"Deleted: {file}");
                    }
                }

                if (File.Exists(LogFilePath))
                {
                    File.Delete(LogFilePath);
                    Debug.WriteLine("Deleted: critical_errors.log");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LogService] Error clearing logs: {ex.Message}");
            }
        }

        private static void SaveLog(string logEntry)
        {
            try
            {
                RotateLogFiles();

                using (var fs = File.Open(LogFilePath, FileMode.Append))
                using (var writer = new StreamWriter(fs))
                {
                    writer.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LogService] Error saving log: {ex.Message}");
            }
        }

        private static void RotateLogFiles()
        {
            try
            {
                if (File.Exists(LogFilePath) && new FileInfo(LogFilePath).Length > MaxLogFileSize)
                {
                    for (int i = MaxLogFiles - 1; i > 0; i--)
                    {
                        string oldFile = $"I:\\critical_errors_{i}.log";
                        string newFile = $"I:\\critical_errors_{i + 1}.log";

                        if (File.Exists(newFile))
                        {
                            File.Delete(newFile);
                        }

                        if (File.Exists(oldFile))
                        {
                            File.Move(oldFile, newFile);
                        }
                    }

                    string newLogFile = $"I:\\critical_errors_1.log";

                    if (File.Exists(newLogFile))
                    {
                        File.Delete(newLogFile);
                    }

                    File.Move(LogFilePath, newLogFile);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RotateLogFiles] Error: {ex.Message}");
            }
        }
    }
}
