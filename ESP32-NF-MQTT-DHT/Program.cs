namespace ESP32_NF_MQTT_DHT
{
    using Microsoft.Extensions.Logging;

    using nanoFramework.DependencyInjection;
    using nanoFramework.Logging.Debug;

    using Services;
    using Services.Contracts;

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
            var application = services.GetRequiredService<Startup>();
            application.Run();
        }

        /// <summary>
        /// Configures services for the application.
        /// </summary>
        /// <returns>Configured service provider.</returns>
        private static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<Startup>()
                .AddSingleton<IConnectionService, ConnectionService>()
                .AddSingleton<IMqttClient, MqttClientService>()
                .AddSingleton<IDhtService, DhtService>()
                .AddSingleton<IUptimeService, UptimeService>()
                .AddSingleton<ILoggerFactory, DebugLoggerFactory>()
                .BuildServiceProvider();
        }
    }
}
