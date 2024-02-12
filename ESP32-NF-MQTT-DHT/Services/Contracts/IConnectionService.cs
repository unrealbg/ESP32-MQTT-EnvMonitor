namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    using System;

    /// <summary>
    /// Defines a contract for a service that manages network connections.
    /// </summary>
    public interface IConnectionService
    {
        /// <summary>
        /// Initiates a connection to the network.
        /// </summary>
        void Connect();

        void CheckConnection();

        event EventHandler ConnectionRestored;

        event EventHandler ConnectionLost;
    }
}
