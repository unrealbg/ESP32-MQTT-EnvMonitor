namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    /// <summary>
    /// Interface for a service that provides web server functionalities.
    /// </summary>
    public interface IWebServerService
    {
        /// <summary>
        /// Starts the web server, allowing it to begin processing incoming HTTP requests.
        /// </summary>
        void Start();
    }
}