namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Device.Wifi;
    using System.Net.NetworkInformation;
    using System.Threading;

    using Contracts;

    using Helpers;

    using Microsoft.Extensions.Logging;

    using static Settings.WifiSettings;

    /// <summary>
    /// Service responsible for managing the network connection of the device.
    /// </summary>
    public class ConnectionService : IConnectionService
    {
        private readonly LogHelper _logHelper;
        private bool _isInitialStart = true;

        /// <summary>
        /// Event that is triggered when the connection is restored.
        /// </summary>
        public event EventHandler ConnectionRestored;

        /// <summary>
        /// Event that is triggered when the connection is lost.
        /// </summary>
        public event EventHandler ConnectionLost;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionService"/> class.
        /// </summary>
        /// <param name="loggerFactory">Factory to create a logger for this service.</param>
        /// <exception cref="ArgumentNullException">Thrown if loggerFactory is null.</exception>
        public ConnectionService(ILoggerFactory loggerFactory)
        {
            _logHelper = new LogHelper(loggerFactory, nameof(ConnectionService));
        }

        /// <summary>
        /// Represents the current connection status.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Initiates a connection to the network.
        /// </summary>
        public void Connect()
        {
            var wifiAdapter = WifiAdapter.FindAllAdapters()[0];
            var count = 0;
            var maxAttempts = 10;

            while (!IsAlreadyConnected(out var ipAddress))
            {
                this._logHelper.LogWithTimestamp(LogLevel.Information, $"Connecting... [Attempt {++count}]");
                var result = wifiAdapter.Connect(SSID, WifiReconnectionKind.Automatic, Password);

                for (int waitTime = 0; waitTime < maxAttempts; waitTime++)
                {
                    if (result.ConnectionStatus == WifiConnectionStatus.Success && IsAlreadyConnected(out ipAddress))
                    {
                        if (_isInitialStart)
                        {
                            Thread.Sleep(200);
                            _logHelper.LogWithTimestamp(LogLevel.Information, $"Connection established. IP address: {ipAddress}");
                            this.IsConnected = true;
                            _isInitialStart = false;
                            return;
                        }

                        Thread.Sleep(200);
                        this._logHelper.LogWithTimestamp(LogLevel.Information, "Connection restored.");
                        ConnectionRestored?.Invoke(this, EventArgs.Empty);
                        this.IsConnected = true;
                        return;
                    }

                    Thread.Sleep(200);
                }

                this._logHelper.LogWithTimestamp(LogLevel.Warning, "Connection failed. Retrying in 10 seconds...");
                Thread.Sleep(10000);

                if (this.IsAlreadyConnected(out ipAddress))
                {
                    _logHelper.LogWithTimestamp(LogLevel.Information, $"Connection restored. IP Address: {ipAddress}");
                    this.ConnectionRestored?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Checks the network connection and attempts to reconnect if it is lost.
        /// </summary>
        public void CheckConnection()
        {
            if (!this.IsAlreadyConnected(out var ipAddress))
            {
                this.ConnectionLost?.Invoke(this, EventArgs.Empty);
                this._logHelper.LogWithTimestamp(LogLevel.Warning, "Lost network connection. Attempting to reconnect...");
                this.Connect();
            }
        }

        private bool IsAlreadyConnected(out string ipAddress)
        {
            ipAddress = NetworkInterface.GetAllNetworkInterfaces()[0].IPv4Address;
            this.IsConnected = true;
            return !(string.IsNullOrEmpty(ipAddress) || ipAddress == "0.0.0.0");
        }

        private string GetErrorMessage(WifiConnectionStatus status)
        {
            switch (status)
            {
                case WifiConnectionStatus.AccessRevoked: return "Access to the network has been revoked.";
                case WifiConnectionStatus.InvalidCredential: return "Invalid credential was presented.";
                case WifiConnectionStatus.NetworkNotAvailable: return "Network is not available.";
                case WifiConnectionStatus.Timeout: return "Connection attempt timed out.";
                case WifiConnectionStatus.UnspecifiedFailure: return "Unspecified error [connection refused]";
                case WifiConnectionStatus.UnsupportedAuthenticationProtocol: return "Authentication protocol is not supported.";
                default: return "Unknown error.";
            }
        }
    }
}
