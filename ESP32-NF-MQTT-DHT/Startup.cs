namespace ESP32_NF_MQTT_DHT
{
    using System;

    using ESP32_NF_MQTT_DHT.Helpers;
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
        private readonly ITcpListenerService _tcpListenerService;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="connectionService">Service for managing connections.</param>
        /// <param name="mqttClient">MQTT client service.</param>
        /// <param name="sensorManager">Sensor manager.</param>
        /// <param name="webServerService">WebServer service.</param>
        /// <param name="tcpListenerService">TCPListener service.</param>"
        public Startup(
            IConnectionService connectionService,
            IMqttClientService mqttClient,
            IWebServerService webServerService,
            ISensorManager sensorManager,
            ITcpListenerService tcpListenerService)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            _webServerService = webServerService ?? throw new ArgumentNullException(nameof(webServerService));
            _sensorManager = sensorManager ?? throw new ArgumentNullException(nameof(sensorManager));
            _tcpListenerService = tcpListenerService ?? throw new ArgumentNullException(nameof(tcpListenerService));

            LogHelper.LogInformation("Initializing application...");
        }

        /// <summary>
        /// Runs the application services.
        /// </summary>
        public void Run()
        {
            try
            {
                LogHelper.LogInformation("Establishing connection...");
                _connectionService.Connect();

                LogHelper.LogInformation("Starting sensor manager...");
                _sensorManager.StartSensor();
                LogHelper.LogInformation("Sensor manager started.");

                LogHelper.LogInformation("Starting MQTT client...");
                _mqttClient.Start();
                LogHelper.LogInformation("MQTT client started.");

                LogHelper.LogInformation("Starting TCPListener service...");
                _tcpListenerService.Start();
                LogHelper.LogInformation("TCPListener service started.");

                // Tested only on ESP32-S3
                if (SystemInfo.TargetName == "ESP32_S3")
                {
                    LogHelper.LogInformation("Starting WebServer service...");
                    _webServerService.Start();
                    LogHelper.LogInformation("WebServer service started.");
                }
                else
                {
                    LogHelper.LogWarning($"WebServer service will not be started due to insufficient memory or unsupported platform ({SystemInfo.TargetName}).");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogInformation($"An error occurred during startup: {ex.Message}");
            }
        }
    }
}
