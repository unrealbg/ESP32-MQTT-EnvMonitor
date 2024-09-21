namespace ESP32_NF_MQTT_DHT.Models
{
    /// <summary>
    /// Represents a microcontroller device, encapsulating its data.
    /// </summary>
    public class Device
    {
        /// <summary>
        /// Gets or sets the name of the device.
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// Gets or sets the location of the device.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Represents the sensor attached to the device.
        /// </summary>
        public string SensorType { get; set; }

        /// <summary>
        /// Gets or sets the date when the data was recorded.
        /// </summary>
        /// <value>
        /// The date of the data recording in "dd/MM/yyyy" format.
        /// </value>
        public string Date { get; set; }

        /// <summary>
        /// Gets or sets the time when the data was recorded.
        /// </summary>
        /// <value>
        /// The time of the data recording in "HH:mm:ss" format.
        /// </value>
        public string Time { get; set; }

        /// <summary>
        /// Gets or sets the temperature reading from the sensor.
        /// </summary>
        /// <value>
        /// The temperature value recorded by the sensor.
        /// </value>
        public double Temp { get; set; }

        /// <summary>
        /// Gets or sets the humidity reading from the sensor.
        /// </summary>
        /// <value>
        /// The humidity value recorded by the sensor, as a percentage.
        /// </value>
        public int Humid { get; set; }
    }
}
