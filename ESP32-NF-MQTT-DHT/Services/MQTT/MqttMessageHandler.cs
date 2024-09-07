namespace ESP32_NF_MQTT_DHT.Services.MQTT
{
    using System;
    using System.Text;
    using System.Threading;

    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Services.Contracts;

    using Microsoft.Extensions.Logging;

    using nanoFramework.M2Mqtt;
    using nanoFramework.M2Mqtt.Messages;
    using nanoFramework.Runtime.Native;

    using static ESP32_NF_MQTT_DHT.Settings.DeviceSettings;

    public class MqttMessageHandler
    {
        private static readonly string RelayTopic = $"home/{DeviceName}/switch";
        private static readonly string SystemTopic = $"home/{DeviceName}/system";
        private static readonly string UptimeTopic = $"home/{DeviceName}/uptime";
        private static readonly string ErrorTopic = $"home/{DeviceName}/errors";

        private readonly IRelayService _relayService;
        private readonly IUptimeService _uptimeService;
        private readonly LogHelper _logHelper;
        private MqttClient _mqttClient;

        public MqttMessageHandler( IRelayService relayService, IUptimeService uptimeService, LogHelper logHelper)
        {
            this._relayService = relayService;
            this._uptimeService = uptimeService;
            this._logHelper = logHelper;
        }


        public void SetMqttClient(MqttClient mqttClient)
        {
            _mqttClient = mqttClient;
        }

        public void HandleIncomingMessage(object sender, MqttMsgPublishEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);

            if (e.Topic == RelayTopic)
            {
                if (message.Contains("on"))
                {
                    this._relayService.TurnOn();
                    this._mqttClient.Publish(RelayTopic + "/relay", Encoding.UTF8.GetBytes("ON"));
                    this._logHelper.LogWithTimestamp(LogLevel.Information, "Relay turned ON and message published");
                }
                else if (message.Contains("off"))
                {
                    this._relayService.TurnOff();
                    this._mqttClient.Publish(RelayTopic + "/relay", Encoding.UTF8.GetBytes("OFF"));
                    this._logHelper.LogWithTimestamp(LogLevel.Information, "Relay turned OFF and message published");
                }
            }
            else if (e.Topic == SystemTopic)
            {
                if (message.Contains("uptime"))
                {
                    string uptime = this._uptimeService.GetUptime();
                    this._mqttClient.Publish(UptimeTopic, Encoding.UTF8.GetBytes(uptime));
                    this._logHelper.LogWithTimestamp(LogLevel.Information, $"Uptime requested, published: {uptime}");
                }
                else if (message.Contains("reboot"))
                {
                    this._mqttClient.Publish($"home/{DeviceName}/maintenance", Encoding.UTF8.GetBytes($"Manual reboot at: {DateTime.UtcNow.ToString("HH:mm:ss")}"));
                    this._logHelper.LogWithTimestamp(LogLevel.Warning, "Rebooting system...");
                    Thread.Sleep(2000);
                    Power.RebootDevice();
                }
            }
            else if (e.Topic == ErrorTopic)
            {
                // Log the error message
            }
        }
    }
}
