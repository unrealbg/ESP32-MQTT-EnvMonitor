namespace ESP32_NF_MQTT_DHT.Services.MQTT.Contracts
{
    using nanoFramework.M2Mqtt;

    internal interface IMqttPublishService
    {
       public void PublishSensorData();

       public void PublishError(string errorMessage);

       public void SetMqttClient(MqttClient mqttClient);
    }
}
