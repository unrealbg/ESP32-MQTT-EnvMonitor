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

    /// <summary>
    /// Controller for handling sensor-related HTTP requests.
    /// </summary>
    public class SensorController : BaseController
    {
        private readonly ISensorService _sensorService;
        private readonly IRelayService _relayService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorController"/> class.
        /// </summary>
        /// <param name="sensorService">The sensor service for retrieving sensor data.</param>
        /// <param name="relayService">The relay service for controlling relays.</param>
        public SensorController(
            ISensorService sensorService,
            IRelayService relayService)
        {
            _sensorService = sensorService ?? throw new ArgumentNullException(nameof(sensorService));
            _relayService = relayService ?? throw new ArgumentNullException(nameof(relayService));
        }

        /// <summary>
        /// Handles the request for the index page.
        /// </summary>
        /// <param name="e">The web server event arguments.</param>
        [Route("/")]
        [Method("GET")]
        public void Index(WebServerEventArgs e)
        {
            if (!this.IsAuthenticated(e))
            {
                this.SendUnauthorizedResponse(e);
                return;
            }

            try
            {
                string htmlContent = Html.GetIndexContent();
                this.SendResponse(e, htmlContent, "text/html");
            }
            catch (Exception ex)
            {
                this.SendErrorResponse(e, "Unable to load index page.", HttpStatusCode.InternalServerError);
                LogHelper.LogWarning("Failed to load index page.");
            }
        }

        /// <summary>
        /// Handles the request for getting the temperature.
        /// </summary>
        /// <param name="e">The web server event arguments.</param>
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
                                LogHelper.LogWarning("Temperature data is out of expected range.");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.SendErrorResponse(e, "An unexpected error occurred.", HttpStatusCode.InternalServerError);
                            LogHelper.LogError("An unexpected error occurred.", ex);
                        }
                    },
                "api/temperature");
        }

        /// <summary>
        /// Handles the request for getting the humidity.
        /// </summary>
        /// <param name="e">The web server event arguments.</param>
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
                                LogHelper.LogWarning("Humidity data is unavailable.");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.SendErrorResponse(e, $"An unexpected error occurred: {ex.Message}", HttpStatusCode.InternalServerError);
                            LogHelper.LogError($"An unexpected error occurred: {ex.Message}");
                        }
                    },
                "api/humidity");
        }

        /// <summary>
        /// Handles the request for getting the sensor data.
        /// </summary>
        /// <param name="e">The web server event arguments.</param>
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
                                        DateTime = DateTime.UtcNow,
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
                                LogHelper.LogWarning("Sensor data is unavailable.");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.SendErrorResponse(e, $"An unexpected error occurred: {ex.Message}", HttpStatusCode.InternalServerError);
                            LogHelper.LogError($"An unexpected error occurred: {ex.Message}");
                        }
                    },
                "api/data");
        }

        /// <summary>
        /// Handles the request for getting the relay status.
        /// </summary>
        /// <param name="e">The web server event arguments.</param>
        [Route("api/relay-status")]
        [Method("GET")]
        public void GetRelayStatus(WebServerEventArgs e)
        {
            this.HandleRequest(
                e,
                () =>
                    {
                        bool isRelayOn = _relayService.IsRelayOn();
                        var jsonResponse = $"{{\"isRelayOn\": {isRelayOn.ToString().ToLower()}}}";
                        this.SendResponse(e, jsonResponse, "application/json");
                    },
                "api/relay-status");
        }

        /// <summary>
        /// Handles the request for toggling the relay.
        /// </summary>
        /// <param name="e">The web server event arguments.</param>
        [Route("api/toggle-relay")]
        [Method("POST")]
        public void ToggleRelay(WebServerEventArgs e)
        {
            this.HandleRequest(
                e,
                () =>
                    {
                        bool isRelayOn = _relayService.IsRelayOn();

                        if (isRelayOn)
                        {
                            _relayService.TurnOff();
                            isRelayOn = false;
                        }
                        else
                        {
                            _relayService.TurnOn();
                            isRelayOn = true;
                        }

                        string jsonResponse = $"{{\"isRelayOn\": {isRelayOn.ToString().ToLower()}}}";
                        this.SendResponse(e, jsonResponse, "application/json");
                    },
                "api/toggle-relay");
        }

        /// <summary>
        /// Fetches the temperature from the sensor service.
        /// </summary>
        /// <returns>The temperature value.</returns>
        private double FetchTemperature()
        {
            try
            {
                return _sensorService.GetTemp();
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Failed to fetch temperature.", ex);
                return double.NaN;
            }
        }

        /// <summary>
        /// Fetches the humidity from the sensor service.
        /// </summary>
        /// <returns>The humidity value.</returns>
        private double FetchHumidity()
        {
            try
            {
                return _sensorService.GetHumidity();
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Failed to fetch humidity.", ex);
                return double.NaN;
            }
        }

        /// <summary>
        /// Validates the temperature value.
        /// </summary>
        /// <param name="temperature">The temperature value to validate.</param>
        /// <returns><c>true</c> if the temperature is within the valid range; otherwise, <c>false</c>.</returns>
        private bool IsValidTemperature(double temperature)
        {
            return temperature >= -40 && temperature <= 85;
        }
    }
}