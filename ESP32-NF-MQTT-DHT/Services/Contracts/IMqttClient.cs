namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    using nanoFramework.M2Mqtt;

    public interface IMqttClient
    {
        MqttClient MqttClient { get; }

        void Start();
    }
}
