namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;

    using Microsoft.Extensions.Logging;

    using Contracts;
    
    using static Helpers.TimeHelper;

    /// <summary>
    /// Provides functionality to measure the uptime of the system.
    /// </summary>
    public class UptimeService : IUptimeService
    {
        private const int UptimeDelay = 60000;
        private const int ErrorDelay = 15000;

        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UptimeService"/> class.
        /// </summary>
        public UptimeService(ILoggerFactory loggerFactory)
        {
            Stopwatch = new Stopwatch();
            Stopwatch.Start();

            _logger = loggerFactory?.CreateLogger(nameof(UptimeService)) ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public void Start()
        {
            var uptimeThread = new Thread(UptimeLoop);
            uptimeThread.Start();
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
            var elapsed = Stopwatch.Elapsed;
            var uptimeStringBuilder = new StringBuilder();
            uptimeStringBuilder.Append(elapsed.Days).Append(" days, ");
            uptimeStringBuilder.Append(elapsed.Hours).Append(" hours, ");
            uptimeStringBuilder.Append(elapsed.Minutes).Append(" minutes, ");
            uptimeStringBuilder.Append(elapsed.Seconds).Append(" seconds");

            return uptimeStringBuilder.ToString();
        }

        private void UptimeLoop()
        {
            while (true)
            {
                try
                {
                    var uptimeMessage = GetUptime();

                    _logger.LogInformation($"[{GetCurrentTimestamp()}] {uptimeMessage}");
                    Thread.Sleep(UptimeDelay);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[{GetCurrentTimestamp()}] {ex.Message}");
                    Thread.Sleep(ErrorDelay);
                    // optional
                    // Power.RebootDevice();
                }
            }
        }
    }
}
