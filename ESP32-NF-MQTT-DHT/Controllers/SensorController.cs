namespace ESP32_NF_MQTT_DHT.Controllers
{
    using System;
    using System.Net;

    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.HTML;
    using ESP32_NF_MQTT_DHT.Services.Contracts;

    using Models;

    using nanoFramework.Json;
    using nanoFramework.WebServer;

    [Authentication("Basic:user p@ssw0rd")]
    public class SensorController : BaseController
    {
        private readonly ISensorService _sensorService;
        private readonly LogHelper _logger;

        public SensorController(
            ISensorService sensorService)
        {
            _sensorService = sensorService ?? throw new ArgumentNullException(nameof(sensorService));
            _logger = new LogHelper();
        }

        [Route("/")]
        [Method("GET")]
        public void Index(WebServerEventArgs e)
        {
            this.HandleRequest(
        e,
        () =>
        {
            try
            {
                string htmlContent = Html.GetIndexContent();

                this.SendResponse(e, htmlContent, "text/html");
            }
            catch (Exception ex)
            {
                this.SendErrorResponse(e, "Unable to load index page.", HttpStatusCode.InternalServerError);
                _logger.LogWithTimestamp("Failed to load index page.");
            }
        });
        }

        [Route("api/temperature")]
        [Method("GET")]
        public void GetTemperature(WebServerEventArgs e)
        {
            this.HandleRequest(
                e,
                () =>
                    {
                        try
                        {
                            var temperature = this.FetchTemperature();
                            if (this.IsValidTemperature(temperature))
                            {
                                var jsonResponse = $"{{\"temperature\": {temperature:f2}}}";
                                this.SendResponse(e, jsonResponse, "application/json");
                            }
                            else
                            {
                                this.SendErrorResponse(e, "Temperature data is out of expected range.", HttpStatusCode.InternalServerError);
                                _logger.LogWithTimestamp("Temperature data is out of expected range.");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.SendErrorResponse(e, "An unexpected error occurred.", HttpStatusCode.InternalServerError);
                            _logger.LogWithTimestamp("An unexpected error occurred.");
                        }
                    });
        }

        [Route("api/humidity")]
        [Method("GET")]
        public void GetHumidity(WebServerEventArgs e)
        {
            this.HandleRequest(
                e,
                () =>
                    {
                        try
                        {
                            var humidity = this.FetchHumidity();
                            if (!double.IsNaN(humidity))
                            {
                                var jsonResponse = $"{{\"humidity\": {humidity:f1}}}";
                                this.SendResponse(e, jsonResponse, "application/json");
                            }
                            else
                            {
                                this.SendErrorResponse(e, "Humidity data is unavailable.", HttpStatusCode.InternalServerError);
                                _logger.LogWithTimestamp("Humidity data is unavailable.");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.SendErrorResponse(e, $"An unexpected error occurred: {ex.Message}", HttpStatusCode.InternalServerError);
                            _logger.LogWithTimestamp("An unexpected error occurred: { ex.Message}");
                        }
                    });
        }

        [Route("api/data")]
        [Method("GET")]
        public void GetData(WebServerEventArgs e)
        {
            this.HandleRequest(
                e,
                () =>
                    {
                        try
                        {
                            var temperature = double.Parse($"{this.FetchTemperature():f2}");
                            var humidity = this.FetchHumidity();
                            var sensorType = _sensorService.GetSensorType();

                            if (!double.IsNaN(temperature) && !double.IsNaN(humidity))
                            {
                                var sensorData = new Sensor
                                {
                                    Data = new Data
                                    {
                                        Date = DateTime.UtcNow.ToString("dd/MM/yyyy"),
                                        Time = DateTime.UtcNow.ToString("HH:mm:ss"),
                                        Temp = temperature,
                                        Humid = (int)humidity,
                                        SensorType = sensorType
                                    }
                                };

                                var jsonResponse = JsonSerializer.SerializeObject(sensorData);
                                this.SendResponse(e, jsonResponse, "application/json");
                            }
                            else
                            {
                                this.SendErrorResponse(e, "Sensor data is unavailable.", HttpStatusCode.InternalServerError);
                                _logger.LogWithTimestamp("Sensor data is unavailable.");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.SendErrorResponse(e, $"An unexpected error occurred: {ex.Message}", HttpStatusCode.InternalServerError);
                            _logger.LogWithTimestamp("An unexpected error occurred.");
                        }
                    });
        }

        private double FetchTemperature()
        {
            try
            {
                return _sensorService.GetTemp();
            }
            catch (Exception exception)
            {
                _logger.LogWithTimestamp(exception, "Failed to fetch temperature.");
                return double.NaN;
            }
        }

        private double FetchHumidity()
        {
            try
            {
                return _sensorService.GetHumidity();
            }
            catch (Exception exception)
            {
                _logger.LogWithTimestamp(exception, "Failed to fetch humidity.");
                return double.NaN;
            }
        }

        private bool IsValidTemperature(double temperature)
        {
            return temperature >= -40 && temperature <= 85;
        }
    }
}
