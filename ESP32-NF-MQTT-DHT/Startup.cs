namespace ESP32_NF_MQTT_DHT
{
    using System;

    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Managers;
    using ESP32_NF_MQTT_DHT.Managers.Contracts;

    using nanoFramework.Runtime.Native;

    using Services.Contracts;

    /// <summary>
    /// Represents the startup process of the application.
    /// </summary>
    public class Startup
    {
        private const int RequiredMemory = 100000;

        private readonly IConnectionService _connectionService;
        private readonly IMqttClientService _mqttClient;
        private readonly IWebServerService _webServerService;
        private readonly ISensorManager _sensorManager;

        private readonly LogHelper _logHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="connectionService">Service for managing connections.</param>
        /// <param name="mqttClient">MQTT client service.</param>
        /// <param name="sensorManager">Sensor manager.</param>
        /// <param name="webServerService">WebServer service.</param>
        public Startup(
            IConnectionService connectionService,
            IMqttClientService mqttClient,
            IWebServerService webServerService,
            ISensorManager sensorManager)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            _webServerService = webServerService ?? throw new ArgumentNullException(nameof(webServerService));
            _sensorManager = sensorManager ?? throw new ArgumentNullException(nameof(sensorManager));

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

                _logHelper.LogWithTimestamp("Starting sensor manager...");
                _sensorManager.StartSensor();
                _logHelper.LogWithTimestamp("Sensor manager started.");

                _logHelper.LogWithTimestamp("Starting MQTT client...");
                _mqttClient.Start();
                _logHelper.LogWithTimestamp("MQTT client started.");

                if (SystemInfo.TargetName == "ESP32_S3" && nanoFramework.Runtime.Native.GC.Run(false) >= RequiredMemory)
                {
                    _logHelper.LogWithTimestamp("Starting WebServer service...");
                    _webServerService.Start();
                    _logHelper.LogWithTimestamp("WebServer service started.");
                }
                else
                {
                    _logHelper.LogWithTimestamp($"WebServer service will not be started due to insufficient memory or unsupported platform ({SystemInfo.TargetName}).");
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogWithTimestamp($"An error occurred during startup: {ex.Message}");
            }
        }
    }
}
