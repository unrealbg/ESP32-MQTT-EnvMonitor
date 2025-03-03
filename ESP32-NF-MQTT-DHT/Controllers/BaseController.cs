namespace ESP32_NF_MQTT_DHT.Controllers
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Net;
    using System.Text;

    using ESP32_NF_MQTT_DHT.Helpers;

    using nanoFramework.WebServer;

    /// <summary>
    /// Abstract base class for handling common web server functionalities such as authentication,
    /// request throttling, and response handling.
    /// </summary>
    public abstract class BaseController
    {
        private static readonly Hashtable RequestTimesByEndpoint = new Hashtable();
        private static readonly Hashtable BanList = new Hashtable();
        private static readonly TimeSpan RequestInterval = TimeSpan.FromMilliseconds(200);
        private static readonly TimeSpan BanDuration = TimeSpan.FromMinutes(5);
        private static readonly object SyncLock = new object();

        /// <summary>
        /// Sends an HTML page as a response.
        /// </summary>
        /// <param name="e">The web server event arguments.</param>
        /// <param name="page">The HTML content to send.</param>
        protected void SendPage(WebServerEventArgs e, string page)
        {
            this.SendResponse(e, page, "text/html");
        }

        /// <summary>
        /// Checks if the request is authenticated.
        /// </summary>
        /// <param name="e">The web server event arguments.</param>
        /// <returns><c>true</c> if the request is authenticated; otherwise, <c>false</c>.</returns>
        protected bool IsAuthenticated(WebServerEventArgs e)
        {
            var authHeader = e.Context.Request.Headers["Authorization"];
            return authHeader != null && this.ValidateAuthHeader(authHeader);
        }

        /// <summary>
        /// Sends an unauthorized response.
        /// </summary>
        /// <param name="e">The web server event arguments.</param>
        protected void SendUnauthorizedResponse(WebServerEventArgs e)
        {
            string responseMessage = "Unauthorized access. Please provide valid credentials.";
            e.Context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            e.Context.Response.ContentType = "text/plain";
            WebServer.OutPutStream(e.Context.Response, responseMessage);
            LogHelper.LogError("Unauthorized access.");
        }

        /// <summary>
        /// Sends a not found response.
        /// </summary>
        /// <param name="e">The web server event arguments.</param>
        protected void SendNotFoundResponse(WebServerEventArgs e)
        {
            string responseMessage = "The requested resource was not found.";
            e.Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            e.Context.Response.ContentType = "text/plain";
            WebServer.OutPutStream(e.Context.Response, responseMessage);
            Debug.WriteLine($"Not Found response sent to {e.Context.Request.RemoteEndPoint}");
            LogHelper.LogError("Resource not found.");
        }

        /// <summary>
        /// Sends an error response.
        /// </summary>
        /// <param name="e">The web server event arguments.</param>
        /// <param name="logMessage">The log message to record.</param>
        /// <param name="statusCode">The HTTP status code to send.</param>
        protected void SendErrorResponse(WebServerEventArgs e, string logMessage, HttpStatusCode statusCode)
        {
            var clientMessage = "An error occurred. Please try again later.";
            e.Context.Response.StatusCode = (int)statusCode;
            this.SendResponse(e, $"{{\"error\": \"{clientMessage}\"}}", "application/json");
            LogHelper.LogError(logMessage);
        }

        /// <summary>
        /// Sends a response with the specified content and content type.
        /// </summary>
        /// <param name="e">The web server event arguments.</param>
        /// <param name="content">The content to send.</param>
        /// <param name="contentType">The content type of the response.</param>
        /// <param name="statusCode">The HTTP status code to send.</param>
        protected void SendResponse(WebServerEventArgs e, string content, string contentType = "application/json", HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            try
            {
                using (var response = e.Context.Response)
                {
                    response.StatusCode = (int)statusCode;
                    response.ContentType = contentType;
                    WebServer.OutPutStream(response, content);
                }
            }
            catch (System.Net.Sockets.SocketException sockEx)
            {
                LogHelper.LogError($"SocketException while sending response: {sockEx.Message}");
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"Failed to send response: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles a web server request, including throttling and banning logic.
        /// </summary>
        /// <param name="e">The web server event arguments.</param>
        /// <param name="action">The action to execute for the request.</param>
        /// <param name="endpoint">The endpoint being accessed.</param>
        protected void HandleRequest(WebServerEventArgs e, Action action, string endpoint)
        {
            string clientIp = e.Context.Request.RemoteEndPoint.Address.ToString();

            lock (SyncLock)
            {
                if (this.IsBanned(clientIp))
                {
                    this.SendForbiddenResponse(e);
                    return;
                }

                if (this.ShouldThrottle(clientIp, endpoint))
                {
                    this.BanClient(clientIp);
                    this.SendThrottleResponse(e);
                    return;
                }

                this.CleanupOldRequests(clientIp, endpoint);
            }

            action.Invoke();
            this.UpdateLastRequestTime(clientIp, endpoint);
        }

        /// <summary>
        /// Sends a forbidden response.
        /// </summary>
        /// <param name="e">The web server event arguments.</param>
        protected void SendForbiddenResponse(WebServerEventArgs e)
        {
            string responseMessage = "Your access is temporarily suspended due to excessive requests.";
            e.Context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            e.Context.Response.ContentType = "text/plain";
            WebServer.OutPutStream(e.Context.Response, responseMessage);
            //Debug.WriteLine($"Forbidden response sent to {e.Context.Request.RemoteEndPoint}");
            LogHelper.LogWarning("Access forbidden.");
        }

        /// <summary>
        /// Sends a throttle response.
        /// </summary>
        /// <param name="e">The web server event arguments.</param>
        protected void SendThrottleResponse(WebServerEventArgs e)
        {
            string responseMessage = "Too many requests. You have been temporarily banned. Please wait 5 minutes.";
            e.Context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            e.Context.Response.ContentType = "text/plain";
            WebServer.OutPutStream(e.Context.Response, responseMessage);
            //Debug.WriteLine($"Throttle response sent to {e.Context.Request.RemoteEndPoint}");
            LogHelper.LogWarning("Request throttled.");
        }

        /// <summary>
        /// Cleans up old requests from the request times hashtable.
        /// </summary>
        /// <param name="clientIp">The client's IP address.</param>
        /// <param name="endpoint">The endpoint being accessed.</param>
        private void CleanupOldRequests(string clientIp, string endpoint)
        {
            string key = $"{clientIp}_{endpoint}";
            DateTime threshold = DateTime.UtcNow.AddMinutes(-5);

            if (RequestTimesByEndpoint.Contains(key) && (DateTime)RequestTimesByEndpoint[key] < threshold)
            {
                RequestTimesByEndpoint.Remove(key);
            }
        }

        /// <summary>
        /// Checks if the client is banned.
        /// </summary>
        /// <param name="clientIp">The client's IP address.</param>
        /// <returns><c>true</c> if the client is banned; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Checks if the client should be throttled based on request frequency.
        /// </summary>
        /// <param name="clientIp">The client's IP address.</param>
        /// <param name="endpoint">The endpoint being accessed.</param>
        /// <returns><c>true</c> if the client should be throttled; otherwise, <c>false</c>.</returns>
        private bool ShouldThrottle(string clientIp, string endpoint)
        {
            string key = $"{clientIp}_{endpoint}";

            if (RequestTimesByEndpoint.Contains(key))
            {
                DateTime lastRequestTime = (DateTime)RequestTimesByEndpoint[key];
                return DateTime.UtcNow - lastRequestTime < RequestInterval;
            }

            return false;
        }

        /// <summary>
        /// Bans the client by adding them to the ban list.
        /// </summary>
        /// <param name="clientIp">The client's IP address.</param>
        private void BanClient(string clientIp)
        {
            BanList[clientIp] = DateTime.UtcNow.Add(BanDuration);
            LogHelper.LogWarning($"Client {clientIp} has been banned until {BanList[clientIp]}.");
        }

        /// <summary>
        /// Updates the last request time for the client and endpoint.
        /// </summary>
        /// <param name="clientIp">The client's IP address.</param>
        /// <param name="endpoint">The endpoint being accessed.</param>
        private void UpdateLastRequestTime(string clientIp, string endpoint)
        {
            string key = $"{clientIp}_{endpoint}";
            RequestTimesByEndpoint[key] = DateTime.UtcNow;
        }

        /// <summary>
        /// Validates the authorization header.
        /// </summary>
        /// <param name="authHeader">The authorization header value.</param>
        /// <returns><c>true</c> if the authorization header is valid; otherwise, <c>false</c>.</returns>
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
