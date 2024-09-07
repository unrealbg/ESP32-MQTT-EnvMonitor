namespace ESP32_NF_MQTT_DHT.Services.MQTT
{
    using System;
    using System.Text;
    using System.Threading;

    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Models;
    using ESP32_NF_MQTT_DHT.Services.Contracts;
    using ESP32_NF_MQTT_DHT.Services.MQTT.Contracts;

    using Microsoft.Extensions.Logging;

    using nanoFramework.Json;
    using nanoFramework.M2Mqtt;

    using static ESP32_NF_MQTT_DHT.Helpers.TimeHelper;
    using static ESP32_NF_MQTT_DHT.Settings.DeviceSettings;

    /// <summary>
    /// Service responsible for publishing sensor data and error messages to an MQTT broker.
    /// </summary>
    public class MqttPublishService : IMqttPublishService
    {
        private const int ErrorInterval = 10000;
        private const int SensorDataInterval = 300000;
        private const double InvalidTemperature = -50;
        private const double InvalidHumidity = -100;

        private static readonly string DataTopic = $"home/{DeviceName}/messages";
        private static readonly string ErrorTopic = $"home/{DeviceName}/errors";

        private readonly LogHelper _logHelper;
        private readonly ManualResetEvent _stopSignal = new ManualResetEvent(false);
        private readonly ISensorService _sensorService;
        private readonly IInternetConnectionService _internetConnectionService;
        private MqttClient _mqttClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttPublishService"/> class.
        /// </summary>
        /// <param name="logHelper">The log helper for logging messages.</param>
        /// <param name="sensorService">The sensor service for retrieving sensor data.</param>
        /// <param name="internetConnectionService">The internet connection service for checking internet availability.</param>
        public MqttPublishService(LogHelper logHelper, ISensorService sensorService, IInternetConnectionService internetConnectionService)
        {
            _logHelper = logHelper;
            _sensorService = sensorService;
            _internetConnectionService = internetConnectionService;
        }

        /// <summary>
        /// Sets the MQTT client to be used for publishing messages.
        /// </summary>
        /// <param name="mqttClient">The MQTT client instance.</param>
        public void SetMqttClient(MqttClient mqttClient)
        {
            _mqttClient = mqttClient;
        }

        /// <summary>
        /// Publishes sensor data to the MQTT broker.
        /// </summary>
        public void PublishSensorData()
        {
            double[] data = _sensorService.GetData();

            if (this.IsSensorDataValid(data))
            {
                this.PublishValidSensorData(data);
                _logHelper.LogWithTimestamp(LogLevel.Information, $"Temperature: {data[0]:f2}°C, Humidity: {data[1]:f1}%");
                _stopSignal.WaitOne(SensorDataInterval, false);
            }
            else
            {
                this.PublishError($"[{GetCurrentTimestamp()}] Unable to read sensor data");
                _logHelper.LogWithTimestamp(LogLevel.Warning, "Unable to read sensor data");
                _stopSignal.WaitOne(ErrorInterval, false);
            }
        }

        /// <summary>
        /// Publishes an error message to the MQTT broker.
        /// </summary>
        /// <param name="errorMessage">The error message to be published.</param>
        public void PublishError(string errorMessage)
        {
            this.CheckInternetAndPublish(ErrorTopic, errorMessage);
            _stopSignal.WaitOne(ErrorInterval, false);
        }

        /// <summary>
        /// Checks if the sensor data is valid.
        /// </summary>
        /// <param name="data">The sensor data to be validated.</param>
        /// <returns>True if the data is valid, otherwise false.</returns>
        private bool IsSensorDataValid(double[] data)
        {
            return !(data[0] == InvalidTemperature || data[1] == InvalidHumidity);
        }

        /// <summary>
        /// Checks internet availability and publishes a message to the specified topic.
        /// </summary>
        /// <param name="topic">The topic to publish the message to.</param>
        /// <param name="message">The message to be published.</param>
        private void CheckInternetAndPublish(string topic, string message)
        {
            if (_internetConnectionService.IsInternetAvailable())
            {
                _mqttClient.Publish(topic, Encoding.UTF8.GetBytes(message));
            }
            else
            {
                _logHelper.LogWithTimestamp(LogLevel.Warning, "No internet connection.");
            }
        }

        /// <summary>
        /// Publishes valid sensor data to the MQTT broker.
        /// </summary>
        /// <param name="data">The valid sensor data to be published.</param>
        private void PublishValidSensorData(double[] data)
        {
            var sensorData = this.CreateSensorData(data);
            var message = JsonSerializer.SerializeObject(sensorData);
            this.CheckInternetAndPublish(DataTopic, message);
        }

        /// <summary>
        /// Creates a <see cref="Device"/> object from the sensor data.
        /// </summary>
        /// <param name="data">The sensor data.</param>
        /// <returns>A <see cref="Device"/> object containing the sensor data.</returns>
        private Device CreateSensorData(double[] data)
        {
            return new Device
            {
                DeviceName = DeviceName,
                Location = Location,
                SensorType = SensorTypeName,
                Date = DateTime.UtcNow.Date.ToString("dd/MM/yyyy"),
                Time = DateTime.UtcNow.AddHours(3).ToString("HH:mm:ss"),
                Temp = data?[0].ToString("F2"),
                Humid = (int)data[1],
            };
        }
    }
}