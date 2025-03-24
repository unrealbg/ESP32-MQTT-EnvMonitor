namespace ESP32_NF_MQTT_DHT.Services.MQTT.Contracts
{
    /// <summary>
    /// Interface for sensor data publisher
    /// </summary>
    public interface ISensorDataPublisher
    {
        void Start(int interval);

        void Stop();
    }
}