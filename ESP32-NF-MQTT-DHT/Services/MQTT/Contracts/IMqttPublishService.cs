namespace ESP32_NF_MQTT_DHT.Services.MQTT.Contracts
{
    using nanoFramework.M2Mqtt;

    /// <summary>
    /// Interface for a service that publishes messages to an MQTT broker.
    /// </summary>
    public interface IMqttPublishService
    {
        /// <summary>
        /// Publishes sensor data to the MQTT broker.
        /// </summary>
        void PublishSensorData();

        /// <summary>
        /// Publishes an error message to the MQTT broker.
        /// </summary>
        /// <param name="errorMessage">The error message to be published.</param>
        void PublishError(string errorMessage);

        /// <summary>
        /// Sets the MQTT client to be used for publishing messages.
        /// </summary>
        /// <param name="mqttClient">The MQTT client instance.</param>
        void SetMqttClient(MqttClient mqttClient);

        /// <summary>
        /// Publishes the device status to the MQTT broker.
        /// </summary>
        /// <param name="status"></param>
        void PublishDeviceStatus(string status);
    }
}
