namespace ESP32_NF_MQTT_DHT.Settings
{
    /// <summary>
    /// Time/NTP configuration for TLS and logging.
    /// </summary>
    public static class TimeSettings
    {
        /// <summary>NTP server hostname or IP.</summary>
        public static string NtpServer = "pool.ntp.org";

        /// <summary>Time zone offset in minutes from UTC (e.g. EET is +120, EEST +180).</summary>
        public static int TimeZoneMinutes = 120;
    }
}
