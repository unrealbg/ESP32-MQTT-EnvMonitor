namespace ESP32_NF_MQTT_DHT.Managers
{
    using System;

    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Managers.Contracts;
    using ESP32_NF_MQTT_DHT.Models;
    using ESP32_NF_MQTT_DHT.Services.Contracts;
    using ESP32_NF_MQTT_DHT.Settings;

    /// <summary>
    /// Manages sensor operations including data collection and validation.
    /// </summary>
    public class SensorManager : ISensorManager
    {
        private const int MinTemp = -50;
        private const int MaxTemp = 125;
        private const int MinHumidity = 0;
        private const int MaxHumidity = 100;

        private readonly ISensorService _sensorService;
        private readonly LogHelper _logHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorManager"/> class.
        /// </summary>
        /// <param name="sensorService">The sensor service for retrieving sensor data.</param>
        /// <param name="logHelper">The log helper for logging messages.</param>
        public SensorManager(ISensorService sensorService, LogHelper logHelper)
        {
            _sensorService = sensorService ?? throw new ArgumentNullException(nameof(sensorService));
            _logHelper = logHelper ?? throw new ArgumentNullException(nameof(logHelper));
        }

        /// <summary>
        /// Collects sensor data and creates a <see cref="Device"/> object with the collected data.
        /// </summary>
        /// <returns>A <see cref="Device"/> object containing the sensor data, or <c>null</c> if the data is invalid.</returns>
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
                        Temp = temperature,
                        Humid = (int)humidity,
                    };
                }

                _logHelper.LogWithTimestamp($"Invalid data from {sensorType}");
                return null;
            }
            catch (Exception ex)
            {
                _logHelper.LogWithTimestamp($"Error reading sensor data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Starts the sensor service.
        /// </summary>
        public void StartSensor()
        {
            _sensorService.Start();
        }

        /// <summary>
        /// Stops the sensor service.
        /// </summary>
        public void StopSensor()
        {
            _sensorService.Stop();
        }

        /// <summary>
        /// Validates the sensor data.
        /// </summary>
        /// <param name="temperature">The temperature value to validate.</param>
        /// <param name="humidity">The humidity value to validate.</param>
        /// <returns><c>true</c> if the data is valid; otherwise, <c>false</c>.</returns>
        private bool IsValidData(double temperature, double humidity)
        {
            return temperature >= MinTemp && temperature <= MaxTemp && humidity >= MinHumidity && humidity <= MaxHumidity;
        }
    }
}
