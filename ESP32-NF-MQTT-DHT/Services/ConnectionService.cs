namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Device.Wifi;
    using System.Net.NetworkInformation;
    using System.Threading;

    using Contracts;

    using Helpers;

    using static Settings.WifiSettings;

    /// <summary>
    /// Service responsible for managing the network connection of the device.
    /// </summary>
    public class ConnectionService : IConnectionService
    {
        private readonly LogHelper _logHelper;
        private bool _isInitialStart = true;

        private string _ipAddress;

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
        public ConnectionService()
        {
            _logHelper = new LogHelper();
        }

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
                this._logHelper.LogWithTimestamp($"Connecting... [Attempt {++count}]");
                var result = wifiAdapter.Connect(SSID, WifiReconnectionKind.Automatic, Password);

                for (int waitTime = 0; waitTime < maxAttempts; waitTime++)
                {
                    if (result.ConnectionStatus == WifiConnectionStatus.Success && IsAlreadyConnected(out ipAddress))
                    {
                        if (_isInitialStart)
                        {
                            _ipAddress = ipAddress;
                            Thread.Sleep(200);
                            _logHelper.LogWithTimestamp($"Connection established. IP address: {ipAddress}");
                            _isInitialStart = false;
                            this.ConnectionRestored?.Invoke(this, EventArgs.Empty);

                            return;
                        }

                        Thread.Sleep(200);
                        this._logHelper.LogWithTimestamp("Connection restored.");
                        this.ConnectionRestored?.Invoke(this, EventArgs.Empty);
                        return;
                    }

                    Thread.Sleep(200);
                }

                this._logHelper.LogWithTimestamp("Connection failed. Retrying in 10 seconds...");
                Thread.Sleep(10000);

                if (this.IsAlreadyConnected(out ipAddress))
                {
                    _ipAddress = ipAddress;
                    _logHelper.LogWithTimestamp($"Connection restored. IP Address: {ipAddress}");
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
                this._logHelper.LogWithTimestamp("Lost network connection. Attempting to reconnect...");
                this.Connect();
            }
        }

        public string GetIpAddress()
        {
            return string.IsNullOrEmpty(_ipAddress) || _ipAddress == "0.0.0.0" ? "IP address not available" : _ipAddress;
        }


        private bool IsAlreadyConnected(out string ipAddress)
        {
            var networkInterface = NetworkInterface.GetAllNetworkInterfaces()[0];
            ipAddress = networkInterface.IPv4Address;
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
