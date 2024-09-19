namespace ESP32_NF_MQTT_DHT.Managers.Contracts
{
    using ESP32_NF_MQTT_DHT.Models;

    public interface ISensorManager
    {
        Device CollectAndCreateSensorData();

        void StartSensor();

        void StopSensor();
    }
}