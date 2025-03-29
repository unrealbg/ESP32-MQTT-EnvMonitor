namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    using System;

    /// <summary>
    /// Defines a contract for a service that manages network connections.
    /// </summary>
    public interface IConnectionService
    {
        event EventHandler ConnectionRestored;

        event EventHandler ConnectionLost;

        /// <summary>
        /// Gets a value indicating whether a connection is in progress.
        /// </summary>
        bool IsConnectionInProgress { get; }

        /// <summary>
        /// Indicates whether a connection is currently established. Returns true if connected, otherwise false.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Initiates a connection to the network.
        /// </summary>
        void Connect();

        /// <summary>
        /// Checks the network connection and attempts to reconnect if it is lost.
        /// </summary>
        void CheckConnection();

        /// <summary>
        /// Gets the IP address of the device.
        /// </summary>
        string GetIpAddress();
    }
}
