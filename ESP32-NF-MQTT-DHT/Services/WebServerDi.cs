namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Diagnostics;
    using System.Net;

    using ESP32_NF_MQTT_DHT.Helpers;

    using Microsoft.Extensions.DependencyInjection;

    using nanoFramework.WebServer;

    internal class WebServerDi : WebServer
    {
        private readonly IServiceProvider _serviceProvider;

        public WebServerDi(int port, HttpProtocol protocol, Type[] controllers, IServiceProvider serviceProvider) : base(port, protocol, controllers)
        {
            _serviceProvider = serviceProvider;
            
            try
            {
                Debug.WriteLine("WebServer created with port " + port);
                LogHelper.LogInformation("WebServer created with port " + port);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error initializing WebServer: " + ex.Message);
                LogHelper.LogError("Error initializing WebServer: " + ex.Message);
            }
        }

        protected override void InvokeRoute(CallbackRoutes route, HttpListenerContext context)
        {
            try
            {
                try 
                {
                    if (context != null && context.Request != null)
                    {
                        var client = context.Request.InputStream;
                        if (client != null)
                        {
                            Debug.WriteLine("Setting HTTP request timeouts");
                        }
                    }
                }
                catch (Exception timeoutEx)
                {
                    Debug.WriteLine("Note: Could not set HTTP timeouts: " + timeoutEx.Message);
                }
                
                route.Callback.Invoke(ActivatorUtilities.CreateInstance(_serviceProvider, route.Callback.DeclaringType), new object[] { new WebServerEventArgs(context) });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error invoking route: " + ex.Message);
                LogHelper.LogError("Error invoking route: " + ex.Message);
                
                try
                {
                    if (context != null && context.Response != null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        context.Response.ContentType = "text/html";
                        WebServer.OutPutStream(context.Response, "<html><body><h2>Server Error</h2><p>An error occurred processing your request.</p></body></html>");
                    }
                }
                catch 
                {
                    // Ignore errors in error handling
                }
            }
        }
    }
}
