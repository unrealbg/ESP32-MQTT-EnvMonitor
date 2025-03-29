namespace ESP32_NF_MQTT_DHT
{
    using System;
    using System.Threading;

    using ESP32_NF_MQTT_DHT.Extensions;
    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Services;

    using Microsoft.Extensions.DependencyInjection;

    using GC = nanoFramework.Runtime.Native.GC;

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
#if DEBUG
            new Thread(MemoryMonitor).Start();
#endif

            try
            {
                // Set the sensor type to use.
                SensorType sensorType = SensorType.SHTC3;

                var services = ConfigureServices(sensorType);
                var application = services.GetService(typeof(Startup)) as Startup;

                application?.Run();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"An error occurred: {ex.Message}");
                LogService.LogCritical("Critical error in Main", ex);
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

        private static void MemoryMonitor()
        {
            while (true)
            {
                long totalMemory = GC.Run(true);
                LogHelper.LogInformation($"[MemoryMonitor] Total unused memory: {totalMemory} bytes");

                Thread.Sleep(60000);
            }
        }
    }
}
