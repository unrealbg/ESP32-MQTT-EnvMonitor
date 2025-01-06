namespace ESP32_NF_MQTT_DHT.Services.MQTT
{
    using System;
    using System.Net.Sockets;

    using ESP32_NF_MQTT_DHT.Helpers;

    using nanoFramework.M2Mqtt;

    internal class MqttConnectionManager
    {
        private readonly LogHelper _logHelper = new LogHelper();

        /// <summary>
        /// Gets the MQTT client.
        /// </summary>
        public MqttClient MqttClient { get; private set; }

        /// <summary>
        /// Connects to the MQTT broker.
        /// </summary>
        public bool Connect(string broker, string clientId, string user, string pass)
        {
            try
            {
                this._logHelper.LogWithTimestamp($"Connecting to MQTT broker: {broker}...");
                this.MqttClient = new MqttClient(broker);
                this.MqttClient.Connect(clientId, user, pass);

                if (this.MqttClient.IsConnected)
                {
                    this._logHelper.LogWithTimestamp("MQTT client connected successfully!");
                    return true;
                }
            }
            catch (SocketException ex)
            {
                this._logHelper.LogWithTimestamp($"SocketException while connecting: {ex.Message}");
            }
            catch (Exception ex)
            {
                this._logHelper.LogWithTimestamp($"Error connecting to MQTT broker: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Disconnects the MQTT client.
        /// </summary>
        public void Disconnect()
        {
            if (this.MqttClient != null)
            {
                try
                {
                    if (this.MqttClient.IsConnected)
                    {
                        this.MqttClient.Disconnect();
                    }

                    this.MqttClient.Dispose();
                }
                catch (Exception ex)
                {
                    this._logHelper.LogWithTimestamp($"Error while disposing MQTT client: {ex.Message}");
                }
                finally
                {
                    this.MqttClient = null;
                }
            }
        }
    }
}
