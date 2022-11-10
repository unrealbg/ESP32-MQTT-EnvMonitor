namespace ESP32_NF_MQTT_DHT.Models
{
    public class Sensor
    {
        public Sensor()
        {
            this.Data = new Data();
        }

        public Data Data { get; set; }
    }
}
