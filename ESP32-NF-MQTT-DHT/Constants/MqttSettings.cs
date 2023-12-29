namespace ESP32_NF_MQTT_DHT.Constants
{
    using System;

    public static class MqttSettings
    {
        public const string Broker = "test.mosquitto.org";
        public const string ClientUsername = "username";
        public const string ClientPassword = "password";

        public static readonly string ClientId = Guid.NewGuid().ToString();
    }
}