namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using nanoFramework.WebServer;

    using Contracts;

    /// <summary>
    /// Represents a service that manages a web server.
    /// </summary>
    public class WebServerService : IWebServerService
    {
        private readonly WebServer _server;

        /// <summary>
        /// Initializes a new instance of the WebServerService class.
        /// </summary>
        /// <param name="port">The port on which the web server listens.</param>
        /// <param name="controllerTypes">The array of controller types that define web server endpoints.</param>
        public WebServerService(int port, Type[] controllerTypes)
        {
            _server = new WebServer(port, HttpProtocol.Http, controllerTypes);
        }

        /// <summary>
        /// Starts the web server.
        /// </summary>
        public void Start()
        {
            _server.Start();
        }
    }
}