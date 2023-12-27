namespace ESP32_NF_MQTT_DHT.Controllers
{
    using Helpers;

    using Microsoft.Extensions.Logging;

    using Models;

    using nanoFramework.Json;
    using nanoFramework.WebServer;

    using System;

    public class SensorController
    {
        private readonly ILogger _logger;

        public SensorController(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger(nameof(SensorController)) ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        [Route("/")]
        [Method("GET")]
        public void GetIndexPage(WebServerEventArgs e)
        {
            var htmlContent = HtmlPages.IndexPage;

            e.Context.Response.ContentType = "text/html";
            WebServer.OutPutStream(e.Context.Response, htmlContent);
        }

        [Route("api/temperature")]
        [Method("GET")]
        public void GetTemperature(WebServerEventArgs e)
        {
            var temperature = FetchTemperature();
            RespondWithJson(e, $"{{\"temperature\": {temperature:f2}}}");
        }

        [Route("api/humidity")]
        [Method("GET")]
        public void GetHumidity(WebServerEventArgs e)
        {
            var humidity = FetchHumidity();
            RespondWithJson(e, $"{{\"humidity\": {humidity:f1}}}");
        }

        [Route("api/data")]
        [Method("GET")]
        public void GetData(WebServerEventArgs e)
        {
            var temperature = FetchTemperature();
            var humidity = FetchHumidity();
            var sensorData = new Sensor
            {
                Data = new Data
                {
                    Date = DateTime.UtcNow.Date.ToString("dd/MM/yyyy"),
                    Time = DateTime.UtcNow.ToString("HH:mm:ss"),
                    Temp = temperature,
                    Humid = (int)humidity
                }
            };

            RespondWithJson(e, JsonSerializer.SerializeObject(sensorData));
        }

        private double FetchTemperature()
        {
            try
            {
                return GlobalServices.DhtService.GetTemp();
                //return GlobalServices.AhtSensorService.GetTemp();
            }
            catch (Exception exception)
            {
                _logger.LogError($"Failed to fetch temperature: {exception.Message}");
                return double.NaN;
            }
        }

        private double FetchHumidity()
        {
            try
            {
                return GlobalServices.DhtService.GetHumidity();
                //return GlobalServices.AhtSensorService.GetHumidity();
            }
            catch (Exception exception)
            {
                _logger.LogError($"Failed to fetch humidity: {exception.Message}");
                return double.NaN;
            }
        }

        private void RespondWithJson(WebServerEventArgs e, string jsonResponse)
        {
            e.Context.Response.ContentType = "application/json";
            WebServer.OutPutStream(e.Context.Response, jsonResponse);
        }
    }
}
