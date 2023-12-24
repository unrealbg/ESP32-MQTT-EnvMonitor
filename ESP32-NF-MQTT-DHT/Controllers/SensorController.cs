namespace ESP32_NF_MQTT_DHT.Controllers
{
    using Services.Contracts;

    using Microsoft.Extensions.Logging;

    using Models;

    using nanoFramework.Json;
    using nanoFramework.WebServer;

    using System;

    public class SensorController
    {
        private readonly IDhtService _dhtService;
        private ILogger _logger;
        private Sensor _device;

        public SensorController(IDhtService dhtService, ILoggerFactory loggerFactory)
        {
            _dhtService = dhtService;
            _device = new Sensor();
            _logger = loggerFactory?.CreateLogger(nameof(SensorController)) ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        [Route("api/temperature")]
        [Method("GET")]
        public void GetTemperature(WebServerEventArgs e)
        {
            var temperature = 0.0;

            try
            {
                temperature = _dhtService.GetTemp();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.Message);
            }

            var jsonResponse = $"{{\"temperature\": {temperature}}}";
            e.Context.Response.ContentType = "application/json";
            WebServer.OutPutStream(e.Context.Response, jsonResponse);
        }

        [Route("api/humidity")]
        [Method("GET")]
        public void GetHumidity(WebServerEventArgs e)
        {
            var humidity = 0.0;

            try
            {
                humidity = _dhtService.GetHumidity();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.Message);
            }

            var jsonResponse = $"{{\"humidity\": {humidity}}}";
            e.Context.Response.ContentType = "application/json";
            WebServer.OutPutStream(e.Context.Response, jsonResponse);
        }

        [Route("api/data")]
        [Method("GET")]
        public void GetData(WebServerEventArgs e)
        {
            var temperature = 0.0;
            var humidity = 0.0;

            try
            {
                temperature = _dhtService.GetTemp();
                humidity = _dhtService.GetHumidity();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.Message);
            }

            //UpdateDeviceData(temperature, humidity);
            var sensorData = new Sensor();
            sensorData.Data = new Data();
            sensorData.Data.Date = DateTime.UtcNow.Date.ToString("dd/MM/yyyy");
            sensorData.Data.Time = DateTime.UtcNow.ToString("HH:mm:ss");
            sensorData.Data.Temp = temperature;
            sensorData.Data.Humid = (int)humidity;

            var data = JsonSerializer.SerializeObject(sensorData);

            var jsonResponse = $"{{\"sensor\": {data}}}";
            e.Context.Response.ContentType = "application/json";
            WebServer.OutPutStream(e.Context.Response, jsonResponse);
        }

        private void UpdateDeviceData(double temperature, double humidity)
        {
            _device.Data.Date = DateTime.UtcNow.Date.ToString("dd/MM/yyyy");
            _device.Data.Time = DateTime.UtcNow.ToString("HH:mm:ss");
            _device.Data.Temp = temperature;
            _device.Data.Humid = (int)humidity;
        }
    }
}