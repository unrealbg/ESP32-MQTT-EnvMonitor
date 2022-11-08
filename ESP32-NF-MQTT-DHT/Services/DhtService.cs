namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;

    using Iot.Device.DHTxx.Esp32;

    using Models;

    using nanoFramework.Json;
    using nanoFramework.M2Mqtt.Messages;

    using Services.Contracts;

    internal class DhtService : IDhtService
    {
        private readonly IMqttClient _client;

        public DhtService(IMqttClient client)
        {
            this._client = client;
            this.JsonSerializer = new JsonSerializer();
            this.Device = new Device();
        }

        public Device Device { get; set; }

        public JsonSerializer JsonSerializer { get; set; }

        // Start Reading Sensor Data from DHT21 sensor
        public void Start()
        {
            Debug.WriteLine("[+] Start Reading Sensor Data from DHT21");
            
            using (Dht21 dht = new Dht21(26, 27))
            {
                while (true)
                {
                    try
                    {
                        var temperature = dht.Temperature.Value;
                        var humidity = dht.Humidity.Value;

                        // if the read is successful it sends data every minute
                        if (dht.IsLastReadSuccessful)
                        {
                            this.Device.Data.Date = DateTime.UtcNow.Date.ToString("dd/MM/yyyy");
                            this.Device.Data.Time = DateTime.UtcNow.ToString("HH:mm:ss");
                            this.Device.Data.Temp = temperature;
                            this.Device.Data.Humid = (int)humidity;

                            var msg = JsonSerializer.SerializeObject(this.Device);

                            this._client.MqttClient.Publish(
                                "IoT/messages2",
                                Encoding.UTF8.GetBytes(msg),
                                MqttQoSLevel.AtLeastOnce,
                                false);
                            Thread.Sleep(60000);
                        }
                        else
                        {
                            Debug.WriteLine("Error reading DHT sensor");
                            this._client.MqttClient.Publish(
                                "nf-mqtt/basic-dht",
                                Encoding.UTF8.GetBytes($"Error"),
                                MqttQoSLevel.AtLeastOnce,
                                false);
                            Thread.Sleep(10000);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
        }
    }
}
