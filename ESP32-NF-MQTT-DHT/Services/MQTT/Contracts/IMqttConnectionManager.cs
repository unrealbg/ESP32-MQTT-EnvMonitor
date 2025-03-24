namespace ESP32_NF_MQTT_DHT.Services.MQTT.Contracts
{
    using nanoFramework.M2Mqtt;

    /// <summary>
    /// Interface for MQTT connection manager
    /// </summary>
    public interface IMqttConnectionManager
    {
        MqttClient MqttClient { get; }

        bool Connect(string broker, string clientId, string username, string password);

        void Disconnect();
    }
}