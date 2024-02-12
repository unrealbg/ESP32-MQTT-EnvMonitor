namespace ESP32_NF_MQTT_DHT.Services
{
    using System;

    using Controllers;
    using Contracts;

    using Microsoft.Extensions.Logging;

    using nanoFramework.WebServer;

    public class WebServerService : IWebServerService
    {
        private readonly IConnectionService _connectionService;
        private readonly ILogger _logger;
        private WebServer _server;
        private bool _isServerRunning = false;

        public WebServerService(IConnectionService connectionService, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger(nameof(WebServerService)) ?? throw new ArgumentNullException(nameof(loggerFactory));
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));

            InitializeWebServer();

            _connectionService.ConnectionRestored += ConnectionRestored;
            _connectionService.ConnectionLost += ConnectionLost;
        }

        public void Start()
        {
            if (!_isServerRunning)
            {
                _server.Start();
                _isServerRunning = true;
                _logger.LogInformation("Web server started.");
            }
        }

        public void Stop()
        {
            if (_isServerRunning)
            {
                _server.Stop();
                _isServerRunning = false;
                _logger.LogInformation("Web server stopped.");
            }
        }

        public void Restart()
        {
            Stop();
            InitializeWebServer();
            Start();
        }

        private void ConnectionLost(object sender, EventArgs e)
        {
            _logger.LogInformation("Connection lost. Stopping the web server.");
            Stop();
        }

        private void ConnectionRestored(object sender, EventArgs e)
        {
            _logger.LogInformation("Connection restored. Starting the web server.");
            Restart();
        }

        private void InitializeWebServer()
        {
            if (_server != null)
            {
                _server.Dispose();
                _server = null;
            }

            _server = new WebServer(80, HttpProtocol.Http, new Type[] { typeof(SensorController) });
        }
    }
}
