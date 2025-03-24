namespace ESP32_NF_MQTT_DHT.Helpers
{
    public static class Constants
    {
        public const int INITIAL_RECONNECT_DELAY = 5000;
        public const int MAX_RECONNECT_DELAY = 120000;
        public const int SENSOR_DATA_INTERVAL = 300000;
        public const int INTERNET_CHECK_INTERVAL = 30000;
        public const int DEEP_SLEEP_MINUTES = 5;
        public const int MAX_TOTAL_ATTEMPTS = 1000;
        public const int JITTER_BASE = 500;
        public const int JITTER_RANGE = 1500;

        public const int MaxReconnectAttempts = 20;
        public const int ReconnectDelay = 10000;
        public const int SensorDataInterval = 300000;
        public const double InvalidTemperature = -50;
        public const double InvalidHumidity = -100;
        public const double InvalidPressure = -1;

        public const int ReadInterval = 60000;
        public const int ErrorInterval = 30000;
    }
}
