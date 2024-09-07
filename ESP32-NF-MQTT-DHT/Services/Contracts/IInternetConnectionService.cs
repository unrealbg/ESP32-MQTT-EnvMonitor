namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    using System;

    public interface IInternetConnectionService
    {
        public event EventHandler InternetLost;

        public event EventHandler InternetRestored;

        public bool IsInternetThreadRunning { get; }

        public bool IsInternetAvailable();
    }
}
