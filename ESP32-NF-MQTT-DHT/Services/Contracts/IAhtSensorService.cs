namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    public interface IAhtSensorService
    {
        void Start();

        double GetTemp();

        double GetHumidity();
    }
}
