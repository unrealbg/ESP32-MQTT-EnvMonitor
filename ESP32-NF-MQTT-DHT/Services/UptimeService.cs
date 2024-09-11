namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    using Helpers;

    using Services.Contracts;

    public class UptimeService : IUptimeService
    {
        private const int UptimeDelay = 60000;

        private readonly LogHelper _logHelper;
        private Timer _uptimeTimer;

        public UptimeService()
        {
            _logHelper = new LogHelper();
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
                _logHelper.LogWithTimestamp(uptimeMessage);
            }
            catch (Exception ex)
            {
                _logHelper.LogWithTimestamp(ex.Message);
            }
        }
    }
}