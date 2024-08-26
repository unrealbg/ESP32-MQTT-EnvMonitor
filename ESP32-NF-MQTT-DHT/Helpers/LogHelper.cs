namespace ESP32_NF_MQTT_DHT.Helpers
{
    using Microsoft.Extensions.Logging;

    using static TimeHelper;

    public class LogHelper
    {
        private readonly ILogger _logger;

        public LogHelper(ILoggerFactory loggerFactory, string loggerName)
        {
            _logger = loggerFactory.CreateLogger(loggerName);
        }

        public void LogWithTimestamp(LogLevel level, string message)
        {
            _logger.Log(level, $"[{GetCurrentTimestamp()}] {message}");
        }
    }
}
