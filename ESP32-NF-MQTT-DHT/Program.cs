namespace ESP32_NF_MQTT_DHT
{
    using System;

    using ESP32_NF_MQTT_DHT.Extensions;
    using ESP32_NF_MQTT_DHT.Helpers;

    using Microsoft.Extensions.DependencyInjection;

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
                // Set the sensor type to use.
                SensorType sensorType = SensorType.DHT;

                var services = ConfigureServices(sensorType);
                var application = services.GetService(typeof(Startup)) as Startup;

                application?.Run();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Configures services for the application.
        /// </summary>
        /// <returns>Configured service provider.</returns>
        private static ServiceProvider ConfigureServices(SensorType sensorType)
        {
            var services = new ServiceCollection();

            services.AddSingleton(typeof(Startup));
            services.AddCoreServices();
            services.AddSensorServices(sensorType);

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }
    }
}
