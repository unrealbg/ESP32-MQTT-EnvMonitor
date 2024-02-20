namespace ESP32_NF_MQTT_DHT.Services
{
    using System;

    using Contracts;

    using Controllers;

    using Microsoft.Extensions.Logging;

    using nanoFramework.WebServer;

    /// <summary>
    /// Provides services for managing the web server.
    /// </summary>
    public class WebServerService : IWebServerService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnectionService _connectionService;
        private readonly ILogger _logger;
        private WebServer _server;
        private bool _isServerRunning = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServerService"/> class.
        /// </summary>
        /// <param name="connectionService">The connection service.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="serviceProvider">The service provider.</param>
        public WebServerService(IConnectionService connectionService, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            _logger = loggerFactory?.CreateLogger(nameof(WebServerService)) ?? throw new ArgumentNullException(nameof(loggerFactory));
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            _serviceProvider = serviceProvider;

            _connectionService.ConnectionRestored += this.ConnectionRestored;
            _connectionService.ConnectionLost += this.ConnectionLost;
        }

        /// <summary>
        /// Starts the web server.
        /// </summary>
        public void Start()
        {
            if (!_isServerRunning)
            {
                this.InitializeWebServer();
                _server.Start();
                _isServerRunning = true;
                _logger.LogInformation("Web server started.");
            }
        }

        /// <summary>
        /// Stops the web server.
        /// </summary>
        public void Stop()
        {
            if (_isServerRunning)
            {
                _server.Stop();
                _server.Dispose();
                _server = null;
                _isServerRunning = false;
                _logger.LogInformation("Web server stopped.");
            }
        }

        /// <summary>
        /// Restarts the web server.
        /// </summary>
        public void Restart()
        {
            this.Stop();
            this.Start();
        }

        private void ConnectionLost(object sender, EventArgs e)
        {
            _logger.LogInformation("Connection lost. Stopping the web server.");
            this.Stop();
        }

        private void ConnectionRestored(object sender, EventArgs e)
        {
            _logger.LogInformation("Connection restored. Starting the web server.");
            this.Restart();
        }

        /// <summary>
        /// Initializes the web server.
        /// </summary>
        private void InitializeWebServer()
        {
            if (_server == null)
            {
                _server = new WebServerDi(80, HttpProtocol.Http, new Type[] { typeof(SensorController) }, _serviceProvider);
                _logger.LogInformation("Web server initialized.");
            }
        }
    }
}