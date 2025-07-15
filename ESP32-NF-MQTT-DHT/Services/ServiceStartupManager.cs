namespace ESP32_NF_MQTT_DHT.Services
{
    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Managers.Contracts;
    using ESP32_NF_MQTT_DHT.Services.Contracts;

    /// <summary>
    /// Manages the startup sequence of application services.
    /// </summary>
    public class ServiceStartupManager : IServiceStartupManager
    {
        private readonly ISensorManager _sensorManager;
        private readonly IMqttClientService _mqttClient;
        private readonly ITcpListenerService _tcpListenerService;
        private readonly IWebServerService _webServerService;
        private readonly IPlatformService _platformService;

        public ServiceStartupManager(
            ISensorManager sensorManager,
            IMqttClientService mqttClient,
            ITcpListenerService tcpListenerService,
            IWebServerService webServerService,
            IPlatformService platformService)
        {
            _sensorManager = sensorManager;
            _mqttClient = mqttClient;
            _tcpListenerService = tcpListenerService;
            _webServerService = webServerService;
            _platformService = platformService;
        }

        /// <summary>
        /// Starts all core services in the correct order.
        /// </summary>
        public void StartAllServices()
        {
            this.StartService("SensorManager");
            this.StartService("MqttClient");
            this.StartService("TcpListener");
            this.StartService("WebServer");
        }

        /// <summary>
        /// Starts a specific service by name.
        /// </summary>
        /// <param name="serviceName">The name of the service to start.</param>
        public void StartService(string serviceName)
        {
            switch (serviceName)
            {
                case "SensorManager":
                    this.StartSensorManager();
                    break;
                case "MqttClient":
                    this.StartMqttClient();
                    break;
                case "TcpListener":
                    this.StartTcpListener();
                    break;
                case "WebServer":
                    this.StartWebServerIfSupported();
                    break;
                default:
                    LogHelper.LogWarning($"Unknown service: {serviceName}");
                    break;
            }
        }

        /// <summary>
        /// Stops all running services.
        /// </summary>
        public void StopAllServices()
        {
            LogHelper.LogInformation("Stopping all services...");
            _sensorManager.StopSensor();
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

        private void StartWebServerIfSupported()
        {
            if (_platformService.SupportsWebServer())
            {
                LogHelper.LogInformation("Starting WebServer service...");
                _webServerService.Start();
                LogHelper.LogInformation("WebServer service started.");
            }
            else
            {
                LogHelper.LogWarning($"WebServer service not started. Platform: {_platformService.PlatformName}, Supports WebServer: {_platformService.SupportsWebServer()}");
            }
        }
    }
}