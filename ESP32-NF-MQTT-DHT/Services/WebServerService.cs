namespace ESP32_NF_MQTT_DHT.Services
{
    using System;

    using Contracts;

    using Controllers;

    using Helpers;

    using nanoFramework.Runtime.Native;
    using nanoFramework.WebServer;

    /// <summary>
    /// Provides services for managing the web server.
    /// </summary>
    public class WebServerService : IWebServerService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnectionService _connectionService;
        private readonly LogHelper _logHelper;
        private WebServer _server;
        private bool _isServerRunning = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServerService"/> class.
        /// </summary>
        /// <param name="connectionService">The connection service.</param>
        /// <param name="serviceProvider">The service provider.</param>
        public WebServerService(IConnectionService connectionService, IServiceProvider serviceProvider)
        {
            _logHelper = new LogHelper();
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
                if (SystemInfo.TargetName == "ESP32_S3")
                {
                    this.InitializeWebServer();
                    _server.Start();
                    _isServerRunning = true;
                    _logHelper.LogWithTimestamp("Web server started.");
                }
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
                _logHelper.LogWithTimestamp("Web server stopped.");
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
            _logHelper.LogWithTimestamp("Connection lost. Stopping the web server.");
            this.Stop();
        }

        private void ConnectionRestored(object sender, EventArgs e)
        {
            if (!_isServerRunning)
            {
                _logHelper.LogWithTimestamp("Connection restored. Starting the web server.");
                this.Start();
            }
        }

        /// <summary>
        /// Initializes the web server.
        /// </summary>
        private void InitializeWebServer()
        {
            if (_server == null)
            {
                _server = new WebServerDi(80, HttpProtocol.Http, new Type[] { typeof(SensorController) }, _serviceProvider);
                _logHelper.LogWithTimestamp("Web server initialized.");
            }
        }
    }
}