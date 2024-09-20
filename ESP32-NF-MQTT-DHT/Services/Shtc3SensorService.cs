namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Device.I2c;
    using System.Threading;

    using Helpers;

    using Iot.Device.Shtc3;

    using nanoFramework.Hardware.Esp32;

    using Services.Contracts;

    using static ESP32_NF_MQTT_DHT.Helpers.Constants;

    /// <summary>
    /// Service for managing the SHTC3 sensor, including starting, stopping, and retrieving data.
    /// </summary>
    internal class Shtc3SensorService : ISensorService
    {
        private const int DataPin = 21;
        private const int ClockPin = 22;

        private readonly LogHelper _logHelper;
        private readonly ManualResetEvent _stopSignal = new ManualResetEvent(false);

        private double _temperature = InvalidTemperature;
        private double _humidity = InvalidHumidity;
        private bool _isRunning;
        private I2cDevice _device;

        /// <summary>
        /// Initializes a new instance of the <see cref="Shtc3SensorService"/> class.
        /// </summary>
        public Shtc3SensorService()
        {
            _logHelper = new LogHelper();
        }

        /// <summary>
        /// Starts the sensor service.
        /// </summary>
        public void Start()
        {
            if (_isRunning)
            {
                return;
            }

            _isRunning = true;

            Configuration.SetPinFunction(DataPin, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(ClockPin, DeviceFunction.I2C1_CLOCK);

            I2cConnectionSettings settings = new I2cConnectionSettings(1, Shtc3.DefaultI2cAddress);
            _device = I2cDevice.Create(settings);

            Thread sensorReadThread = new Thread(this.StartReceivingData);
            sensorReadThread.Start();
        }

        /// <summary>
        /// Stops the sensor service.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;

            _stopSignal.Set();
            _device.Dispose();
        }

        /// <summary>
        /// Retrieves the sensor data.
        /// </summary>
        /// <returns>An array of doubles containing the temperature and humidity data.</returns>
        public double[] GetData()
        {
            return new[] { _temperature, _humidity };
        }

        /// <summary>
        /// Retrieves the temperature reading from the sensor.
        /// </summary>
        /// <returns>The temperature value recorded by the sensor.</returns>
        public double GetTemp()
        {
            return _temperature;
        }

        /// <summary>
        /// Retrieves the humidity reading from the sensor.
        /// </summary>
        /// <returns>The humidity value recorded by the sensor.</returns>
        public double GetHumidity()
        {
            return _humidity;
        }

        /// <summary>
        /// Retrieves the type of the sensor.
        /// </summary>
        /// <returns>A string representing the type of the sensor.</returns>
        public string GetSensorType()
        {
            return "SHTC3";
        }

        /// <summary>
        /// Starts the thread for receiving data from the sensor.
        /// </summary>
        private void StartReceivingData()
        {
            using (Shtc3 sensor = new Shtc3(_device))
            {
                while (_isRunning)
                {
                    try
                    {
                        if (!_stopSignal.WaitOne(0, true))
                        {
                            if (sensor.TryGetTemperatureAndHumidity(out var temperature, out var relativeHumidity))
                            {
                                _temperature = temperature.DegreesCelsius;
                                _humidity = relativeHumidity.Percent;

                                _stopSignal.WaitOne(ReadInterval, false);
                            }
                            else
                            {
                                this.SetErrorValues();
                                _stopSignal.WaitOne(ErrorInterval, false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logHelper.LogWithTimestamp($"Error reading sensor data: {ex.Message}");
                        this.SetErrorValues();
                        _stopSignal.WaitOne(ErrorInterval, false);
                    }
                }
            }
        }

        /// <summary>
        /// Sets error values for temperature and humidity.
        /// </summary>
        private void SetErrorValues()
        {
            _temperature = InvalidTemperature;
            _humidity = InvalidHumidity;
        }
    }
}
