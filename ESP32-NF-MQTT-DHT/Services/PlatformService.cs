namespace ESP32_NF_MQTT_DHT.Services
{
    using ESP32_NF_MQTT_DHT.Configuration;
    using ESP32_NF_MQTT_DHT.Services.Contracts;
    using nanoFramework.Runtime.Native;

    /// <summary>
    /// Provides platform-specific capabilities and checks for ESP32 devices.
    /// </summary>
    public class PlatformService : IPlatformService
    {
        /// <summary>
        /// Gets the target platform name.
        /// </summary>
        public string PlatformName => SystemInfo.TargetName;

        /// <summary>
        /// Checks if the current platform supports web server functionality.
        /// </summary>
        /// <returns>True if web server is supported, otherwise false.</returns>
        public bool SupportsWebServer()
        {
            return this.PlatformName == AppConfiguration.Platform.SupportedWebServerPlatform && this.HasSufficientMemory(AppConfiguration.Platform.WebServerRequiredMemory);
        }

        /// <summary>
        /// Gets the available memory on the platform.
        /// </summary>
        /// <returns>Available memory in bytes.</returns>
        public long GetAvailableMemory()
        {
            return GC.Run(false);
        }

        /// <summary>
        /// Checks if the platform has sufficient memory for a specific feature.
        /// </summary>
        /// <param name="requiredMemory">Required memory in bytes.</param>
        /// <returns>True if sufficient memory is available, otherwise false.</returns>
        public bool HasSufficientMemory(long requiredMemory)
        {
            return this.GetAvailableMemory() >= requiredMemory;
        }
    }
}