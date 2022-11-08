namespace ESP32_NF_MQTT_DHT.Services
{
    using System.Diagnostics;

    using Services.Contracts;

    public class UptimeService : IUptimeService
    {
        public UptimeService()
        {
            this.Stopwatch = new Stopwatch();
        }

        public Stopwatch Stopwatch { get; }

        public string GetUptime()
        {
            this.Stopwatch.Stop();
            return this.Stopwatch.Elapsed.Days + " days, " +
                   this.Stopwatch.Elapsed.Hours + " hours, " +
                   this.Stopwatch.Elapsed.Minutes + " minutes, " +
                   this.Stopwatch.Elapsed.Seconds + " seconds";
        }
    }
}
