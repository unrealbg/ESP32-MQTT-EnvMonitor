namespace ESP32_NF_MQTT_DHT.Services.Sensors
{
    using System;

    using Helpers;

    using Iot.Device.DHTxx.Esp32;

    /// <summary>
    /// Service for managing the DHT21 sensor.
    /// </summary>
    internal class DhtService : BaseSensorService
    {
        private const int PinEcho = 26;
        private const int PinTrigger = 27;
        private Dht21 _dht;

        /// <summary>
        /// Initializes a new instance of the <see cref="DhtService"/> class.
        /// </summary>
        public DhtService()
        {
            _dht = new Dht21(PinEcho, PinTrigger);
        }

        /// <summary>
        /// Gets the type of the sensor.
        /// </summary>
        /// <returns>A string representing the sensor type.</returns>
        public override string GetSensorType() => "DHT21";

        /// <summary>
        /// Reads the sensor data and updates the temperature and humidity values.
        /// </summary>
        protected override void ReadSensorData()
        {
            try
            {
                _temperature = _dht.Temperature.DegreesCelsius;
                _humidity = _dht.Humidity.Percent;

                if (!_dht.IsLastReadSuccessful)
                {
                    this.SetErrorValues();
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Error in DhtServiceOptimized:", ex);
                this.SetErrorValues();
            }
        }
    }
}
