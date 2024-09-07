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

        public MqttPublishService(LogHelper logHelper, ISensorService sensorService, IInternetConnectionService internetConnectionService)
        {
            _logHelper = logHelper;
            _sensorService = sensorService;
            _internetConnectionService = internetConnectionService;
        }

        public void SetMqttClient(MqttClient mqttClient)
        {
            _mqttClient = mqttClient;
        }

        public void PublishSensorData()
        {
            double[] data;

            data = this._sensorService.GetData();

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

        public void PublishError(string errorMessage)
        {
            this.CheckInternetAndPublish(ErrorTopic, errorMessage);
            _stopSignal.WaitOne(ErrorInterval, false);
        }

        private bool IsSensorDataValid(double[] data)
        {
            return !(data[0] == InvalidTemperature || data[1] == InvalidHumidity);
        }

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

        private void PublishValidSensorData(double[] data)
        {
            var sensorData = this.CreateSensorData(data);
            var message = JsonSerializer.SerializeObject(sensorData);
            this.CheckInternetAndPublish(DataTopic, message);
        }

        private Device CreateSensorData(double[] data)
        {
            var temp = data[0];
            temp = (int)((temp * 100) + 0.5) / 100.0;

            return new Device
            {
                DeviceName = DeviceName,
                Location = Location,
                SensorType = SensorTypeName,
                Date = DateTime.UtcNow.Date.ToString("dd/MM/yyyy"),
                Time = DateTime.UtcNow.AddHours(3).ToString("HH:mm:ss"),
                Temp = temp,
                Humid = (int)data[1],
            };
        }
    }
}