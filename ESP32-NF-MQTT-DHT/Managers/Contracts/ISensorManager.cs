namespace ESP32_NF_MQTT_DHT.Managers.Contracts
{
    using ESP32_NF_MQTT_DHT.Models;

    /// <summary>
    /// Interface for managing sensor operations including data collection and validation.
    /// </summary>
    public interface ISensorManager
    {
        /// <summary>
        /// Collects sensor data and creates a <see cref="Device"/> object with the collected data.
        /// </summary>
        /// <returns>A <see cref="Device"/> object containing the sensor data, or <c>null</c> if the data is invalid.</returns>
        Device CollectAndCreateSensorData();

        /// <summary>
        /// Starts the sensor service.
        /// </summary>
        void StartSensor();

        /// <summary>
        /// Stops the sensor service.
        /// </summary>
        void StopSensor();
    }
}