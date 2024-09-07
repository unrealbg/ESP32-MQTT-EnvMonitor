namespace ESP32_NF_MQTT_DHT
{
    using System;
    using System.Diagnostics;

    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Services.MQTT;
    using ESP32_NF_MQTT_DHT.Services.MQTT.Contracts;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using nanoFramework.Logging.Debug;
    using nanoFramework.M2Mqtt;

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
                SensorType sensorType = SensorType.SHTC3;

                var services = ConfigureServices(sensorType);
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
        private static ServiceProvider ConfigureServices(SensorType sensorType)
        {
            var services = new ServiceCollection();

            // Register individual services
            services.AddSingleton(typeof(Startup));

            var a = services.AddSingleton(typeof(IConnectionService), typeof(ConnectionService))[0];
            services.AddSingleton(typeof(IMqttClientService), typeof(MqttClientService));
            services.AddSingleton(typeof(IRelayService), typeof(RelayService));

            switch (sensorType)
            {
                case SensorType.DHT:
                    services.AddSingleton(typeof(ISensorService), typeof(DhtService));
                    break;

                case SensorType.AHT:
                    services.AddSingleton(typeof(ISensorService), typeof(AhtSensorService));
                    break;

                case SensorType.SHTC3:
                    services.AddSingleton(typeof(ISensorService), typeof(Shtc3SensorService));
                    break;

                default:
                    throw new ArgumentException("Unknown sensor type", nameof(sensorType));
            }

            services.AddSingleton(typeof(IUptimeService), typeof(UptimeService));
            services.AddSingleton(typeof(ILoggerFactory), typeof(DebugLoggerFactory));
            services.AddSingleton(typeof(IWebServerService), typeof(WebServerService));
            services.AddSingleton(typeof(IInternetConnectionService), typeof(InternetConnectionService));
            services.AddSingleton(typeof(MqttMessageHandler));
            services.AddSingleton(typeof(IMqttPublishService), typeof(MqttPublishService));

            services.AddTransient(typeof(LogHelper));

            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }
    }
}
