namespace ESP32_NF_MQTT_DHT.Modules.Contracts
{
    /// <summary>
    /// Represents a pluggable module with lifecycle hooks.
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// Human friendly module name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Starts the module.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the module and releases resources.
        /// </summary>
        void Stop();
    }
}
