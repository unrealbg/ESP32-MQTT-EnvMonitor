namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Device.I2c;
    using System.Threading;

    using Contracts;

    using Iot.Device.Ahtxx;

    using Microsoft.Extensions.Logging;

    using nanoFramework.Hardware.Esp32;

    using static Helpers.TimeHelper;

    /// <summary>
    /// Provides services for reading data from an AHT sensor.
    /// </summary>
    public class AhtSensorService : IAhtSensorService
    {
        private const int DataPin = 4;
        private const int ClockPin = 5;
        private const int ReadInterval = 60000;
        private const int ErrorInterval = 30000;

        private readonly ILogger _logger;
        private readonly ManualResetEvent _stopSignal = new ManualResetEvent(false);

        private double _temperature = -50;
        private double _humidity = -100;
        private bool _running;

        /// <summary>
        /// Initializes a new instance of the <see cref="AhtSensorService"/> class.
        /// </summary>
        /// <param name="loggerFactory">Factory to create a logger for this service.</param>
        public AhtSensorService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(nameof(AhtSensorService));
        }

        /// <summary>
        /// Starts the service to continuously read sensor data.
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
        /// Stops the service from reading sensor data.
        /// </summary>
        public void Stop()
        {
            _running = false;
            _stopSignal.Set();
        }

        /// <summary>
        /// Gets the temperature and humidity data.
        /// </summary>
        /// <returns>An array containing the temperature and humidity data.</returns>
        public double[] GetData() => new[] { _temperature, _humidity };

        /// <summary>
        /// Gets the temperature data.
        /// </summary>
        /// <returns>The temperature data.</returns>
        public double GetTemp() => _temperature;

        /// <summary>
        /// Gets the humidity data.
        /// </summary>
        /// <returns>The humidity data.</returns>
        public double GetHumidity() => _humidity;

        private void StartReceivingData()
        {
            I2cConnectionSettings i2CSettings = new I2cConnectionSettings(1, AhtBase.DefaultI2cAddress);
            using (I2cDevice i2CDevice = I2cDevice.Create(i2CSettings))
            using (var aht = new Aht10(i2CDevice))
            {
                while (_running)
                {
                    try
                    {
                        if (!_stopSignal.WaitOne(0, true))
                        {
                            _temperature = aht.GetTemperature().DegreesCelsius;
                            _humidity = aht.GetHumidity().Percent;

                            if (_temperature < 45 && _temperature != -50)
                            {
                                _logger.LogInformation($"[{GetCurrentTimestamp()}] Temp: {_temperature}, Humidity: {_humidity}");
                                _stopSignal.WaitOne(ReadInterval,false);
                            }
                            else
                            {
                                _logger.LogWarning($"[{GetCurrentTimestamp()}] Unable to read sensor data");
                                this.SetErrorValues();
                                _stopSignal.WaitOne(ErrorInterval, false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[{GetCurrentTimestamp()}] Sensor reading error: {ex.Message}");
                        this.SetErrorValues();
                        _stopSignal.WaitOne(ErrorInterval,false);
                    }
                }
            }
        }

        private void SetErrorValues()
        {
            _temperature = -50;
            _humidity = -100;
        }
    }
}
