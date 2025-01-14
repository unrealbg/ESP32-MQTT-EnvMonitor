namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Threading;

    using Contracts;

    using Helpers;

    using Iot.Device.DHTxx.Esp32;

    using static ESP32_NF_MQTT_DHT.Helpers.Constants;

    /// <summary>
    /// Provides services for reading data from a DHT21 sensor and publishing it via MQTT.
    /// </summary>
    internal class DhtService : ISensorService
    {
        private const int PinEcho = 26;
        private const int PinTrigger = 27;

        private Thread _sensorThread;
        private readonly ManualResetEvent _stopSignal = new ManualResetEvent(false);

        private double _temp = InvalidTemperature;
        private double _humidity = InvalidHumidity;

        /// <summary>
        /// Initializes a new instance of the <see cref="DhtService"/> class.
        /// </summary>
        public DhtService()
        {
            _sensorThread = new Thread(this.SensorReadLoop);
        }

        /// <summary>
        /// Starts the service to continuously read and publish sensor data.
        /// </summary>
        public void Start()
        {
            LogHelper.LogInformation("Start Reading Sensor Data from DHT21");
            _sensorThread.Start();
        }

        public void Stop()
        {
            _sensorThread.Abort();
        }

        /// <summary>
        /// Gets the temperature and humidity data.
        /// </summary>
        /// <returns>An array containing the temperature and humidity data.</returns>
        public double[] GetData()
        {
            return new[] { _temp, _humidity };
        }

        /// <summary>
        /// Gets the temperature data.
        /// </summary>
        /// <returns>The temperature data.</returns>
        public double GetTemp()
        {
            return _temp;
        }

        /// <summary>
        /// Gets the humidity data.
        /// </summary>
        /// <returns>The humidity data.</returns>
        public double GetHumidity()
        {
            return _humidity;
        }

        public string GetSensorType()
        {
            return "DHT21";
        }

        /// <summary>
        /// Reads and publishes the data from the DHT21 sensor.
        /// </summary>
        /// <param name="dht">The DHT21 sensor.</param>
        private void ReadAndPublishData(Dht21 dht)
        {
            try
            {
                _temp = dht.Temperature.DegreesCelsius;
                _humidity = dht.Humidity.Percent;
            }
            catch (Exception e)
            {
                LogHelper.LogError("Error reading sensor data:", e);
                this.SetErrorValues();
            }

            if (dht.IsLastReadSuccessful)
            {
                _stopSignal.WaitOne(ReadInterval, false);
            }
            else
            {
                this.SetErrorValues();
                this._stopSignal.WaitOne(ErrorInterval, false);
            }
        }

        /// <summary>
        /// The loop that continuously reads data from the sensor.
        /// </summary>
        private void SensorReadLoop()
        {
            try
            {
                using (Dht21 dht = new Dht21(PinEcho, PinTrigger))
                {
                    while (true)
                    {
                        this.ReadAndPublishData(dht);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"Error reading sensor data: {ex.Message}");
                this.SetErrorValues();
            }
        }

        /// <summary>
        /// Sets the temperature and humidity values to error values.
        /// </summary>
        private void SetErrorValues()
        {
            _temp = InvalidTemperature;
            _humidity = InvalidHumidity;
        }
    }
}
