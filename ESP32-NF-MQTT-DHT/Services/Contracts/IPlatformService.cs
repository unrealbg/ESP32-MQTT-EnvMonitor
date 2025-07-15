namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    /// <summary>
    /// Defines a contract for platform-specific capabilities and checks.
    /// </summary>
    public interface IPlatformService
    {
        /// <summary>
        /// Gets the target platform name.
        /// </summary>
        string PlatformName { get; }

        /// <summary>
        /// Checks if the current platform supports web server functionality.
        /// </summary>
        /// <returns>True if web server is supported, otherwise false.</returns>
        bool SupportsWebServer();

        /// <summary>
        /// Gets the available memory on the platform.
        /// </summary>
        /// <returns>Available memory in bytes.</returns>
        long GetAvailableMemory();

        /// <summary>
        /// Checks if the platform has sufficient memory for a specific feature.
        /// </summary>
        /// <param name="requiredMemory">Required memory in bytes.</param>
        /// <returns>True if sufficient memory is available, otherwise false.</returns>
        bool HasSufficientMemory(long requiredMemory);
    }
}