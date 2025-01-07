namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    using Helpers;

    using Services.Contracts;

    /// <summary>
    /// Service for managing and retrieving system uptime information.
    /// </summary>
    public class UptimeService : IUptimeService
    {
        private const int UptimeDelay = 60000;

        private Timer _uptimeTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="UptimeService"/> class.
        /// </summary>
        public UptimeService()
        {
            Stopwatch = new Stopwatch();
            Stopwatch.Start();
            //_uptimeTimer = new Timer(this.UptimeTimerCallback, null, 0, UptimeDelay);
        }

        /// <summary>
        /// Gets the stopwatch used to measure the system uptime.
        /// </summary>
        public Stopwatch Stopwatch { get; private set; }

        /// <summary>
        /// Retrieves the current uptime of the system.
        /// </summary>
        /// <returns>
        /// A string representing the duration for which the system has been running.
        /// This duration is typically presented in a human-readable format, such as 
        /// days, hours, minutes, and seconds.
        /// </returns>
        public string GetUptime()
        {
            var elapsed = Stopwatch.Elapsed;
            return $"{elapsed.Days} days, {elapsed.Hours} hours, {elapsed.Minutes} minutes, {elapsed.Seconds} seconds";
        }

        /// <summary>
        /// Callback method for the uptime timer.
        /// </summary>
        /// <param name="state">The state object passed to the callback method.</param>
        private void UptimeTimerCallback(object state)
        {
            try
            {
                var uptimeMessage = this.GetUptime();
                LogHelper.LogInformation(uptimeMessage);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex.Message);
            }
        }
    }
}