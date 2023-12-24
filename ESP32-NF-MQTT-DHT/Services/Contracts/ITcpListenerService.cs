namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    /// <summary>
    /// Defines a contract for a service that manages TCP listener functionalities.
    /// </summary>
    public interface ITcpListenerService
    {
        /// <summary>
        /// Starts the TCP listener service.
        /// </summary>
        /// <remarks>
        /// This method should initialize and begin the TCP listening process, 
        /// enabling the application to accept and handle incoming TCP connections.
        /// It typically involves setting up a socket to listen on a specific port,
        /// and may include configuring various network parameters and handling client connections.
        /// </remarks>
        void Start();
    }
}