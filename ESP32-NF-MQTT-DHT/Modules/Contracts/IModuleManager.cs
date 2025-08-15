namespace ESP32_NF_MQTT_DHT.Modules.Contracts
{
    /// <summary>
    /// Manages lifecycle of registered modules.
    /// </summary>
    public interface IModuleManager
    {
        /// <summary>
        /// Registers a module instance for lifecycle management.
        /// </summary>
        /// <param name="module">Module to register.</param>
        void Register(IModule module);

        /// <summary>
        /// Starts all registered modules.
        /// </summary>
        void StartAll();

        /// <summary>
        /// Stops all registered modules.
        /// </summary>
        void StopAll();
    }
}
