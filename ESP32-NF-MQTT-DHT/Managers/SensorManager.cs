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
        private readonly ISensorService[] _sensorServices;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorManager"/> class.
        /// </summary>
        /// <param name="sensorServices">An array of sensor services to manage.</param>
        public SensorManager(ISensorService[] sensorServices)
        {
            _sensorServices = sensorServices;
        }

        /// <summary>
        /// Collects sensor data and creates a <see cref="Device"/> object with the collected data.
        /// </summary>
        /// <returns>A <see cref="Device"/> object containing the sensor data, or <c>null</c> if the data is invalid.</returns>
        public Device CollectAndCreateSensorData()
        {
            foreach (var sensor in _sensorServices)
            {
                var temperature = sensor.GetTemp();
                var humidity = sensor.GetHumidity();
                var sensorType = sensor.GetSensorType();

                if (!double.IsNaN(temperature) && !double.IsNaN(humidity))
                {
                    LogHelper.LogInformation($"{sensorType} - Temp: {temperature}°C, Humidity: {humidity}%");
                    return new Device
                    {
                        DeviceName = DeviceSettings.DeviceName,
                        Location = DeviceSettings.Location,
                        SensorType = sensorType,
                        DateTime = DateTime.UtcNow,
                        Temp = Math.Round(temperature * 100) / 100,
                        Humid = (int)humidity,
                    };
                }
            }

            LogHelper.LogWarning("Invalid data from sensors.");
            return null;
        }

        /// <summary>
        /// Starts all sensor services.
        /// </summary>
        public void StartSensor()
        {
            foreach (var sensor in _sensorServices)
            {
                sensor.Start();
            }
        }

        /// <summary>
        /// Stops all sensor services.
        /// </summary>
        public void StopSensor()
        {
            foreach (var sensor in _sensorServices)
            {
                sensor.Stop();
            }
        }
    }
}
