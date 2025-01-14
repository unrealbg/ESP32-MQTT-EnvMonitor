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
    /// Service for managing network connections, including connecting to Wi-Fi and checking connection status.
    /// </summary>
    public class ConnectionService : IConnectionService
    {
        private bool _isInitialStart = true;
        private bool _isConnectionInProgress = false;

        private string _ipAddress;

        /// <summary>
        /// Gets a value indicating whether the connection is in progress.
        /// </summary>
        public bool IsConnectionInProgress
        {
            get => _isConnectionInProgress;
            private set => _isConnectionInProgress = value;
        }

        /// <summary>
        /// Event triggered when the connection is restored.
        /// </summary>
        public event EventHandler ConnectionRestored;

        /// <summary>
        /// Event triggered when the connection is lost.
        /// </summary>
        public event EventHandler ConnectionLost;

        /// <summary>
        /// Initiates a connection to the network.
        /// </summary>
        public void Connect()
        {
            var wifiAdapter = WifiAdapter.FindAllAdapters()[0];
            var count = 0;
            var maxAttempts = 10;

            while (!this.IsAlreadyConnected(out var ipAddress))
            {
                LogHelper.LogInformation($"Connecting... [Attempt {++count}]");
                var result = wifiAdapter.Connect(SSID, WifiReconnectionKind.Automatic, Password);

                for (int waitTime = 0; waitTime < maxAttempts; waitTime++)
                {
                    if (result.ConnectionStatus == WifiConnectionStatus.Success && this.IsAlreadyConnected(out ipAddress))
                    {
                        if (_isInitialStart)
                        {
                            _ipAddress = ipAddress;
                            Thread.Sleep(200);
                            LogHelper.LogInformation($"Connection established. IP address: {ipAddress}");
                            _isInitialStart = false;
                            _isConnectionInProgress = false;

                            return;
                        }

                        Thread.Sleep(200);
                        LogHelper.LogInformation("Connection restored.");
                        this.RaiseConnectionRestored();
                        _isConnectionInProgress = false;

                        return;
                    }

                    Thread.Sleep(200);
                }

                var msg = this.GetErrorMessage(result.ConnectionStatus);
                LogHelper.LogWarning($"{msg}. Retrying in 10 seconds...");
                Thread.Sleep(1000);

                if (this.IsAlreadyConnected(out ipAddress))
                {
                    _ipAddress = ipAddress;
                    LogHelper.LogInformation($"Connection restored. IP Address: {ipAddress}");
                    this.RaiseConnectionRestored();
                    _isConnectionInProgress = false;
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
                _isConnectionInProgress = true;
                this.RaiseConnectionLost();
                LogHelper.LogWarning("Lost network connection. Attempting to reconnect...");
                this.Connect();
            }
            else
            {
                _isConnectionInProgress = false;
            }
        }

        /// <summary>
        /// Gets the IP address of the device.
        /// </summary>
        /// <returns>The IP address of the device.</returns>
        public string GetIpAddress()
        {
            return string.IsNullOrEmpty(_ipAddress) || _ipAddress == "0.0.0.0" ? "IP address not available" : _ipAddress;
        }

        /// <summary>
        /// Checks if the device is already connected to the network.
        /// </summary>
        /// <param name="ipAddress">The IP address of the device if connected.</param>
        /// <returns><c>true</c> if the device is connected; otherwise, <c>false</c>.</returns>
        private bool IsAlreadyConnected(out string ipAddress)
        {
            try
            {
                var networkInterface = NetworkInterface.GetAllNetworkInterfaces()[0];
                ipAddress = networkInterface.IPv4Address;
                return !(string.IsNullOrEmpty(ipAddress) || ipAddress == "0.0.0.0");
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"ERROR: {ex.Message}");
            }

            ipAddress = null;
            return false;
        }

        /// <summary>
        /// Gets the error message corresponding to the Wi-Fi connection status.
        /// </summary>
        /// <param name="status">The Wi-Fi connection status.</param>
        /// <returns>The error message.</returns>
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

        private void RaiseConnectionRestored()
        {
            this.ConnectionRestored?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseConnectionLost()
        {
            this.ConnectionLost?.Invoke(this, EventArgs.Empty);
        }
    }
}
