namespace ESP32_NF_MQTT_DHT.Services
{
    using System.Diagnostics;
    using System.Text;

    using Services.Contracts;

    /// <summary>
    /// Provides functionality to measure the uptime of the system.
    /// </summary>
    public class UptimeService : IUptimeService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UptimeService"/> class.
        /// </summary>
        public UptimeService()
        {
            this.Stopwatch = new Stopwatch();
            this.Stopwatch.Start();
        }

        /// <summary>
        /// Gets the stopwatch used to measure uptime.
        /// </summary>
        public Stopwatch Stopwatch { get; private set; }

        /// <summary>
        /// Gets the total uptime of the system in a human-readable format.
        /// </summary>
        /// <returns>A string representing the total uptime.</returns>
        public string GetUptime()
        {
            var elapsed = this.Stopwatch.Elapsed;
            var uptimeStringBuilder = new StringBuilder();
            uptimeStringBuilder.Append(elapsed.Days).Append(" days, ");
            uptimeStringBuilder.Append(elapsed.Hours).Append(" hours, ");
            uptimeStringBuilder.Append(elapsed.Minutes).Append(" minutes, ");
            uptimeStringBuilder.Append(elapsed.Seconds).Append(" seconds");

            return uptimeStringBuilder.ToString();
        }
    }
}
