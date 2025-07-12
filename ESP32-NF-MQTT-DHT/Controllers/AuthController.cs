namespace ESP32_NF_MQTT_DHT.Controllers
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Text;

    using ESP32_NF_MQTT_DHT.Helpers;

    using nanoFramework.WebServer;

    /// <summary>
    /// Controller for handling authentication-related actions such as changing the password.
    /// </summary>
    public class AuthController : BaseController
    {
        private const string ChangePasswordHtml = 
            "<html><head><title>Change Password</title></head><body>" +
            "<form method='post' action='/change-password'>" +
            "Username:<input name='username' value='admin'><br>" +
            "Password:<input type='password' name='password'><br>" +
            "<input type='submit' value='Change'>" +
            "</form></body></html>";

        private const string ErrorHtml = 
            "<html><body><h3>Error: Username and password required</h3><a href='/change-password'>Back</a></body></html>";

        private const string SuccessHtml = 
            "<html><body><h3>Credentials updated successfully.</h3><a href='/'>Home</a></body></html>";

        private const string ProcessingErrorHtml = 
            "<html><body><h3>Error processing request. Try again.</h3><a href='/change-password'>Back</a></body></html>";

        [Route("/change-password")]
        [Method("GET")]
        public void ChangePasswordPage(WebServerEventArgs e)
        {
            Debug.WriteLine("GET request for change-password page");
            
            if (!this.IsAuthenticated(e))
            {
                this.SendUnauthorizedResponse(e);
                return;
            }

            this.SendResponse(e, ChangePasswordHtml, "text/html");
        }

        [Route("/change-password")]
        [Method("POST")]
        public void ChangePassword(WebServerEventArgs e)
        {
            Debug.WriteLine("=== Processing POST request for change-password ===");

            if (!this.IsAuthenticated(e))
            {
                Debug.WriteLine("Authentication failed for password change request");
                this.SendUnauthorizedResponse(e);
                return;
            }

            string body = null;
            try
            {
                Debug.WriteLine("Reading request body...");
                
                string contentLengthHeader = e.Context.Request.Headers["Content-Length"];
                int contentLength = 0;
                if (!string.IsNullOrEmpty(contentLengthHeader))
                {
                    int.TryParse(contentLengthHeader, out contentLength);
                }

                Debug.WriteLine("Content-Length: " + contentLength);

                body = ReadPostDataSafely(e.Context.Request.InputStream, contentLength);

                int bodyLength = body != null ? body.Length : 0;
                Debug.WriteLine("Form data received: " + bodyLength + " chars");
                Debug.WriteLine("Raw form data: " + (body ?? "null"));

                var credentials = this.ParseFormData(body);
                string usernameInfo = credentials.Username != null ? credentials.Username : "null";
                int passwordLength = credentials.Password != null ? credentials.Password.Length : 0;
                Debug.WriteLine("Parsed credentials - Username: " + usernameInfo + ", Password length: " + passwordLength);
                
                if (string.IsNullOrEmpty(credentials.Username) || string.IsNullOrEmpty(credentials.Password))
                {
                    Debug.WriteLine("Empty credentials provided - sending error response");
                    this.SendResponse(e, ErrorHtml, "text/html");
                    return;
                }

                Debug.WriteLine("Updating credentials for username: " + credentials.Username);
                CredentialCache.Update(credentials.Username, credentials.Password);
                Debug.WriteLine("Credentials updated successfully - sending success response");

                this.SendResponse(e, SuccessHtml, "text/html");
                Debug.WriteLine("Success response sent");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR processing form: " + ex.Message);
                Debug.WriteLine("Exception type: " + ex.GetType().Name);
                LogHelper.LogError("Error processing password change", ex);
                this.SendResponse(e, ProcessingErrorHtml, "text/html");
                Debug.WriteLine("Error response sent");
            }
            finally
            {
                Debug.WriteLine("=== Finished processing POST request ===");
            }
        }

        [Route("/change-success")]
        [Method("GET")]
        public void ChangeSuccess(WebServerEventArgs e)
        {
            if (!this.IsAuthenticated(e))
            {
                this.SendUnauthorizedResponse(e);
                return;
            }

            this.SendSimpleResponse(e, "Password updated successfully!", "/");
        }

        private static string UrlDecode(string value)
        {
            if (value == null)
            {
                return null;
            }

            var sb = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '%' && i + 2 < value.Length)
                {
                    try
                    {
                        string hex = value.Substring(i + 1, 2);
                        sb.Append((char)Convert.ToInt32(hex, 16));
                        i += 2;
                    }
                    catch
                    {
                        sb.Append(value[i]);
                    }
                }
                else if (value[i] == '+')
                {
                    sb.Append(' ');
                }
                else
                {
                    sb.Append(value[i]);
                }
            }
            return sb.ToString();
        }

        private static string ReadPostDataSafely(Stream inputStream, int contentLength)
        {
            Debug.WriteLine("Attempting to read POST data safely");

            try
            {
                string result = ReadPostData(inputStream, contentLength);
                if (!string.IsNullOrEmpty(result))
                {
                    Debug.WriteLine("Method 1 (chunked reading) succeeded");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Method 1 failed: " + ex.Message);
            }

            try
            {
                Debug.WriteLine("Trying Method 2: StreamReader with timeout");
                using (var reader = new StreamReader(inputStream))
                {
                    var startTime = DateTime.UtcNow;
                    var timeoutMs = 3000; // 3 seconds max

                    var sb = new StringBuilder();
                    char[] buffer = new char[256];
                    int charsRead;

                    while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
                    {
                        try
                        {
                            charsRead = reader.Read(buffer, 0, buffer.Length);
                            if (charsRead == 0)
                            {
                                break;
                            }

                            sb.Append(buffer, 0, charsRead);

                            if (sb.Length > 10 && sb.ToString().Contains("username=") && sb.ToString().Contains("password="))
                            {
                                break;
                            }
                        }
                        catch (Exception readEx)
                        {
                            Debug.WriteLine("Read exception in Method 2: " + readEx.Message);
                            break;
                        }
                    }

                    string result = sb.ToString();
                    if (!string.IsNullOrEmpty(result))
                    {
                        Debug.WriteLine("Method 2 (StreamReader with timeout) succeeded");
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Method 2 failed: " + ex.Message);
            }

            Debug.WriteLine("All read methods failed, returning empty string");
            return string.Empty;
        }

        private static string ReadPostData(Stream inputStream, int contentLength)
        {
            try
            {
                Debug.WriteLine("Reading POST data with content length: " + contentLength);

                // Set a reasonable timeout for the stream operation
                if (inputStream.CanTimeout)
                {
                    inputStream.ReadTimeout = 5000; // 5 seconds timeout
                    Debug.WriteLine("Set stream read timeout to 5 seconds");
                }

                if (contentLength <= 0 || contentLength > 4096)
                {
                    Debug.WriteLine("Invalid content length, attempting to read with buffer");
                    contentLength = 1024;
                }

                byte[] buffer = new byte[contentLength];
                int totalBytesRead = 0;
                int attempts = 0;
                const int maxAttempts = 10;

                while (totalBytesRead < contentLength && attempts < maxAttempts)
                {
                    attempts++;
                    int bytesToRead = Math.Min(256, contentLength - totalBytesRead);

                    try
                    {
                        int bytesRead = inputStream.Read(buffer, totalBytesRead, bytesToRead);

                        if (bytesRead == 0)
                        {
                            Debug.WriteLine("No more data available, breaking read loop at attempt " + attempts);
                            break;
                        }

                        totalBytesRead += bytesRead;
                        Debug.WriteLine("Attempt " + attempts + ": Read " + bytesRead + " bytes, total: " + totalBytesRead);
                    }
                    catch (IOException ioEx)
                    {
                        Debug.WriteLine("IO Exception during read attempt " + attempts + ": " + ioEx.Message);
                        break;
                    }
                    catch (System.Net.Sockets.SocketException sockEx)
                    {
                        Debug.WriteLine("Socket Exception during read attempt " + attempts + ": " + sockEx.Message);
                        break;
                    }
                }

                if (totalBytesRead > 0)
                {
                    string result = Encoding.UTF8.GetString(buffer, 0, totalBytesRead);
                    Debug.WriteLine("Successfully read POST data: " + totalBytesRead + " bytes");
                    return result;
                }
                else
                {
                    Debug.WriteLine("No POST data read after " + attempts + " attempts");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error reading POST data: " + ex.Message);
                Debug.WriteLine("Exception type: " + ex.GetType().Name);
                return string.Empty;
            }
        }

        private CredentialPair ParseFormData(string body)
        {
            if (string.IsNullOrEmpty(body))
            {
                return new CredentialPair(null, null);
            }

            string username = null;
            string password = null;

            string[] parts = body.Split('&');
            foreach (string part in parts)
            {
                string[] kv = part.Split('=');
                if (kv.Length == 2)
                {
                    switch (kv[0])
                    {
                        case "username":
                            username = UrlDecode(kv[1]);
                            break;
                        case "password":
                            password = UrlDecode(kv[1]);
                            break;
                    }
                }
            }

            return new CredentialPair(username, password);
        }
        
        private void SendSimpleResponse(WebServerEventArgs e, string message, string backLink)
        {
            string html = "<html><body><h3>" + message + "</h3><a href='" + backLink + "'>Back</a></body></html>";
            
            this.SendResponse(e, html, "text/html");
        }
        
        private void SendRedirectResponse(WebServerEventArgs e, string location)
        {
            try
            {
                HttpListenerResponse response = e.Context.Response;
                response.StatusCode = (int)HttpStatusCode.Redirect;
                response.Headers.Add("Location", location);
                response.Close();
                Debug.WriteLine("Redirect sent to " + location);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error sending redirect: " + ex.Message);
                LogHelper.LogError("Error sending redirect", ex);
            }
        }
        

        private struct CredentialPair
        {
            public string Username;
            public string Password;

            public CredentialPair(string username, string password)
            {
                this.Username = username;
                this.Password = password;
            }
        }
    }
}
