namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Device.I2c;
    using System.Threading;

    using Helpers;

    using Iot.Device.Shtc3;

    using Microsoft.Extensions.Logging;

    using nanoFramework.Hardware.Esp32;

    using Services.Contracts;

    internal class Shtc3SensorService : ISensorService
    {
        private const int DataPin = 21;
        private const int ClockPin = 22;
        private const int ReadInterval = 60000;
        private const int ErrorInterval = 30000;
        private const int TempErrorValue = -50;
        private const int HumidityErrorValue = -100;

        private readonly LogHelper _logHelper;
        private readonly ManualResetEvent _stopSignal = new ManualResetEvent(false);

        private double _temperature = TempErrorValue;
        private double _humidity = HumidityErrorValue;
        private bool _isRunning;
        private I2cDevice _device;

        public Shtc3SensorService(ILoggerFactory loggerFactory)
        {
            _logHelper = new LogHelper(loggerFactory, nameof(Shtc3SensorService));
        }

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

        public void Stop()
        {
            _isRunning = false;

            _stopSignal.Set();
            _device.Dispose();
        }

        public double[] GetData()
        {
            return new[] { _temperature, _humidity };
        }

        public double GetTemp()
        {
            return _temperature;
        }

        public double GetHumidity()
        {
            return _humidity;
        }

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

                                this._logHelper.LogWithTimestamp(LogLevel.Information, "Data read from SHTC3 sensor");

                                _stopSignal.WaitOne(ReadInterval, false);
                            }
                            else
                            {
                                this.SetErrorValues();
                                _stopSignal.WaitOne(ErrorInterval, false);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        this._logHelper.LogWithTimestamp(LogLevel.Error, "Unable to read sensor data");
                        this.SetErrorValues();
                        _stopSignal.WaitOne(ErrorInterval, false);
                    }
                }
            }
        }

        private void SetErrorValues()
        {
            _temperature = TempErrorValue;
            _humidity = HumidityErrorValue;
        }
    }
}
