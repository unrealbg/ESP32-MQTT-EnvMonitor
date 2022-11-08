namespace ESP32_NF_MQTT_DHT
{
    using Microsoft.Extensions.Logging;

    using Services.Contracts;

    public class Startup
    {
        private readonly IConnectionService _connectionService;
        private readonly IMqttClient _mqttClient;
        private readonly IDhtService _dhtService;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        public Startup(
            IConnectionService connectionService,
            IMqttClient mqttClient,
            IDhtService dhtService,
            ILoggerFactory loggerFactory)
        {
            this._connectionService = connectionService;
            this._mqttClient = mqttClient;
            this._dhtService = dhtService;
            this._logger = loggerFactory.CreateLogger(nameof(Startup));

            this._logger.LogInformation("Initializing application...");
        }

        public void Run()
        {
            this._connectionService.Connect();
            this._mqttClient.Start();
            this._dhtService.Start();
        }
    }
}
