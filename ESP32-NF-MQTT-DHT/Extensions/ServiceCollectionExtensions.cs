namespace ESP32_NF_MQTT_DHT.Extensions
{
    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Managers;
    using ESP32_NF_MQTT_DHT.Managers.Contracts;
    using ESP32_NF_MQTT_DHT.Services;
    using ESP32_NF_MQTT_DHT.Services.Contracts;
    using ESP32_NF_MQTT_DHT.Services.MQTT;
    using ESP32_NF_MQTT_DHT.Services.MQTT.Contracts;

    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Extension methods for the <see cref="IServiceCollection"/> interface.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSensorServices(this IServiceCollection services, SensorType sensorType)
        {
            services.AddSingleton(typeof(ISensorService), SensorServiceFactory.GetSensorServiceType(sensorType));
            return services;
        }

        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            services.AddSingleton(typeof(IConnectionService), typeof(ConnectionService));
            services.AddSingleton(typeof(IMqttClientService), typeof(MqttClientService));
            services.AddSingleton(typeof(IRelayService), typeof(RelayService));
            services.AddSingleton(typeof(IUptimeService), typeof(UptimeService));
            services.AddSingleton(typeof(IWebServerService), typeof(WebServerService));
            services.AddSingleton(typeof(IInternetConnectionService), typeof(InternetConnectionService));
            services.AddSingleton(typeof(MqttMessageHandler));
            services.AddSingleton(typeof(ISensorManager), typeof(SensorManager));
            services.AddSingleton(typeof(IMqttConnectionManager), typeof(MqttConnectionManager));
            services.AddSingleton(typeof(IMqttPublishService), typeof(MqttPublishService));
            services.AddSingleton(typeof(ITcpListenerService), typeof(TcpListenerService));

            return services;
        }
    }
}
