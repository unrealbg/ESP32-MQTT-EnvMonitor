namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    using System;

    /// <summary>
    /// Interface for managing internet connection status and events.
    /// </summary>
    public interface IInternetConnectionService
    {
        /// <summary>
        /// Event triggered when the internet connection is lost.
        /// </summary>
        event EventHandler InternetLost;

        /// <summary>
        /// Event triggered when the internet connection is restored.
        /// </summary>
        event EventHandler InternetRestored;

        /// <summary>
        /// Checks if the internet connection is available.
        /// </summary>
        /// <returns><c>true</c> if the internet connection is available; otherwise, <c>false</c>.</returns>
        bool IsInternetAvailable();
    }
}
