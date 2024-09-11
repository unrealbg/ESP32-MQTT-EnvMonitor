namespace ESP32_NF_MQTT_DHT.Helpers
{
    using System;
    using System.Diagnostics;


    using static TimeHelper;

    public class LogHelper
    {
        public void LogWithTimestamp(string message)
        {
            Debug.WriteLine($"[{GetCurrentTimestamp()}] {message}");
        }

        public void LogWithTimestamp(Exception ex, string message)
        {
            Debug.WriteLine($"{GetCurrentTimestamp()} - {ex} - {message}");
        }
    }
}
