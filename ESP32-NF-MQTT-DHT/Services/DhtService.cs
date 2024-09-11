namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Threading;

    using Contracts;

    using Helpers;

    using Iot.Device.DHTxx.Esp32;

    /// <summary>
    /// Provides services for reading data from a DHT21 sensor and publishing it via MQTT.
    /// </summary>
    internal class DhtService : ISensorService
    {
        private const int ReadInterval = 60000; // 1 minute
        private const int ErrorInterval = 30000; // 30 seconds
        private const int PinEcho = 26;
        private const int PinTrigger = 27;
        private const int TempErrorValue = -50;
        private const int HumidityErrorValue = -100;

        private readonly LogHelper _logHelper;

        private Thread _sensorThread;

        private double _temp = TempErrorValue;
        private double _humidity = HumidityErrorValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="DhtService"/> class.
        /// </summary>
        public DhtService()
        {
            _logHelper = new LogHelper();
            _sensorThread = new Thread(this.SensorReadLoop);
        }

        /// <summary>
        /// Starts the service to continuously read and publish sensor data.
        /// </summary>
        public void Start()
        {
            this._logHelper.LogWithTimestamp("Start Reading Sensor Data from DHT21");
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
            _temp = dht.Temperature.DegreesCelsius;
            _humidity = dht.Humidity.Percent;

            if (dht.IsLastReadSuccessful)
            {
                _logHelper.LogWithTimestamp("Data read from DHT21 sensor.");
                _logHelper.LogWithTimestamp($"Temperature: {_temp}°C, Humidity: {_humidity}%");
                Thread.Sleep(ReadInterval);
            }
            else
            {
                _logHelper.LogWithTimestamp("Unable to read sensor data");
                this.SetErrorValues();
                Thread.Sleep(ErrorInterval);
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
                this._logHelper.LogWithTimestamp("Unable to read sensor data");
                this.SetErrorValues();
            }
        }

        /// <summary>
        /// Sets the temperature and humidity values to error values.
        /// </summary>
        private void SetErrorValues()
        {
            _temp = TempErrorValue;
            _humidity = HumidityErrorValue;
        }
    }
}
