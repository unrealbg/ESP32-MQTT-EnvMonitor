namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Threading;

    using Iot.Device.DHTxx.Esp32;

    using Microsoft.Extensions.Logging;

    using Contracts;

    using static Helpers.TimeHelper;

    /// <summary>
    /// Provides services for reading data from a DHT21 sensor and publishing it via MQTT.
    /// </summary>
    internal class DhtService : IDhtService
    {
        private readonly ILogger _logger;
        private const int ReadInterval = 60000; // 1 minute
        private const int ErrorInterval = 30000; // 30 seconds

        private Thread _sensorThread;
        private double _temp = -50;
        private double _humidity = -100;

        /// <summary>
        /// Initializes a new instance of the <see cref="DhtService"/> class.
        /// </summary>
        /// <param name="loggerFactory">Factory to create a logger for this service.</param>
        /// <exception cref="ArgumentNullException">Thrown if loggerFactory is null.</exception>
        public DhtService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger(nameof(DhtService)) ?? throw new ArgumentNullException(nameof(loggerFactory));
            _sensorThread = new Thread(SensorReadLoop);
        }

        /// <summary>
        /// Starts the service to continuously read and publish sensor data.
        /// </summary>
        public void Start()
        {
            _logger.LogInformation($"[{GetCurrentTimestamp()}] Start Reading Sensor Data from DHT21");
            _sensorThread.Start();
        }

        public double[] GetData()
        {
            return new[] {_temp,  _humidity};
        }

        public double GetTemp()
        {
            return this._temp;
        }

        public double GetHumidity()
        {
            return this._humidity;
        }

        private void ReadAndPublishData(Dht21 dht)
        {
            this._temp = dht.Temperature.Value;
            this._humidity = dht.Humidity.Value;

            if (dht.IsLastReadSuccessful)
            {
                _logger.LogInformation($"[{GetCurrentTimestamp()}] Temp: {_temp}\nHumid: {_humidity}");
                Thread.Sleep(ReadInterval);
            }
            else
            {
                _logger.LogWarning($"[{GetCurrentTimestamp()}] Unable to read sensor data");
                SetErrorValues();
                Thread.Sleep(ErrorInterval);
            }
        }

        private void SensorReadLoop()
        {
            using (Dht21 dht = new Dht21(26, 27))
            {
                while (true)
                {
                    try
                    {
                        this.ReadAndPublishData(dht);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[{GetCurrentTimestamp()}] Sensor reading error: {ex.Message}");
                        SetErrorValues();
                    }
                }
            }
        }
        private void SetErrorValues()
        {
            _temp = -50;
            _humidity = -100;
        }
    }
}
