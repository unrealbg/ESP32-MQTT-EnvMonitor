namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    using ESP32_NF_MQTT_DHT.Services.Contracts;

    using Microsoft.Extensions.Logging;

    using static Helpers.TimeHelper;

    public class UptimeService : IUptimeService
    {
        private const int UptimeDelay = 60000;

        private readonly ILogger _logger;
        private Timer _uptimeTimer;

        public UptimeService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger(nameof(UptimeService)) ?? throw new ArgumentNullException(nameof(loggerFactory));
            Stopwatch = new Stopwatch();
            Stopwatch.Start();
            _uptimeTimer = new Timer(this.UptimeTimerCallback, null, 0, UptimeDelay);
        }

        public Stopwatch Stopwatch { get; private set; }

        public string GetUptime()
        {
            var elapsed = Stopwatch.Elapsed;
            return $"{elapsed.Days} days, {elapsed.Hours} hours, {elapsed.Minutes} minutes, {elapsed.Seconds} seconds";
        }

        private void UptimeTimerCallback(object state)
        {
            try
            {
                var uptimeMessage = this.GetUptime();
                _logger.LogInformation($"[{GetCurrentTimestamp()}] {uptimeMessage}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{GetCurrentTimestamp()}] {ex.Message}");
            }
        }
    }
}