namespace ESP32_NF_MQTT_DHT.Controllers
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Net;
    using System.Text;

    using nanoFramework.WebServer;

    public abstract class BaseController
    {
        private static readonly Hashtable LastRequestTimes = new Hashtable();
        private static readonly Hashtable BanList = new Hashtable();
        private static readonly TimeSpan RequestInterval = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan BanDuration = TimeSpan.FromMinutes(5);
        private static readonly object SyncLock = new object();

        protected void SendPage(WebServerEventArgs e, string page)
        {
            this.SendResponse(e, page, "text/html");
        }

        protected bool IsAuthenticated(WebServerEventArgs e)
        {
            var authHeader = e.Context.Request.Headers["Authorization"];
            return authHeader != null && this.ValidateAuthHeader(authHeader);
        }

        protected void SendUnauthorizedResponse(WebServerEventArgs e)
        {
            string responseMessage = "Unauthorized access. Please provide valid credentials.";
            e.Context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            e.Context.Response.ContentType = "text/plain";
            WebServer.OutPutStream(e.Context.Response, responseMessage);
            Debug.WriteLine("Unauthorized response sent.");
        }

        protected void SendNotFoundResponse(WebServerEventArgs e)
        {
            string responseMessage = "The requested resource was not found.";
            e.Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            e.Context.Response.ContentType = "text/plain";
            WebServer.OutPutStream(e.Context.Response, responseMessage);
            Debug.WriteLine($"Not Found response sent to {e.Context.Request.RemoteEndPoint}");
        }

        protected void SendErrorResponse(WebServerEventArgs e, string logMessage, HttpStatusCode statusCode)
        {
            var clientMessage = "An error occurred. Please try again later.";
            e.Context.Response.StatusCode = (int)statusCode;
            this.SendResponse(e, $"{{\"error\": \"{clientMessage}\"}}", "application/json");
            Debug.WriteLine(logMessage);
        }

        protected void SendResponse(WebServerEventArgs e, string content, string contentType = "application/json", HttpStatusCode statusCode = HttpStatusCode.OK)
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

        protected void HandleRequest(WebServerEventArgs e, Action action)
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

        protected void SendForbiddenResponse(WebServerEventArgs e)
        {
            string responseMessage = "Your access is temporarily suspended due to excessive requests.";
            e.Context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            e.Context.Response.ContentType = "text/plain";
            WebServer.OutPutStream(e.Context.Response, responseMessage);
            Debug.WriteLine($"Forbidden response sent to {e.Context.Request.RemoteEndPoint}");
        }

        protected void SendThrottleResponse(WebServerEventArgs e)
        {
            string responseMessage = "Too many requests. You have been temporarily banned. Please wait 5 minutes.";
            e.Context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            e.Context.Response.ContentType = "text/plain";
            WebServer.OutPutStream(e.Context.Response, responseMessage);
            Debug.WriteLine($"Throttle response sent to {e.Context.Request.RemoteEndPoint}");
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
            BanList[clientIp] = DateTime.UtcNow.Add(BanDuration);
            Debug.WriteLine($"Client {clientIp} has been banned until {BanList[clientIp]}.");
        }

        private void UpdateLastRequestTime(string clientIp)
        {
            LastRequestTimes[clientIp] = DateTime.UtcNow;
        }

        private bool ValidateAuthHeader(string authHeader)
        {
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
            {
                return false;
            }

            var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
            var decodedBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(decodedBytes, 0, decodedBytes.Length);

            var parts = credentials.Split(':');
            if (parts.Length != 2)
            {
                return false;
            }

            var username = parts[0];
            var password = parts[1];

            return username == "expectedUsername" && password == "expectedPassword";
        }
    }
}
