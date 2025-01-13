namespace ESP32_NF_MQTT_DHT.Helpers
{
    using System;

    public static class LogMessages
    {
        public const string ReconnectingBroker = "Reconnecting to MQTT broker...";
        public const string NoInternet = "No internet connection. Retrying in 10 seconds...";
        public const string MaxReconnectAttempts = "Max reconnect attempts reached. Rebooting device...";
        public const string ErrorConnectingToBroker = "Error connecting to MQTT broker: {0}";
        public const string ConnectingToBroker = "Attempting to connect to MQTT broker: {0}";
        public const string ConnectionFailed = "Attempt {0} failed. Retrying in {1} seconds...";
        public const string LostConnectionToBroker = "Lost connection to MQTT broker, attempting to reconnect...";
        public const string StartingSensorThread = "Starting sensor data thread...";
        public const string UnableToPublishData = "Unable to publish sensor data. No connection.";
        public const string UnableToPublishError = "Unable to publish error message. No connection.";
        public const string InvalidSensorData = "Invalid sensor data.";
        public const string NoNetworkConnection = "No network connection. Retrying later.";
        public const string NetworkConnectionRestored = "Network connection restored.";
        public const string NetworkConnectionLost = "Network connection lost. Reconnecting...";
        public const string StartingWifiConnection = "Starting WiFi connection...";
        public const string WifiConnectionFailed = "WiFi connection failed. Retrying...";
        public const string WifiConnectionRestored = "WiFi connection restored.";
        public const string WifiConnectionLost = "WiFi connection lost. Reconnecting...";
        public const string StartingMqttConnection = "Starting MQTT connection...";
        public const string MqttConnectionFailed = "MQTT connection failed. Retrying...";
        public const string MqttConnectionRestored = "MQTT connection restored.";
        public const string MqttConnectionLost = "MQTT connection lost. Reconnecting...";
        public const string StartingSensorDataThread = "Starting sensor data thread...";
        public const string SensorDataThreadStarted = "Sensor data thread started.";
        public const string SensorDataThreadStopped = "Sensor data thread stopped.";

        public static readonly string TimeStamp = $"[{DateTime.UtcNow:dd-MM-yyyy HH:mm:ss}]";
    }
}
