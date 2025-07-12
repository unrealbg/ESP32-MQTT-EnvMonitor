namespace ESP32_NF_MQTT_DHT.Settings
{
    /// <summary>
    /// Configuration settings for the web server.
    /// </summary>
    public static class WebServerSettings
    {
        /// <summary>
        /// The timeout value in milliseconds for socket operations.
        /// Increase this value if you experience socket timeouts.
        /// </summary>
        public const int SocketTimeout = 20000; // 20 seconds
        
        /// <summary>
        /// The timeout value in milliseconds for form processing operations.
        /// </summary>
        public const int FormProcessingTimeout = 5000; // 5 seconds
        
        /// <summary>
        /// Maximum allowed request size in bytes.
        /// </summary>
        public const int MaxRequestSize = 4096; // 4KB
        
        /// <summary>
        /// Controls whether to set Content-Length headers on responses.
        /// This can help with browser rendering and connection handling.
        /// </summary>
        public const bool UseContentLength = true;
    }
}