namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    /// <summary>
    /// Defines a contract for a service that provides uptime information.
    /// </summary>
    public interface IUptimeService
    {
        /// <summary>
        /// Retrieves the current uptime of the system.
        /// </summary>
        /// <returns>
        /// A string representing the duration for which the system has been running.
        /// This duration is typically presented in a human-readable format, such as 
        /// days, hours, minutes, and seconds.
        /// </returns>
        /// <example>
        /// For example, if the system has been running for 2 days, 3 hours, and 4 minutes, 
        /// this method might return "2 days, 3 hours, 4 minutes".
        /// </example>
        string GetUptime();

        // write summary

        /// <summary>
        /// Starts the service for measuring uptime.
        /// </summary>
        void Start();
    }
}
