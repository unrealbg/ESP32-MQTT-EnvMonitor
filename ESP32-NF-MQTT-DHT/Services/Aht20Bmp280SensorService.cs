namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Device.I2c;
    using System.Threading;

    using Contracts;

    using ESP32_NF_MQTT_DHT.Helpers;

    using Iot.Device.Ahtxx;
    using Iot.Device.Bmxx80;
    using Iot.Device.Bmxx80.FilteringMode;

    using nanoFramework.Hardware.Esp32;

    using static ESP32_NF_MQTT_DHT.Helpers.Constants;

    /// <summary>
    /// Service for reading temperature and humidity from AHT20 and pressure from BMP280.
    /// Requires I²C pins 22 (data) and 23 (clock).
    /// </summary>
    public class Aht20Bmp280SensorService : ISensorService
    {
        private const int DataPin = 17;
        private const int ClockPin = 18;

        private readonly ManualResetEvent _stopSignal = new ManualResetEvent(false);

        private double _temperature = InvalidTemperature;
        private double _humidity = InvalidHumidity;
        private double _pressure = InvalidPressure;

        private bool _running;

        /// <summary>
        /// Starts the service.
        /// </summary>
        public void Start()
        {
            _running = true;

            Configuration.SetPinFunction(DataPin, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(ClockPin, DeviceFunction.I2C1_CLOCK);

            Thread sensorReadThread = new Thread(StartReceivingData);
            sensorReadThread.Start();
        }

        /// <summary>
        /// Спира услугата.
        /// </summary>
        public void Stop()
        {
            _running = false;
            _stopSignal.Set();
        }

        /// <summary>
        /// Retrieves the sensor data.
        /// </summary>
        public double[] GetData() => new[] { _temperature, _humidity, _pressure };

        /// <summary>
        /// Retrieves the temperature reading from the sensor.
        /// </summary>
        public double GetTemp() => _temperature;

        /// <summary>
        /// Retrieves the humidity reading from the sensor.
        /// </summary>
        public double GetHumidity() => _humidity;

        /// <summary>
        /// Retrieves the pressure reading from the sensor.
        /// </summary>
        public double GetPressure() => _pressure;

        /// <summary>
        /// Връща описанието на сензорния тип.
        /// </summary>
        public string GetSensorType() => "AHT20 + BMP280";

        /// <summary>
        /// Main loop for continuous reading from the sensors.
        /// </summary>
        private void StartReceivingData()
        {
            I2cConnectionSettings settingsAht20 = new I2cConnectionSettings(1, AhtBase.DefaultI2cAddress);
            I2cConnectionSettings settingsBmp280 = new I2cConnectionSettings(1, Bmp280.DefaultI2cAddress);

            using (I2cDevice aht20Device = I2cDevice.Create(settingsAht20))
            using (I2cDevice bmp280Device = I2cDevice.Create(settingsBmp280))
            using (var aht20 = new Aht20(aht20Device))
            using (var bmp280 = new Bmp280(bmp280Device))
            {
                bmp280.TemperatureSampling = Sampling.UltraHighResolution;
                bmp280.PressureSampling = Sampling.UltraHighResolution;
                bmp280.FilterMode = Bmx280FilteringMode.X4;

                bmp280.Reset();

                while (_running)
                {
                    try
                    {
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

                        _stopSignal.WaitOne(ReadInterval, false);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogError($"Error reading sensor data: {ex.Message}");
                        SetErrorValues();
                        _stopSignal.WaitOne(ErrorInterval, false);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the sensor values to error values.
        /// </summary>
        private void SetErrorValues()
        {
            _temperature = InvalidTemperature;
            _humidity = InvalidHumidity;
            _pressure = InvalidPressure;
        }
    }
}
