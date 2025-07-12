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

        private static readonly object StringBuilderLock = new object();

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
            try
            {
                e.Context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                e.Context.Response.ContentType = "text/html";
                e.Context.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"ESP32 Device Access\"");
                WebServer.OutPutStream(e.Context.Response, "Authentication required");
                LogHelper.LogWarning("Authentication required for access");
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Error sending unauthorized response", ex);
            }
        }

        protected void SendNotFoundResponse(WebServerEventArgs e)
        {
            try
            {
                string responseMessage = "The requested resource was not found.";
                e.Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                e.Context.Response.ContentType = "text/plain";
                WebServer.OutPutStream(e.Context.Response, responseMessage);
                
                string remoteEndpoint = e.Context.Request.RemoteEndPoint != null ? 
                    e.Context.Request.RemoteEndPoint.ToString() : "Unknown";
                Debug.WriteLine("Not Found response sent to " + remoteEndpoint);
                LogHelper.LogError("Resource not found.");
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Error sending not found response", ex);
            }
        }

        protected void SendErrorResponse(WebServerEventArgs e, string logMessage, HttpStatusCode statusCode)
        {
            try
            {
                var clientMessage = "An error occurred. Please try again later.";
                e.Context.Response.StatusCode = (int)statusCode;
                
                string jsonResponse = "{\"error\": \"" + clientMessage + "\"}";
                
                this.SendResponse(e, jsonResponse, "application/json");
                LogHelper.LogError(logMessage);
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Error sending error response", ex);
            }
        }

        protected void SendResponse(WebServerEventArgs e, string content, string contentType = "application/json", HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            HttpListenerResponse response = null;
            try
            {
                Debug.WriteLine("Sending response with content type: " + contentType + ", status: " + statusCode);
                response = e.Context.Response;
                response.StatusCode = (int)statusCode;
                response.ContentType = contentType;
                
                WebServer.OutPutStream(response, content);
                Debug.WriteLine("Response sent successfully");
            }
            catch (System.Net.Sockets.SocketException sockEx)
            {
                Debug.WriteLine("SocketException while sending response: " + sockEx.Message);
                LogHelper.LogError("SocketException while sending response: " + sockEx.Message);
            }
            catch (ObjectDisposedException)
            {
                Debug.WriteLine("Response object was already disposed");
                LogHelper.LogWarning("Response object was already disposed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to send response: " + ex.Message);
                LogHelper.LogError("Failed to send response: " + ex.Message);
            }
        }

        protected void HandleRequest(WebServerEventArgs e, Action action, string endpoint)
        {
            if (!this.IsAuthenticated(e))
            {
                this.SendUnauthorizedResponse(e);
                return;
            }

            string clientIp = e.Context.Request.RemoteEndPoint != null && e.Context.Request.RemoteEndPoint.Address != null ? 
                             e.Context.Request.RemoteEndPoint.Address.ToString() : "Unknown";

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

            try
            {
                action.Invoke();
                this.UpdateLastRequestTime(clientIp, endpoint);
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Error handling request", ex);
                this.SendErrorResponse(e, "Request processing failed", HttpStatusCode.InternalServerError);
            }
        }

        protected void SendForbiddenResponse(WebServerEventArgs e)
        {
            try
            {
                string responseMessage = "Your access is temporarily suspended due to excessive requests.";
                e.Context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                e.Context.Response.ContentType = "text/plain";
                WebServer.OutPutStream(e.Context.Response, responseMessage);
                LogHelper.LogWarning("Access forbidden.");
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Error sending forbidden response", ex);
            }
        }

        protected void SendThrottleResponse(WebServerEventArgs e)
        {
            try
            {
                string responseMessage = "Too many requests. You have been temporarily banned. Please wait 5 minutes.";
                e.Context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                e.Context.Response.ContentType = "text/plain";
                WebServer.OutPutStream(e.Context.Response, responseMessage);
                LogHelper.LogWarning("Request throttled.");
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Error sending throttle response", ex);
            }
        }

        private static string[] ReadAllLines(string path)
        {
            var lines = new System.Collections.ArrayList();
            System.IO.FileStream fs = null;
            System.IO.StreamReader reader = null;

            try
            {
                fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                reader = new System.IO.StreamReader(fs);

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
                if (fs != null)
                {
                    fs.Dispose();
                }
            }

            var arr = new string[lines.Count];
            for (int i = 0; i < lines.Count; i++)
            {
                arr[i] = (string)lines[i];
            }

            return arr;
        }

        private void CleanupOldRequests(string clientIp, string endpoint)
        {
            try
            {
                string key = clientIp + "_" + endpoint;
                DateTime threshold = DateTime.UtcNow.AddMinutes(-5);

                if (RequestTimesByEndpoint.Contains(key))
                {
                    DateTime requestTime = (DateTime)RequestTimesByEndpoint[key];
                    if (requestTime < threshold)
                    {
                        RequestTimesByEndpoint.Remove(key);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Error during cleanup of old requests", ex);
            }
        }

        private bool IsBanned(string clientIp)
        {
            try
            {
                if (BanList.Contains(clientIp))
                {
                    DateTime banEndTime = (DateTime)BanList[clientIp];
                    if (DateTime.UtcNow <= banEndTime)
                    {
                        Debug.WriteLine("Access denied for " + clientIp + ". Still banned.");
                        return true;
                    }

                    BanList.Remove(clientIp);
                }

                return false;
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Error checking ban status", ex);
                return false;
            }
        }

        private bool ShouldThrottle(string clientIp, string endpoint)
        {
            try
            {
                string key = clientIp + "_" + endpoint;

                if (RequestTimesByEndpoint.Contains(key))
                {
                    DateTime lastRequestTime = (DateTime)RequestTimesByEndpoint[key];
                    return DateTime.UtcNow - lastRequestTime < RequestInterval;
                }

                return false;
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Error checking throttle status", ex);
                return false;
            }
        }

        private void BanClient(string clientIp)
        {
            try
            {
                DateTime banUntil = DateTime.UtcNow.Add(BanDuration);
                BanList[clientIp] = banUntil;
                LogHelper.LogWarning("Client " + clientIp + " has been banned until " + banUntil.ToString());
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Error banning client", ex);
            }
        }

        private void UpdateLastRequestTime(string clientIp, string endpoint)
        {
            try
            {
                string key = clientIp + "_" + endpoint;
                RequestTimesByEndpoint[key] = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Error updating request time", ex);
            }
        }

        private bool ValidateAuthHeader(string authHeader)
        {
            try
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

                return CredentialCache.Validate(username, password);
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Error validating credentials", ex);
                return false;
            }
        }
    }
}
