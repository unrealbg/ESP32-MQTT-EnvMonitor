namespace ESP32_NF_MQTT_DHT.Constants
{
    using System;

    public static class Constants
    {
        public const string Device = "DeviceName";

        public const string Ssid = "MySSID";

        public const string WifiPassword = "password";

        public const string Broker = "test.mosquitto.org";

        public static readonly string ClientId = Guid.NewGuid().ToString();

        public const string MqttClientUsername = "username";

        public const string MqttClientPassword = "password";

        public const string TcpClientUsername = "user";

        public const string TcpClientPassword = "pass";
    }
}
