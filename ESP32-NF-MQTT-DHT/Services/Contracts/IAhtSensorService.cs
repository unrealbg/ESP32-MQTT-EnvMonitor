namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    public interface IAhtSensorService
    {
        void Start();

        void Stop();

        double[] GetData();

        double GetTemp();

        double GetHumidity();
    }
}
