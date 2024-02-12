namespace ESP32_NF_MQTT_DHT.Controllers
{
    using Helpers;

    using Microsoft.Extensions.Logging;

    using Models;

    using nanoFramework.Json;
    using nanoFramework.WebServer;

    using System;
    using System.Net;

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
            SendPage(e, HtmlPages.IndexPage);
        }

        [Route("/documentation")]
        [Method("GET")]
        public void GetDocPage(WebServerEventArgs e)
        {
            SendPage(e, HtmlPages.DocPage);
        }

        [Route("api/temperature")]
        [Method("GET")]
        public void GetTemperature(WebServerEventArgs e)
        {
            try
            {
                var temperature = FetchTemperature();
                if (IsValidTemperature(temperature))
                {
                    var jsonResponse = $"{{\"temperature\": {temperature:f2}}}";
                    SendResponse(e, jsonResponse, "application/json");
                }
                else
                {
                    SendErrorResponse(e, "Temperature data is out of expected range.", HttpStatusCode.InternalServerError);
                }
            }
            catch (Exception ex)
            {
                SendErrorResponse(e, "An unexpected error occurred.", HttpStatusCode.InternalServerError);
                _logger.LogError($"An unexpected error occurred: {ex.Message}");
            }
        }

        [Route("api/humidity")]
        [Method("GET")]
        public void GetHumidity(WebServerEventArgs e)
        {
            try
            {
                var humidity = FetchHumidity();
                if (!double.IsNaN(humidity))
                {
                    var jsonResponse = $"{{\"humidity\": {humidity:f1}}}";
                    SendResponse(e, jsonResponse, "application/json");
                }
                else
                {
                    SendErrorResponse(e, "Humidity data is unavailable.", HttpStatusCode.InternalServerError);
                }
            }
            catch (Exception ex)
            {
                SendErrorResponse(e, $"An unexpected error occurred: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        [Route("api/data")]
        [Method("GET")]
        public void GetData(WebServerEventArgs e)
        {
            try
            {
                var temperature = FetchTemperature();
                var humidity = FetchHumidity();
                if (!double.IsNaN(temperature) && !double.IsNaN(humidity))
                {
                    var sensorData = new Sensor
                    {
                        Data = new Data
                        {
                            Date = DateTime.UtcNow.ToString("dd/MM/yyyy"),
                            Time = DateTime.UtcNow.ToString("HH:mm:ss"),
                            Temp = temperature,
                            Humid = (int)humidity
                        }
                    };

                    var jsonResponse = JsonSerializer.SerializeObject(sensorData);
                    SendResponse(e, jsonResponse, "application/json");
                }
                else
                {
                    SendErrorResponse(e, "Sensor data is unavailable.", HttpStatusCode.InternalServerError);
                }
            }
            catch (Exception ex)
            {
                SendErrorResponse(e, $"An unexpected error occurred: {ex.Message}", HttpStatusCode.InternalServerError);
            }
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

        private void SendResponse(WebServerEventArgs e, string content, string contentType)
        {
            try
            {
                e.Context.Response.ContentType = contentType;
                WebServer.OutPutStream(e.Context.Response, content);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send response: {ex.Message}");
            }
        }

        private void SendPage(WebServerEventArgs e, string page)
        {
            SendResponse(e, page, "text/html");
        }

        private void SendErrorResponse(WebServerEventArgs e, string logMessage, HttpStatusCode statusCode)
        {
            var clientMessage = "An error occurred. Please try again later.";
            e.Context.Response.StatusCode = (int)statusCode;
            SendResponse(e, $"{{\"error\": \"{clientMessage}\"}}", "application/json");
            _logger.LogError(logMessage);
        }

        private bool IsValidTemperature(double temperature)
        {
            return temperature >= -40 && temperature <= 85;
        }
    }
}
