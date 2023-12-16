namespace ESP32_NF_MQTT_DHT.Services
{
    using System.Diagnostics;
    using System.Text;

    using Services.Contracts;

    public class UptimeService : IUptimeService
    {
        public UptimeService()
        {
            this.Stopwatch = new Stopwatch();
            this.Stopwatch.Start();
        }

        public Stopwatch Stopwatch { get; private set; }

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
