namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;

    using Iot.Device.DHTxx.Esp32;

    using Microsoft.Extensions.Logging;

    using Models;

    using nanoFramework.Json;
    using nanoFramework.M2Mqtt.Messages;

    using Services.Contracts;

    internal class DhtService : IDhtService
    {
        private readonly IMqttClient _client;
        private readonly ILogger _logger;
        private const int ReadInterval = 60000; // 1 minute
        private const int ErrorInterval = 10000; // 10 seconds
        private const string Topic = "IoT/messages2";
        private const string ErrorTopic = "nf-mqtt/basic-dht";

        public DhtService(IMqttClient client, ILogger logger)
        {
            _client = client;
            _logger = logger;
            Device = new Sensor();
        }

        public Sensor Device { get; set; }

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

        private void ReadAndPublishData(Dht21 dht)
        {
            var temperature = dht.Temperature.Value;
            var humidity = dht.Humidity.Value;

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
    }
}
