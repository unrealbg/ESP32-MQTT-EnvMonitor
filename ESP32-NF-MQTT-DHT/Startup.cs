namespace ESP32_NF_MQTT_DHT
{
    using System;

    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Managers.Contracts;
    using ESP32_NF_MQTT_DHT.Services;

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

        public void Run()
        {
            try
            {
                this.EstablishConnection();
                this.StartServices();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"Startup failed: {ex.Message}");
                LogService.LogCritical($"Startup failed: {ex.Message}", ex);
            }
        }

        private void EstablishConnection()
        {
            LogHelper.LogInformation("Establishing connection...");
            _connectionService.Connect();
            LogHelper.LogInformation("Connection established.");
        }

        private void StartServices()
        {
            this.StartSensorManager();
            this.StartMqttClient();
            this.StartTcpListener();
            this.StartWebServerIfPossible();
        }

        private void StartSensorManager()
        {
            LogHelper.LogInformation("Starting sensor manager...");
            _sensorManager.StartSensor();
            LogHelper.LogInformation("Sensor manager started.");
        }

        private void StartMqttClient()
        {
            LogHelper.LogInformation("Starting MQTT client...");
            _mqttClient.Start();
            LogHelper.LogInformation("MQTT client started.");
        }

        private void StartTcpListener()
        {
            LogHelper.LogInformation("Starting TCPListener service...");
            _tcpListenerService.Start();
            LogHelper.LogInformation("TCPListener service started.");
        }

        private void StartWebServerIfPossible()
        {
            if (SystemInfo.TargetName == "ESP32_S3")
            {
                LogHelper.LogInformation("Starting WebServer service...");
                _webServerService.Start();
                LogHelper.LogInformation("WebServer service started.");
            }
            else
            {
                LogHelper.LogWarning($"WebServer service not started. Insufficient memory or unsupported platform ({SystemInfo.TargetName}).");
            }
        }
    }
}
