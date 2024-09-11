namespace ESP32_NF_MQTT_DHT
{
    using System;

    using ESP32_NF_MQTT_DHT.Helpers;

    using Services.Contracts;

    /// <summary>
    /// Represents the startup process of the application.
    /// </summary>
    public class Startup
    {
        private readonly ISensorService _sensorService;
        private readonly IConnectionService _connectionService;
        private readonly IMqttClientService _mqttClient;
        private readonly IWebServerService _webServerService;

        private readonly LogHelper _logHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="connectionService">Service for managing connections.</param>
        /// <param name="mqttClient">MQTT client service.</param>
        /// <param name="sensorService">DHT sensor service.</param>
        /// <param name="webServerService">WebServer service.</param>
        public Startup(
            IConnectionService connectionService,
            IMqttClientService mqttClient,
            ISensorService sensorService,
            IWebServerService webServerService)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            _sensorService = sensorService ?? throw new ArgumentNullException(nameof(sensorService));
            _webServerService = webServerService ?? throw new ArgumentNullException(nameof(webServerService));

            _logHelper = new LogHelper();

            _logHelper.LogWithTimestamp("Initializing application...");
        }

        /// <summary>
        /// Runs the application services.
        /// </summary>
        public void Run()
        {
            try
            {
                _logHelper.LogWithTimestamp("Establishing connection...");
                _connectionService.Connect();
                _logHelper.LogWithTimestamp("Connection established.");

                _logHelper.LogWithTimestamp("Starting sensor service...");
                _sensorService.Start();
                _logHelper.LogWithTimestamp("Sensor service started.");

                _logHelper.LogWithTimestamp("Starting MQTT client...");
                _mqttClient.Start();
                _logHelper.LogWithTimestamp("MQTT client started.");

                _logHelper.LogWithTimestamp("Starting WebServer service...");
                _webServerService.Start();
                _logHelper.LogWithTimestamp("WebServer service started.");
            }
            catch (Exception ex)
            {
                _logHelper.LogWithTimestamp($"An error occurred during startup: {ex.Message}");
            }
        }
    }
}
