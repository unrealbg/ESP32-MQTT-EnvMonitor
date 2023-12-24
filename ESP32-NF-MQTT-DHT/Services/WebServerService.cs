namespace ESP32_NF_MQTT_DHT.Services
{
    using System;

    using nanoFramework.WebServer;

    using Contracts;
    public class WebServerService : IWebServerService
    {
        private readonly WebServer _server;

        public WebServerService(int port, Type[] controllerTypes)
        {
            _server = new WebServer(port, HttpProtocol.Http, controllerTypes);
        }

        public void Start()
        {
            _server.Start();
        }
    }
}