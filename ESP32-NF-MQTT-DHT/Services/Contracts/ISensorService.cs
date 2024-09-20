namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    /// <summary>
    /// Interface for a service that manages sensor operations.
    /// </summary>
    public interface ISensorService
    {
        /// <summary>
        /// Starts the sensor service.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the sensor service.
        /// </summary>
        void Stop();

        /// <summary>
        /// Retrieves the sensor data.
        /// </summary>
        /// <returns>An array of doubles containing the sensor data.</returns>
        double[] GetData();

        /// <summary>
        /// Retrieves the temperature reading from the sensor.
        /// </summary>
        /// <returns>The temperature value recorded by the sensor.</returns>
        double GetTemp();

        /// <summary>
        /// Retrieves the humidity reading from the sensor.
        /// </summary>
        /// <returns>The humidity value recorded by the sensor.</returns>
        double GetHumidity();

        /// <summary>
        /// Retrieves the type of the sensor.
        /// </summary>
        /// <returns>A string representing the type of the sensor.</returns>
        string GetSensorType();
    }
}
