namespace ESP32_NF_MQTT_DHT.Configuration
{
    /// <summary>
    /// Configuration settings for application startup.
    /// </summary>
    public static class StartupConfiguration
    {
        /// <summary>
        /// Minimum required memory for basic operations in bytes.
        /// </summary>
        public const long RequiredMemory = AppConfiguration.Platform.StartupRequiredMemory;

        /// <summary>
        /// Default startup timeout in milliseconds.
        /// </summary>
        public const int StartupTimeoutMs = AppConfiguration.Network.ConnectionTimeoutMs;

        /// <summary>
        /// Maximum retry attempts for service startup.
        /// </summary>
        public const int MaxRetryAttempts = AppConfiguration.Network.MaxRetryAttempts;
    }
}