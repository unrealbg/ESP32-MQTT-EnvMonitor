namespace ESP32_NF_MQTT_DHT.Helpers
{
    using System;

    public static class TimeHelper
    {
        public static string GetCurrentTimestamp()
        {
            return DateTime.UtcNow.ToString("dd-MM-yyy HH:mm:ss");
        }
    }
}
