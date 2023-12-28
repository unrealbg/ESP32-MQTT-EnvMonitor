namespace ESP32_NF_MQTT_DHT.Models
{
    /// <summary>
    /// Represents a sensor device, encapsulating its data.
    /// </summary>
    public class Sensor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Sensor"/> class.
        /// </summary>
        public Sensor()
        {
            Data = new Data();
        }

        /// <summary>
        /// Gets or sets the data collected by the sensor.
        /// </summary>
        /// <value>
        /// The <see cref="Data"/> object that holds the data collected by the sensor.
        /// </value>
        public Data Data { get; set; }
    }
}
