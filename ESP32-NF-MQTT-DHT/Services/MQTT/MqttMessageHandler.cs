namespace ESP32_NF_MQTT_DHT.Services.MQTT
{
    using System;
    using System.Text;
    using System.Threading;

    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Services.Contracts;

    using nanoFramework.M2Mqtt;
    using nanoFramework.M2Mqtt.Messages;
    using nanoFramework.Runtime.Native;

    using static ESP32_NF_MQTT_DHT.Settings.DeviceSettings;

    /// <summary>
    /// Handles incoming MQTT messages and performs actions based on the message content and topic.
    /// </summary>
    public class MqttMessageHandler
    {
        private static readonly string RelayTopic = $"home/{DeviceName}/switch";
        private static readonly string SystemTopic = $"home/{DeviceName}/system";
        private static readonly string UptimeTopic = $"home/{DeviceName}/uptime";
        private static readonly string ErrorTopic = $"home/{DeviceName}/errors";

        private readonly IRelayService _relayService;
        private readonly IUptimeService _uptimeService;
        private readonly IConnectionService _connectionService;
        private readonly LogHelper _logHelper;
        private MqttClient _mqttClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttMessageHandler"/> class.
        /// </summary>
        /// <param name="relayService">The relay service for controlling relays.</param>
        /// <param name="uptimeService">The uptime service for retrieving system uptime.</param>
        /// <param name="logHelper">The log helper for logging messages.</param>
        /// <param name="connectionService">The connection service for retrieving network information.</param>
        public MqttMessageHandler(IRelayService relayService, IUptimeService uptimeService, LogHelper logHelper, IConnectionService connectionService)
        {
            this._relayService = relayService;
            this._uptimeService = uptimeService;
            this._logHelper = logHelper;
            this._connectionService = connectionService;
        }

        /// <summary>
        /// Sets the MQTT client to be used for handling messages.
        /// </summary>
        /// <param name="mqttClient">The MQTT client instance.</param>
        public void SetMqttClient(MqttClient mqttClient)
        {
            _mqttClient = mqttClient;
        }

        /// <summary>
        /// Handles incoming MQTT messages and performs actions based on the message content and topic.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MqttMsgPublishEventArgs"/> instance containing the event data.</param>
        public void HandleIncomingMessage(object sender, MqttMsgPublishEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);

            if (e.Topic == RelayTopic)
            {
                if (message.Contains("on"))
                {
                    _relayService.TurnOn();
                    _mqttClient.Publish(RelayTopic + "/relay", Encoding.UTF8.GetBytes("ON"));
                    _logHelper.LogWithTimestamp("Relay turned ON and message published");
                }
                else if (message.Contains("off"))
                {
                    _relayService.TurnOff();
                    _mqttClient.Publish(RelayTopic + "/relay", Encoding.UTF8.GetBytes("OFF"));
                    _logHelper.LogWithTimestamp("Relay turned OFF and message published");
                }
                else if (message.Contains("status"))
                {
                    string status = _relayService.IsRelayOn() ? "ON" : "OFF";
                    _mqttClient.Publish(RelayTopic + "/status", Encoding.UTF8.GetBytes(status));
                    _logHelper.LogWithTimestamp($"Relay status requested, published: {status}");
                }
            }
            else if (e.Topic == SystemTopic)
            {
                if (message.Contains("uptime"))
                {
                    string uptime = this._uptimeService.GetUptime();
                    _mqttClient.Publish(UptimeTopic, Encoding.UTF8.GetBytes(uptime));
                    _logHelper.LogWithTimestamp($"Uptime requested, published: {uptime}");
                }
                else if (message.Contains("reboot"))
                {
                    _mqttClient.Publish($"home/{DeviceName}/maintenance", Encoding.UTF8.GetBytes($"Manual reboot at: {DateTime.UtcNow.ToString("HH:mm:ss")}"));
                    _logHelper.LogWithTimestamp("Rebooting system...");
                    Thread.Sleep(2000);
                    Power.RebootDevice();
                }
                else if (message.Contains("getip"))
                {
                    string ipAddress = this._connectionService.GetIpAddress();
                    _mqttClient.Publish(SystemTopic + "/ip", Encoding.UTF8.GetBytes(ipAddress));
                    _logHelper.LogWithTimestamp($"IP address requested, published: {ipAddress}");
                }
            }
            else if (e.Topic == ErrorTopic)
            {
                // Log the error message
            }
        }
    }
}
