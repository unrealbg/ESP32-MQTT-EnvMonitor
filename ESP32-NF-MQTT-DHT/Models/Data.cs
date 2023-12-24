namespace ESP32_NF_MQTT_DHT.Models
{
    using System;

    using Constants;

    /// <summary>
    /// Represents the data collected from the Dht21 sensor.
    /// </summary>
    public class Data
    {
        /// <summary>
        /// Gets the name of the sensor.
        /// </summary>
        public string Sensorname => "Dht21";

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

        /// <summary>
        /// Gets the WiFi SSID that the device is connected to.
        /// </summary>
        /// <value>
        /// The SSID of the connected WiFi network. This value is obtained from the Constants.SSID.
        /// </value>
        public string Wifissid => Constants.Ssid;

        /// <summary>
        /// Gets the firmware release information.
        /// </summary>
        public string Rel => "1.0 Init";
    }
}
