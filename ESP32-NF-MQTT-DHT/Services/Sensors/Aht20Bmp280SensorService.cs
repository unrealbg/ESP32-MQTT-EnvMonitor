namespace ESP32_NF_MQTT_DHT.Services.Sensors
{
    using System.Device.I2c;

    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Services.Contracts;

    using Iot.Device.Ahtxx;
    using Iot.Device.Bmxx80;
    using Iot.Device.Bmxx80.FilteringMode;

    using nanoFramework.Hardware.Esp32;

    using static ESP32_NF_MQTT_DHT.Helpers.Constants;

    /// <summary>
    /// Service for managing the AHT20 and BMP280 sensors.
    /// </summary>
    public class Aht20Bmp280SensorService : BaseSensorService
    {
        private const int DataPin = 17;
        private const int ClockPin = 18;
        private double _pressure = InvalidPressure;

        /// <summary>
        /// Gets the sensor data including temperature, humidity, and pressure.
        /// </summary>
        /// <returns>An array of doubles containing the temperature, humidity, and pressure values.</returns>
        public override double[] GetData() => new[] { _temperature, _humidity, _pressure };

        /// <summary>
        /// Gets the pressure reading from the sensor.
        /// </summary>
        /// <returns>The pressure value recorded by the sensor.</returns>
        public override double GetPressure() => _pressure;

        /// <summary>
        /// Gets the type of the sensor.
        /// </summary>
        /// <returns>A string representing the sensor type.</returns>
        public override string GetSensorType() => "AHT20 + BMP280";

        /// <summary>
        /// Starts the sensor service.
        /// </summary>
        public override void Start()
        {
            Configuration.SetPinFunction(DataPin, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(ClockPin, DeviceFunction.I2C1_CLOCK);
            base.Start();
        }

        /// <summary>
        /// Reads the sensor data and updates the temperature, humidity, and pressure values.
        /// </summary>
        protected override void ReadSensorData()
        {
            I2cConnectionSettings settingsAht20 = new I2cConnectionSettings(1, AhtBase.DefaultI2cAddress);
            I2cConnectionSettings settingsBmp280 = new I2cConnectionSettings(1, Bmx280Base.DefaultI2cAddress);

            using (I2cDevice aht20Device = I2cDevice.Create(settingsAht20))
            using (I2cDevice bmp280Device = I2cDevice.Create(settingsBmp280))
            using (var aht20 = new Aht20(aht20Device))
            using (var bmp280 = new Bmp280(bmp280Device))
            {
                bmp280.TemperatureSampling = Sampling.UltraHighResolution;
                bmp280.PressureSampling = Sampling.UltraHighResolution;
                bmp280.FilterMode = Bmx280FilteringMode.X4;
                bmp280.Reset();

                _temperature = aht20.GetTemperature().DegreesCelsius;
                _humidity = aht20.GetHumidity().Percent;

                var bmpReadResult = bmp280.Read();
                if (bmpReadResult.TemperatureIsValid && bmpReadResult.PressureIsValid)
                {
                    _pressure = bmpReadResult.Pressure.Hectopascals;
                    LogHelper.LogDebug($"Pressure: {_pressure} hPa");
                }
                else
                {
                    _pressure = InvalidPressure;
                }
            }
        }
    }
}
