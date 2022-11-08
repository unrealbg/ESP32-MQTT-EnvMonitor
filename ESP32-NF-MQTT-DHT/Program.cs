namespace ESP32_NF_MQTT_DHT
{
    using Microsoft.Extensions.Logging;

    using nanoFramework.DependencyInjection;
    using nanoFramework.Logging.Debug;

    using Services;
    using Services.Contracts;

    public class Program
    {
        public static void Main()
        {
            var services = ConfigureServices();
            var application = (Startup)services.GetRequiredService(typeof(Startup));
            application.Run();
        }

        private static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(typeof(Startup))
            .AddSingleton(typeof(IConnectionService), typeof(ConnectionService))
                .AddSingleton(typeof(IMqttClient), typeof(MqttClientService))
                .AddSingleton(typeof(IDhtService), typeof(DhtService))
                .AddSingleton(typeof(IUptimeService), typeof(UptimeService))
                .AddSingleton(typeof(ILoggerFactory), typeof(DebugLoggerFactory))
                .BuildServiceProvider();
        }
    }
}
