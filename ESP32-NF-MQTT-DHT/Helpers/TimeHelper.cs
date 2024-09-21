namespace ESP32_NF_MQTT_DHT.Helpers
{
    using System;

    /// <summary>
    /// Helper class for time-related functionalities.
    /// </summary>
    public static class TimeHelper
    {
        /// <summary>
        /// Gets the current timestamp in "dd-MM-yyyy HH:mm:ss" format.
        /// </summary>
        /// <returns>A string representing the current timestamp.</returns>
        public static string GetCurrentTimestamp()
        {
            return DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss");
        }
    }
}
