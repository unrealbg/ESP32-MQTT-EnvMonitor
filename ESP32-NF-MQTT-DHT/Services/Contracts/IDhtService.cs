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
    }
}
