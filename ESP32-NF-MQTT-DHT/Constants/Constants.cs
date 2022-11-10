namespace ESP32_NF_MQTT_DHT.Constants
{
    using System;

    public static class Constants
    {
        public const string SSID = "MySSID";

        public const string WIFI_PASSWORD = "password";

        public static readonly string BROKER = "test.mosquitto.org";

        public static readonly string CLIENT_ID = Guid.NewGuid().ToString();

        public static readonly string MQTT_CLIENT_USERNAME = "username";

        public static readonly string MQTT_CLIENT_PASSWORD = "password";
    }
}
