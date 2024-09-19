namespace ESP32_NF_MQTT_DHT.Managers
{
    using System;
    using System.Diagnostics;

    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Managers.Contracts;
    using ESP32_NF_MQTT_DHT.Models;
    using ESP32_NF_MQTT_DHT.Services.Contracts;
    using ESP32_NF_MQTT_DHT.Settings;

    public class SensorManager : ISensorManager
    {
        private const int MinTemp = -50;
        private const int MaxTemp = 125;
        private const int MinHumidity = 0;
        private const int MaxHumidity = 100;

        private readonly ISensorService _sensorService;
        private readonly LogHelper _logHelper;

        public SensorManager(ISensorService sensorService, LogHelper logHelper)
        {
            _sensorService = sensorService ?? throw new ArgumentNullException(nameof(sensorService));
            _logHelper = logHelper ?? throw new ArgumentNullException(nameof(logHelper));
        }

        public Device CollectAndCreateSensorData()
        {
            try
            {
                var sensorType = _sensorService.GetSensorType();
                var data = _sensorService.GetData();
                var temperature = data[0];
                var humidity = data[1];

                if (this.IsValidData(temperature, humidity))
                {
                    _logHelper.LogWithTimestamp($"{sensorType} - Temp: {temperature}°C, Humidity: {humidity}%");

                    return new Device
                    {
                        DeviceName = DeviceSettings.DeviceName,
                        Location = DeviceSettings.Location,
                        SensorType = sensorType,
                        Date = DateTime.UtcNow.Date.ToString("dd/MM/yyyy"),
                        Time = DateTime.UtcNow.AddHours(3).ToString("HH:mm:ss"),
                        Temp = temperature.ToString("F2"),
                        Humid = (int)humidity,
                    };
                }

                Debug.WriteLine($"Invalid data from {sensorType}");
                _logHelper.LogWithTimestamp($"Invalid data from {sensorType}");
                return null;
            }
            catch (Exception ex)
            {
                _logHelper.LogWithTimestamp($"Error reading sensor data: {ex.Message}");
                return null;
            }
        }

        public void StartSensor()
        {
            _sensorService.Start();
        }

        public void StopSensor()
        {
            _sensorService.Stop();
        }

        private bool IsValidData(double temperature, double humidity)
        {
            return temperature >= MinTemp && temperature <= MaxTemp && humidity >= MinHumidity && humidity <= MaxHumidity;
        }
    }
}
