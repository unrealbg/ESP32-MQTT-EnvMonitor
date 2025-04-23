namespace ESP32_NF_MQTT_DHT.Services.MQTT
{
    internal class ReconnectStrategy
    {
        private readonly int _initialDelay;
        private readonly int _maxDelay;

        public ReconnectStrategy(int initialDelay, int maxDelay)
        {
            _initialDelay = initialDelay;
            _maxDelay = maxDelay;
        }

        public int GetNextDelay(int currentDelay)
        {
            return currentDelay < _maxDelay
                       ? System.Math.Min(currentDelay * 3 / 2, _maxDelay)
                       : _maxDelay;
        }
    }
}
