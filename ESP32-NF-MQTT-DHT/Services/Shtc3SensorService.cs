namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Device.I2c;
    using System.Threading;

    using Iot.Device.Shtc3;

    using Microsoft.Extensions.Logging;

    using nanoFramework.Hardware.Esp32;

    using Services.Contracts;

    internal class Shtc3SensorService : IShtc3SensorService
    {
        private const int DataPin = 4;
        private const int ClockPin = 5;
        private const int ReadInterval = 60000;
        private const int ErrorInterval = 30000;
        private const int TempErrorValue = -50;
        private const int HumidityErrorValue = -100;

        private readonly ILogger _logger;
        private readonly ManualResetEvent _stopSignal = new ManualResetEvent(false);

        private double _temperature = TempErrorValue;
        private double _humidity = HumidityErrorValue;
        private bool _isRunning;


        public Shtc3SensorService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(nameof(Shtc3SensorService));
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

            Thread sensorReadThread = new Thread(this.StartReceivingData);
            sensorReadThread.Start();
        }

        public void Stop()
        {
            _isRunning = false;

            _stopSignal.Set();
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
            I2cConnectionSettings settings = new I2cConnectionSettings(1, Shtc3.DefaultI2cAddress);
            I2cDevice device = I2cDevice.Create(settings);

            using (Shtc3 sensor = new Shtc3(device))
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

                                _logger.LogInformation($"Temp: {_temperature}\nHumid: {_humidity}");

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
                        _logger.LogError(e, "Error reading sensor data");
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
