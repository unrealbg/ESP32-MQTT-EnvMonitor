namespace ESP32_NF_MQTT_DHT
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

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
            var application = (Startup)services.GetService(typeof(Startup));

            application.Run();
        }

        /// <summary>
        /// Configures services for the application.
        /// </summary>
        /// <returns>Configured service provider.</returns>
        private static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(typeof(Startup))
                .AddSingleton(typeof(IConnectionService, typeof(ConnectionService))
                .AddSingleton(typeof(IMqttClient), typeof(MqttClientService))
                .AddSingleton(typeof(IDhtService), typeof(DhtService))
                .AddSingleton(typeof(IUptimeService), typeof(UptimeService))
                .AddSingleton(typeof(ILoggerFactory), typeof(DebugLoggerFactory))
                .BuildServiceProvider();
        }
    }
}
