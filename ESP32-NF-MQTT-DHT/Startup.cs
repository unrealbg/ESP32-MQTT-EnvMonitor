namespace ESP32_NF_MQTT_DHT
{
    using Microsoft.Extensions.Logging;

    using Services.Contracts;

    using System;

    /// <summary>
    /// Represents the startup process of the application.
    /// </summary>
    public class Startup
    {
        private readonly IConnectionService _connectionService;
        private readonly IMqttClient _mqttClient;
        private readonly IDhtService _dhtService;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="connectionService">Service for managing connections.</param>
        /// <param name="mqttClient">MQTT client service.</param>
        /// <param name="dhtService">DHT sensor service.</param>
        /// <param name="loggerFactory">Factory for creating logger instances.</param>
        public Startup(
            IConnectionService connectionService,
            IMqttClient mqttClient,
            IDhtService dhtService,
            ILoggerFactory loggerFactory)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            _dhtService = dhtService ?? throw new ArgumentNullException(nameof(dhtService));
            _logger = loggerFactory?.CreateLogger(nameof(Startup)) ?? throw new ArgumentNullException(nameof(loggerFactory));

            _logger.LogInformation("Startup: Initializing application...");
        }

        /// <summary>
        /// Runs the application services.
        /// </summary>
        public void Run()
        {
            try
            {
                _logger.LogInformation("Startup: Establishing connection...");
                _connectionService.Connect();
                _logger.LogInformation("Startup: Connection established.");

                _logger.LogInformation("Startup: Starting MQTT client...");
                _mqttClient.Start();
                _logger.LogInformation("Startup: MQTT client started.");

                _logger.LogInformation("Startup: Starting DHT service...");
                _dhtService.Start();
                _logger.LogInformation("Startup: DHT service started.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred during startup: {ex.Message}");
                // Consider re-throwing the exception or handling it as needed for your application
            }
        }
    }
}
