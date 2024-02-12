namespace ESP32_NF_MQTT_DHT
{
    using Services;
    using Services.Contracts;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using nanoFramework.Logging.Debug;

    /// <summary>
    /// Main program class.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        public static void Main()
        {
            var services = ConfigureServices();
            var application = (Startup)services.GetService(typeof(Startup));

            application.Run();
        }

        /// <summary>
        /// Configures services for the application.
        /// </summary>
        /// <returns>Configured service provider.</returns>
        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Register individual services
            services.AddSingleton(typeof(Startup));

            var a = services.AddSingleton(typeof(IConnectionService), typeof(ConnectionService))[0];
            services.AddSingleton(typeof(IMqttClientService), typeof(MqttClientService));
            services.AddSingleton(typeof(IRelayService), typeof(RelayService));

            services.AddSingleton(typeof(IDhtService), typeof(DhtService));
            services.AddSingleton(typeof(IAhtSensorService), typeof(AhtSensorService));

            services.AddSingleton(typeof(IUptimeService), typeof(UptimeService));
            services.AddSingleton(typeof(ILoggerFactory), typeof(DebugLoggerFactory));
            services.AddSingleton(typeof(IWebServerService), typeof(WebServerService));

            var serviceProvider = services.BuildServiceProvider();

            // Set the global DhtService instance
            GlobalServices.DhtService = serviceProvider.GetService(typeof(IDhtService)) as IDhtService;
            //GlobalServices.AhtSensorService = serviceProvider.GetService(typeof(IAhtSensorService)) as IAhtSensorService;

            return serviceProvider;
        }
    }

    /// <summary>
    /// Provides a global access point to shared services across the application.
    /// </summary>
    public static class GlobalServices
    {
        /// <summary>
        /// Gets or sets the globally available instance of the DHT sensor service.
        /// This service is responsible for interacting with the DHT sensor to read temperature and humidity data.
        /// </summary>
        public static IDhtService DhtService { get; set; }

        //public static IAhtSensorService AhtSensorService { get; set; }
    }
}
