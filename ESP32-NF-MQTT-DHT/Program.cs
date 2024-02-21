namespace ESP32_NF_MQTT_DHT
{
    using System;
    using System.Diagnostics;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using nanoFramework.Logging.Debug;

    using Services;
    using Services.Contracts;

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
            try
            {
                var services = ConfigureServices();
                var application = services.GetService(typeof(Startup)) as Startup;

                application?.Run();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred: {ex.Message}");
            }
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

            return serviceProvider;
        }
    }
}
