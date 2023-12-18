namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    using nanoFramework.M2Mqtt;

    /// <summary>
    /// Defines a contract for a service that manages MQTT client functionalities.
    /// </summary>
    public interface IMqttClient
    {
        /// <summary>
        /// Gets the instance of the MQTT client.
        /// </summary>
        /// <value>
        /// The MQTT client used for publishing and subscribing to messages.
        /// </value>
        MqttClient MqttClient { get; }

        /// <summary>
        /// Starts the MQTT client service.
        /// </summary>
        /// <remarks>
        /// This method should initialize and connect the MQTT client to a broker.
        /// It may also involve setting up necessary callbacks or listeners for handling
        /// incoming messages and maintaining the connection.
        /// </remarks>
        void Start();
    }
}