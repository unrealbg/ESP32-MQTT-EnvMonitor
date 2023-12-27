namespace ESP32_NF_MQTT_DHT.Services
{
    using Contracts;
    using Models;

    using nanoFramework.Hardware.Esp32;
    using nanoFramework.Logging;
    using nanoFramework.Json;

    using Iot.Device.Ahtxx;

    using System;
    using System.Device.I2c;
    using System.Text;
    using System.Threading;

    using Microsoft.Extensions.Logging;

    using static Constants.Constants;

    public class AhtSensorService : IAhtSensorService
    {
        private readonly IMqttClientService _mqttClientService;
        private double _temperature;
        private double _humidity;

        public AhtSensorService(IMqttClientService mqttClientService)
        {
            _mqttClientService = mqttClientService;
        }

        public Sensor Sensor { get; set; }

        public void Start()
        {
            Configuration.SetPinFunction(4, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(5, DeviceFunction.I2C1_CLOCK);

            Thread sensorReadThread = new Thread(StartReceivingData);
            sensorReadThread.Start();
        }

        public double GetTemp() => _temperature;
        public double GetHumidity() => _humidity;

        private void StartReceivingData()
        {
            I2cConnectionSettings i2CSettings = new I2cConnectionSettings(1, AhtBase.DefaultI2cAddress);
            I2cDevice i2CDevice = I2cDevice.Create(i2CSettings);

            using (var aht = new Aht10(i2CDevice))
            {
                while (true)
                {
                    try
                    {
                        _temperature = aht.GetTemperature().DegreesCelsius;
                        _humidity = aht.GetHumidity().Percent;

                        if (_temperature < 45)
                        {
                            var deviceData = new Sensor
                            {
                                Data = new Data
                                {
                                    Date = DateTime.UtcNow.Date.ToString("dd/MM/yyyy"),
                                    Time = DateTime.UtcNow.AddHours(2).ToString("HH:mm:ss"),
                                    Temp = _temperature,
                                    Humid = (int)_humidity
                                }
                            };

                            var msg = JsonSerializer.SerializeObject(deviceData);
                            _mqttClientService.MqttClient.Publish("IoT/messages", Encoding.UTF8.GetBytes(msg));
                        }
                        else
                        {
                            _mqttClientService.MqttClient.Publish($"home/{Device}/errors", Encoding.UTF8.GetBytes("Error reading from DHT sensor"));
                        }

                        Thread.Sleep(60000); // Adjust time as needed
                    }
                    catch (Exception ex)
                    {
                        aht.GetCurrentClassLogger().LogError($"[e] ERROR: {ex.Message}");
                    }
                }
            }
        }
    }
}