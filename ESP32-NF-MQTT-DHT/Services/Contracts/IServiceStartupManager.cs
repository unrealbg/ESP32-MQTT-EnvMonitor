namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    /// <summary>
    /// Defines a contract for managing the startup sequence of application services.
    /// </summary>
    public interface IServiceStartupManager
    {
        /// <summary>
        /// Starts all core services in the correct order.
        /// </summary>
        void StartAllServices();

        /// <summary>
        /// Starts a specific service by name.
        /// </summary>
        /// <param name="serviceName">The name of the service to start.</param>
        void StartService(string serviceName);

        /// <summary>
        /// Stops all running services.
        /// </summary>
        void StopAllServices();
    }
}