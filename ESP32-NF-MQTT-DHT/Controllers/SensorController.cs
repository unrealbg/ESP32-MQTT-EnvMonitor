namespace ESP32_NF_MQTT_DHT.Controllers
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Net;

    using Helpers;

    using Models;

    using nanoFramework.Json;
    using nanoFramework.WebServer;

    public class SensorController
    {
        private static readonly Hashtable LastRequestTimes = new Hashtable();
        private static readonly Hashtable BanList = new Hashtable();
        private static readonly TimeSpan RequestInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan BanDuration = TimeSpan.FromMinutes(5);
        private static readonly object SyncLock = new object();

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
                            var temperature = FetchTemperature();
                            if (IsValidTemperature(temperature))
                            {
                                var jsonResponse = $"{{\"temperature\": {temperature:f2}}}";
                                this.SendResponse(e, jsonResponse, "application/json");
                            }
                            else
                            {
                                this.SendErrorResponse(e, "Temperature data is out of expected range.", HttpStatusCode.InternalServerError);
                            }
                        }
                        catch (Exception ex)
                        {
                            this.SendErrorResponse(e, "An unexpected error occurred.", HttpStatusCode.InternalServerError);
                            Debug.WriteLine($"An unexpected error occurred: {ex.Message}");
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
                            var humidity = FetchHumidity();
                            if (!double.IsNaN(humidity))
                            {
                                var jsonResponse = $"{{\"humidity\": {humidity:f1}}}";
                                this.SendResponse(e, jsonResponse, "application/json");
                            }
                            else
                            {
                                this.SendErrorResponse(e, "Humidity data is unavailable.", HttpStatusCode.InternalServerError);
                            }
                        }
                        catch (Exception ex)
                        {
                            this.SendErrorResponse(e, $"An unexpected error occurred: {ex.Message}", HttpStatusCode.InternalServerError);
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
                            var temperature = double.Parse($"{FetchTemperature():f2}");
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
                                this.SendResponse(e, jsonResponse, "application/json");
                            }
                            else
                            {
                                this.SendErrorResponse(e, "Sensor data is unavailable.", HttpStatusCode.InternalServerError);
                            }
                        }
                        catch (Exception ex)
                        {
                            this.SendErrorResponse(e, $"An unexpected error occurred: {ex.Message}", HttpStatusCode.InternalServerError);
                        }
                    });
        }

        private static double FetchTemperature()
        {
            try
            {
                //return GlobalServices.DhtService.GetTemp();
                return GlobalServices.AhtSensorService.GetTemp();
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Failed to fetch temperature: {exception.Message}");
                return double.NaN;
            }
        }

        private static double FetchHumidity()
        {
            try
            {
                //return GlobalServices.DhtService.GetHumidity();
                return GlobalServices.AhtSensorService.GetHumidity();
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Failed to fetch humidity: {exception.Message}");
                return double.NaN;
            }
        }

        private void SendResponse(WebServerEventArgs e, string content, string contentType, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            try
            {
                e.Context.Response.StatusCode = (int)statusCode;
                e.Context.Response.ContentType = contentType;
                WebServer.OutPutStream(e.Context.Response, content);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to send response: {ex.Message}");
            }
        }

        private void SendPage(WebServerEventArgs e, string page)
        {
            this.SendResponse(e, page, "text/html");
        }

        private void SendErrorResponse(WebServerEventArgs e, string logMessage, HttpStatusCode statusCode)
        {
            var clientMessage = "An error occurred. Please try again later.";
            e.Context.Response.StatusCode = (int)statusCode;
            this.SendResponse(e, $"{{\"error\": \"{clientMessage}\"}}", "application/json");
            Debug.WriteLine(logMessage);
        }

        private bool IsValidTemperature(double temperature)
        {
            return temperature >= -40 && temperature <= 85;
        }

        private void HandleRequest(WebServerEventArgs e, Action action)
        {
            string clientIp = e.Context.Request.RemoteEndPoint.Address.ToString();

            lock (SyncLock)
            {
                if (this.IsBanned(clientIp))
                {
                    this.SendForbiddenResponse(e);
                    return;
                }

                if (this.ShouldThrottle(clientIp))
                {
                    this.BanClient(clientIp);
                    this.SendThrottleResponse(e);
                    return;
                }
            }

            action.Invoke();
            this.UpdateLastRequestTime(clientIp);
        }

        private bool IsBanned(string clientIp)
        {
            if (BanList.Contains(clientIp))
            {
                DateTime banEndTime = (DateTime)BanList[clientIp];
                if (DateTime.UtcNow <= banEndTime)
                {
                    Debug.WriteLine($"Access denied for {clientIp}. Still banned.");
                    return true;
                }

                BanList.Remove(clientIp);
            }

            return false;
        }

        private bool ShouldThrottle(string clientIp)
        {
            if (LastRequestTimes.Contains(clientIp))
            {
                DateTime lastRequestTime = (DateTime)LastRequestTimes[clientIp];
                return DateTime.UtcNow - lastRequestTime < RequestInterval;
            }

            return false;
        }

        private void BanClient(string clientIp)
        {
            // Add clientIP to banList
            BanList[clientIp] = DateTime.UtcNow.Add(BanDuration);
            Debug.WriteLine($"Client {clientIp} has been banned until {BanList[clientIp]}.");
        }

        private void UpdateLastRequestTime(string clientIp)
        {
            LastRequestTimes[clientIp] = DateTime.UtcNow;
        }

        private void SendForbiddenResponse(WebServerEventArgs e)
        {
            string responseMessage = "Your access is temporarily suspended due to excessive requests.";
            e.Context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            e.Context.Response.ContentType = "text/plain";
            WebServer.OutPutStream(e.Context.Response, responseMessage);
            Debug.WriteLine($"Forbidden response sent to {e.Context.Request.RemoteEndPoint}");
        }

        private void SendThrottleResponse(WebServerEventArgs e)
        {
            string responseMessage = "Too many requests. You have been temporarily banned. Please wait 5 minutes.";
            e.Context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            e.Context.Response.ContentType = "text/plain";
            WebServer.OutPutStream(e.Context.Response, responseMessage);
            Debug.WriteLine($"Throttle response sent to {e.Context.Request.RemoteEndPoint}");
        }
    }
}
