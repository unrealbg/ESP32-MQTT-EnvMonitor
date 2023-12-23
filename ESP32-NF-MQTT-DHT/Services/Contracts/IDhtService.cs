namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    /// <summary>
    /// Defines a contract for a service that interacts with a DHT (Digital Humidity and Temperature) sensor.
    /// </summary>
    public interface IDhtService
    {
        /// <summary>
        /// Starts the service for reading and processing data from a DHT sensor.
        /// </summary>
        /// <remarks>
        /// This method should initialize the sensor and begin a continuous process
        /// of reading and handling sensor data, typically in a loop or via periodic updates.
        /// </remarks>
        void Start();

        /// <summary>
        /// Retrieves the current temperature reading from the DHT sensor.
        /// </summary>
        /// <returns>
        /// A string representing the current temperature reading.
        /// </returns>
        /// <remarks>
        /// This method should extract the temperature data from the sensor
        /// and format it as a string for easy display or logging.
        /// </remarks>
        string GetTemp();

        /// <summary>
        /// Retrieves the current humidity reading from the DHT sensor.
        /// </summary>
        /// <returns>
        /// A string representing the current humidity reading.
        /// </returns>
        /// <remarks>
        /// This method should extract the humidity data from the sensor
        /// and format it as a string for easy display or logging.
        /// </remarks>
        string GetHumidity();

        /// <summary>
        /// Initiates the process of gathering sensor data from the DHT sensor.
        /// </summary>
        /// <remarks>
        /// This method should trigger a sequence of actions to collect both
        /// temperature and humidity data from the DHT sensor.
        /// The collected data can then be used for further processing or analysis.
        /// </remarks>
        void GetData();
    }
}
