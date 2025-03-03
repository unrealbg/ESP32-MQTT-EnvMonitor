namespace ESP32_NF_MQTT_DHT.Services.MQTT
{
    using System.Threading;

    using ESP32_NF_MQTT_DHT.Helpers;

    /// <summary>
    /// Publishes sensor data at regular intervals.
    /// </summary>
    internal class SensorDataPublisher
    {
        private readonly TimerCallback _publishCallback;
        private Timer _timer;

        public SensorDataPublisher(TimerCallback publishCallback)
        {
            this._publishCallback = publishCallback;
        }

        /// <summary>
        /// Starts the sensor data publisher.
        /// </summary>
        /// <param name="intervalMs">The interval in milliseconds at which to publish sensor data.</param>
        public void Start(int intervalMs)
        {
            if (_timer == null)
            {
                _timer = new Timer(this._publishCallback, null, 0, intervalMs);
                LogHelper.LogInformation("Sensor data timer started successfully.");
            }
            else
            {
                LogHelper.LogInformation("Sensor data timer is already running.");
            }
        }

        /// <summary>
        /// Stops the sensor data publisher.
        /// </summary>
        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
                LogHelper.LogInformation("Sensor data timer stopped.");
            }
            else
            {
                LogHelper.LogInformation("Sensor data timer is not running.");
            }
        }
    }
}