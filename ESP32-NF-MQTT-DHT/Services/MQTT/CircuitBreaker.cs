namespace ESP32_NF_MQTT_DHT.Services.MQTT
{
    using System;

    internal class CircuitBreaker
    {
        private bool _isOpen;
        private DateTime _resetTime;

        public bool IsOpen => _isOpen && DateTime.UtcNow < _resetTime;

        public void Open(TimeSpan duration)
        {
            _isOpen = true;
            _resetTime = DateTime.UtcNow.Add(duration);
        }

        public void Close()
        {
            _isOpen = false;
        }
    }
}
