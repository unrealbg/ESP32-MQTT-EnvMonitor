namespace ESP32_NF_MQTT_DHT.Models
{
    public class Device
    {
        public Device()
        {
            this.Data = new Data();
        }

        public Data Data { get; set; }
    }
}
