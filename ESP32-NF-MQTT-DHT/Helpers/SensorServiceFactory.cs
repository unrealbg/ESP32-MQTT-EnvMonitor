namespace ESP32_NF_MQTT_DHT.Helpers
{
    using System;

    using ESP32_NF_MQTT_DHT.Services.Sensors;

    /// <summary>
    /// Factory for creating sensor services.
    /// </summary>
    public static class SensorServiceFactory
    {
        public static Type GetSensorServiceType(SensorType sensorType)
        {
            return sensorType switch
            {
                SensorType.DHT => typeof(DhtService),
                SensorType.AHT => typeof(AhtSensorService),
                SensorType.SHTC3 => typeof(Shtc3SensorService),
                SensorType.AHT20_BMP280 => typeof(Aht20Bmp280SensorService),
                _ => throw new ArgumentException("Unknown sensor type", nameof(sensorType)),
            };
        }
    }
}
