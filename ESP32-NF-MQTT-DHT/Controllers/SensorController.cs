namespace ESP32_NF_MQTT_DHT.Controllers
{
    using Helpers;

    using Models;

    using nanoFramework.Json;
    using nanoFramework.WebServer;

    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Net;

    public class SensorController
    {
        private static readonly Hashtable lastRequestTimes = new Hashtable();
        private static readonly Hashtable banList = new Hashtable();
        private static readonly TimeSpan requestInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan banDuration = TimeSpan.FromMinutes(5);
        private static readonly object syncLock = new object();

        [Route("/")]
        [Method("GET")]
        public void GetIndexPage(WebServerEventArgs e)
        {
            HandleRequest(e, () =>
            {
                SendPage(e, HtmlPages.IndexPage);
            });
        }

        [Route("api/temperature")]
        [Method("GET")]
        public void GetTemperature(WebServerEventArgs e)
        {
            HandleRequest(e, () =>
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
                    Debug.WriteLine($"An unexpected error occurred: {ex.Message}");
                }
            });
        }

        [Route("api/humidity")]
        [Method("GET")]
        public void GetHumidity(WebServerEventArgs e)
        {
            HandleRequest(e, () =>
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
            });
        }

        [Route("api/data")]
        [Method("GET")]
        public void GetData(WebServerEventArgs e)
        {
            HandleRequest(e, () =>
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
            });
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
                Debug.WriteLine($"Failed to fetch temperature: {exception.Message}");
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
            SendResponse(e, page, "text/html");
        }

        private void SendErrorResponse(WebServerEventArgs e, string logMessage, HttpStatusCode statusCode)
        {
            var clientMessage = "An error occurred. Please try again later.";
            e.Context.Response.StatusCode = (int)statusCode;
            SendResponse(e, $"{{\"error\": \"{clientMessage}\"}}", "application/json");
            Debug.WriteLine(logMessage);
        }

        private bool IsValidTemperature(double temperature)
        {
            return temperature >= -40 && temperature <= 85;
        }

        private void HandleRequest(WebServerEventArgs e, Action action)
        {
            string clientIP = e.Context.Request.RemoteEndPoint.Address.ToString();

            lock (syncLock)
            {
                if (IsBanned(clientIP))
                {
                    SendForbiddenResponse(e);
                    return;
                }

                if (ShouldThrottle(clientIP))
                {
                    BanClient(clientIP);
                    SendThrottleResponse(e);
                    return;
                }
            }

            action.Invoke();
            UpdateLastRequestTime(clientIP);
        }

        private bool IsBanned(string clientIP)
        {
            if (banList.Contains(clientIP))
            {
                DateTime banEndTime = (DateTime)banList[clientIP];
                if (DateTime.UtcNow <= banEndTime)
                {
                    Debug.WriteLine($"Access denied for {clientIP}. Still banned.");
                    return true;
                }

                banList.Remove(clientIP);
            }

            return false;
        }

        private bool ShouldThrottle(string clientIP)
        {
            if (lastRequestTimes.Contains(clientIP))
            {
                DateTime lastRequestTime = (DateTime)lastRequestTimes[clientIP];
                return DateTime.UtcNow - lastRequestTime < requestInterval;
            }

            return false;
        }

        private void BanClient(string clientIP)
        {
            // Add clientIP to banList
            banList[clientIP] = DateTime.UtcNow.Add(banDuration);
            Debug.WriteLine($"Client {clientIP} has been banned until {banList[clientIP]}.");
        }

        private void UpdateLastRequestTime(string clientIP)
        {
            lastRequestTimes[clientIP] = DateTime.UtcNow;
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
