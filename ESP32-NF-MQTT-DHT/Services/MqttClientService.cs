namespace ESP32_NF_MQTT_DHT.Services
{
    using nanoFramework.M2Mqtt;

    using Services.Contracts;

    internal class MqttClientService : IMqttClient
    {
        public MqttClient MqttClient { get; }

        public void Start()
        {
            throw new System.NotImplementedException();
        }
    }
}
