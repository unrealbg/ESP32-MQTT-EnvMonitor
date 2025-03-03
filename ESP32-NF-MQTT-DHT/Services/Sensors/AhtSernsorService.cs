namespace ESP32_NF_MQTT_DHT.Services.Sensors
{
    using System.Device.I2c;

    using Iot.Device.Ahtxx;

    using nanoFramework.Hardware.Esp32;

    /// <summary>
    /// Service for managing the AHT10 sensor.
    /// </summary>
    public class AhtSensorService : BaseSensorService
    {
        private const int DataPin = 22;
        private const int ClockPin = 23;

        /// <summary>
        /// Gets the type of the sensor.
        /// </summary>
        /// <returns>A string representing the sensor type.</returns>
        public override string GetSensorType() => "AHT10";

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
        /// Reads the sensor data and updates the temperature and humidity values.
        /// </summary>
        protected override void ReadSensorData()
        {
            I2cConnectionSettings settings = new I2cConnectionSettings(1, AhtBase.DefaultI2cAddress);
            using (I2cDevice i2cDevice = I2cDevice.Create(settings))
            using (var aht = new Aht10(i2cDevice))
            {
                double temp = aht.GetTemperature().DegreesCelsius;
                double humid = aht.GetHumidity().Percent;

                if (temp < 45 && temp != -50)
                {
                    _temperature = temp;
                    _humidity = humid;
                }
                else
                {
                    this.SetErrorValues();
                }
            }
        }
    }
}