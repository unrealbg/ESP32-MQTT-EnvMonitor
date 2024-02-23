namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    public interface IShtc3SensorService
    {
        void Start();

        void Stop();

        double[] GetData();

        double GetTemp();

        double GetHumidity();
    }
}