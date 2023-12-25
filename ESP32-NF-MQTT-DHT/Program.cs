
namespace ESP32_NF_MQTT_DHT
{

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using nanoFramework.Logging.Debug;

    using Services;
    using Services.Contracts;
    using Controllers;

    using System;

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
            services.AddSingleton(typeof(IConnectionService), typeof(ConnectionService));
            services.AddSingleton(typeof(IMqttClientService), typeof(MqttClientService));
            services.AddSingleton(typeof(IDhtService), typeof(DhtService));
            services.AddSingleton(typeof(IUptimeService), typeof(UptimeService));
            services.AddSingleton(typeof(ILoggerFactory), typeof(DebugLoggerFactory));
            services.AddSingleton(typeof(IWebServerService), new WebServerService(80, new Type[] { typeof(SensorController) }));

            var serviceProvider = services.BuildServiceProvider();

            // Set the global DhtService instance
            GlobalServices.DhtService = serviceProvider.GetService(typeof(IDhtService)) as IDhtService;

            return serviceProvider;
        }
    }

    public static class GlobalServices
    {
        public static IDhtService DhtService { get; set; }
    }
}
