namespace ESP32_NF_MQTT_DHT.Services.MQTT
{
    using System.Threading;

    using ESP32_NF_MQTT_DHT.Helpers;

    /// <summary>
    /// Publishes sensor data at regular intervals.
    /// </summary>
    internal class SensorDataPublisher
    {
        private readonly LogHelper _logHelper = new LogHelper();
        private readonly TimerCallback _publishCallback;
        private Timer _timer;

        public SensorDataPublisher(TimerCallback publishCallback)
        {
            this._publishCallback = publishCallback;
        }

        public void Start(int intervalMs)
        {
            if (this._timer == null)
            {
                this._timer = new Timer(this._publishCallback, null, 0, intervalMs);
                this._logHelper.LogWithTimestamp("Sensor data timer started successfully.");
            }
        }

        public void Stop()
        {
            if (this._timer != null)
            {
                this._timer.Dispose();
                this._timer = null;
                this._logHelper.LogWithTimestamp("Sensor data timer stopped.");
            }
        }
    }
}