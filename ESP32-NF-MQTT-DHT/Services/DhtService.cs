namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Text;
    using System.Threading;

    using Iot.Device.DHTxx.Esp32;

    using Microsoft.Extensions.Logging;

    using Models;

    using nanoFramework.Json;

    using Services.Contracts;

    using Constants;

    /// <summary>
    /// Provides services for reading data from a DHT21 sensor and publishing it via MQTT.
    /// </summary>
    internal class DhtService : IDhtService
    {
        private readonly IMqttClientService _client;
        private readonly ILogger _logger;
        private const int ReadInterval = 300000; // 5 minutes
        private const int ErrorInterval = 10000; // 10 seconds
        private const string Topic = "IoT/messages2";
        private static readonly string ErrorTopic = $"home/{Constants.DEVICE}/errors";
        private double _temp;
        private double _humidity;

        /// <summary>
        /// Initializes a new instance of the <see cref="DhtService"/> class.
        /// </summary>
        /// <param name="client">The MQTT client used for publishing messages.</param>
        /// <param name="loggerFactory">Factory to create a logger for this service.</param>
        /// <exception cref="ArgumentNullException">Thrown if loggerFactory is null.</exception>
        public DhtService(IMqttClientService client, ILoggerFactory loggerFactory)
        {
            _client = client;
            _logger = loggerFactory?.CreateLogger(nameof(DhtService)) ?? throw new ArgumentNullException(nameof(loggerFactory));
            Device = new Sensor();
        }

        /// <summary>
        /// Gets or sets the sensor device.
        /// </summary>
        public Sensor Device { get; set; }

        /// <summary>
        /// Starts the service to continuously read and publish sensor data.
        /// </summary>
        public void Start()
        {
            _logger.LogInformation("[+] Start Reading Sensor Data from DHT21");

            using (Dht21 dht = new Dht21(26, 27))
            {
                while (true)
                {
                    try
                    {
                        ReadAndPublishData(dht);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Sensor reading error: {ex.Message}");
                        PublishError("Sensor reading error: " + ex.Message);
                    }
                }
            }
        }

        public void GetData()
        {
            PublishSensorData();
        }

        public string GetTemp()
        {
            return this._temp.ToString();
        }

        public string GetHumidity()
        {
            return this._humidity.ToString();
        }

        private void ReadAndPublishData(Dht21 dht)
        {
            var temperature = dht.Temperature.Value;
            var humidity = dht.Humidity.Value;
            this._temp = temperature;
            this._humidity = humidity;

            if (dht.IsLastReadSuccessful)
            {
                UpdateDeviceData(temperature, humidity);
                var msg = JsonSerializer.SerializeObject(Device);
                _client.MqttClient.Publish(Topic, Encoding.UTF8.GetBytes(msg));
                Thread.Sleep(ReadInterval);
            }
            else
            {
                _logger.LogWarning("Unable to read sensor data");
                PublishError("Unable to read sensor data");
            }
        }

        private void UpdateDeviceData(double temperature, double humidity)
        {
            Device.Data.Date = DateTime.UtcNow.Date.ToString("dd/MM/yyyy");
            Device.Data.Time = DateTime.UtcNow.ToString("HH:mm:ss");
            Device.Data.Temp = temperature;
            Device.Data.Humid = (int)humidity;
        }

        private void PublishError(string errorMessage)
        {
            _client.MqttClient.Publish(ErrorTopic, Encoding.UTF8.GetBytes(errorMessage));
            Thread.Sleep(ErrorInterval);
        }

        private void PublishSensorData()
        {
            var msg = JsonSerializer.SerializeObject(this.Device);
            this._client.MqttClient.Publish(Topic, Encoding.UTF8.GetBytes(msg));
        }
    }
}
