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
        private const int MAX_CONNECTION_ATTEMPTS = 10;
        private const int RECONNECT_DELAY_MS = 10000;
        private const int CONNECTION_CHECK_INTERVAL_MS = 200;

        private readonly object _connectionLock = new object();
        private bool _hasConnectedSuccessfully = false;
        private bool _isConnectionInProgress = false;
        private string _ipAddress;
        private WifiAdapter _wifiAdapter;
        private Thread _connectionThread;
        private bool _connectionThreadActive = false;
        private readonly ManualResetEvent _stopConnectionRequest = new ManualResetEvent(false);

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionService"/> class.
        /// </summary>
        public ConnectionService()
        {
            InitializeWifiAdapter();
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
        /// Gets a value indicating whether the device is currently connected to the network.
        /// </summary>
        public bool IsConnected => IsAlreadyConnected(out _);

        /// <summary>
        /// Gets a value indicating whether the connection is in progress.
        /// </summary>
        public bool IsConnectionInProgress
        {
            get
            {
                lock (_connectionLock)
                {
                    return _isConnectionInProgress;
                }
            }
        }

        /// <summary>
        /// Initiates a connection to the network.
        /// </summary>
        public void Connect()
        {
            if (_wifiAdapter == null && !InitializeWifiAdapter())
            {
                return;
            }

            if (IsAlreadyConnected(out var currentIp))
            {
                LogHelper.LogInformation($"Already connected. IP: {currentIp}");
                return;
            }

            lock (_connectionLock)
            {
                if (_isConnectionInProgress && _connectionThreadActive)
                {
                    LogHelper.LogDebug("Connection attempt already in progress");
                    return;
                }

                _stopConnectionRequest.Set();
                Thread.Sleep(100);
                _stopConnectionRequest.Reset();

                _isConnectionInProgress = true;
                _connectionThreadActive = true;
                _connectionThread = new Thread(ConnectionThreadWorker);
                _connectionThread.Start();
                LogHelper.LogDebug("Started new connection thread");
            }
        }

        /// <summary>
        /// Checks the network connection and attempts to reconnect if it is lost.
        /// </summary>
        public void CheckConnection()
        {
            lock (_connectionLock)
            {
                if (_isConnectionInProgress)
                {
                    LogHelper.LogDebug("Connection attempt already in progress");
                    return;
                }
            }

            if (!IsAlreadyConnected(out _))
            {
                RaiseConnectionLost();
                LogHelper.LogWarning("Lost network connection. Attempting to reconnect...");
                Connect();
            }
        }

        /// <summary>
        /// Gets the IP address of the device.
        /// </summary>
        /// <returns>The IP address of the device.</returns>
        public string GetIpAddress()
        {
            if (string.IsNullOrEmpty(_ipAddress) || _ipAddress == "0.0.0.0")
            {
                if (IsAlreadyConnected(out string currentIp))
                {
                    _ipAddress = currentIp;
                }
                else
                {
                    return "IP address not available";
                }
            }

            return _ipAddress;
        }

        /// <summary>
        /// Stops any ongoing connection attempts.
        /// </summary>
        public void StopConnectionAttempts()
        {
            _stopConnectionRequest.Set();
            lock (_connectionLock)
            {
                _isConnectionInProgress = false;
            }
            LogHelper.LogDebug("Connection attempts stopped");
        }

        /// <summary>
        /// Thread worker method for handling Wi-Fi connection attempts.
        /// </summary>
        private void ConnectionThreadWorker()
        {
            try
            {
                int attemptCount = 0;
                bool connected = false;

                LogHelper.LogDebug("Connection thread started");

                while (!connected && !_stopConnectionRequest.WaitOne(0, false))
                {
                    Thread.Sleep(500);

                    if (IsAlreadyConnected(out string ipAddress))
                    {
                        HandleSuccessfulConnection(ipAddress);
                        connected = true;
                        break;
                    }

                    while (!connected && attemptCount < MAX_CONNECTION_ATTEMPTS && !_stopConnectionRequest.WaitOne(0, false))
                    {
                        attemptCount++;
                        LogHelper.LogInformation($"Connecting... [Attempt {attemptCount}/{MAX_CONNECTION_ATTEMPTS}]");

                        try
                        {
                            var result = _wifiAdapter.Connect(SSID, WifiReconnectionKind.Automatic, Password);

                            if (TryWaitForConnection(result, out string ipAddressTry))
                            {
                                HandleSuccessfulConnection(ipAddressTry);
                                connected = true;
                                break;
                            }

                            LogHelper.LogWarning($"{GetErrorMessage(result.ConnectionStatus)}. Retrying in {RECONNECT_DELAY_MS / 1000} seconds...");
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogError($"Connection error: {ex.Message}");
                        }

                        for (int i = 0; i < RECONNECT_DELAY_MS / 500 && !_stopConnectionRequest.WaitOne(0, false); i++)
                        {
                            Thread.Sleep(500);
                            if (IsAlreadyConnected(out string ipWhileWaiting))
                            {
                                HandleSuccessfulConnection(ipWhileWaiting);
                                connected = true;
                                break;
                            }
                        }
                    }

                    if (!connected)
                    {
                        LogHelper.LogError("Failed to connect after maximum attempts. Waiting passively for WiFi...");

                        for (int i = 0; i < 30 && !_stopConnectionRequest.WaitOne(0, false); i++)
                        {
                            Thread.Sleep(2000);
                            if (IsAlreadyConnected(out string ipDuringWait))
                            {
                                HandleSuccessfulConnection(ipDuringWait);
                                connected = true;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"Error in connection thread: {ex.Message}");
            }
            finally
            {
                lock (_connectionLock)
                {
                    _connectionThreadActive = false;
                    _isConnectionInProgress = false;
                }

                LogHelper.LogDebug("Connection thread completed");
            }
        }

        /// <summary>
        /// Checks if the device is already connected to the network.
        /// </summary>
        /// <param name="ipAddress">The IP address of the device if connected.</param>
        /// <returns><c>true</c> if the device is connected; otherwise, <c>false</c>.</returns>
        private bool IsAlreadyConnected(out string ipAddress)
        {
            ipAddress = null;
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                if (interfaces.Length == 0) return false;

                var netInterface = interfaces[0];
                ipAddress = netInterface.IPv4Address;
                return !string.IsNullOrEmpty(ipAddress) && ipAddress != "0.0.0.0" && ipAddress != "0";
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"Network interface error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tries to wait for a successful connection.
        /// </summary>
        /// <param name="result">The WiFi connection result.</param>
        /// <param name="ipAddress">The resulting IP address.</param>
        /// <returns>True if connection successful, false otherwise.</returns>
        private bool TryWaitForConnection(WifiConnectionResult result, out string ipAddress)
        {
            ipAddress = null;
            if (result.ConnectionStatus != WifiConnectionStatus.Success) return false;

            for (int i = 0; i < MAX_CONNECTION_ATTEMPTS && !_stopConnectionRequest.WaitOne(0, false); i++)
            {
                if (IsAlreadyConnected(out string currentIp))
                {
                    ipAddress = currentIp;
                    LogHelper.LogDebug($"Successfully obtained IP: {ipAddress}");
                    return true;
                }
                Thread.Sleep(CONNECTION_CHECK_INTERVAL_MS);
            }
            return false;
        }

        /// <summary>
        /// Handles logic for a successful connection.
        /// </summary>
        /// <param name="ipAddress">The assigned IP address.</param>
        private void HandleSuccessfulConnection(string ipAddress)
        {
            lock (_connectionLock)
            {
                _ipAddress = ipAddress;
                _isConnectionInProgress = false;
            }

            if (!_hasConnectedSuccessfully)
            {
                LogHelper.LogInformation($"Connection established. IP address: {ipAddress}");
                _hasConnectedSuccessfully = true;
            }
            else
            {
                LogHelper.LogInformation($"Connection restored. IP Address: {ipAddress}");
                RaiseConnectionRestored();
            }
        }

        /// <summary>
        /// Gets the error message corresponding to the Wi-Fi connection status.
        /// </summary>
        /// <param name="status">The Wi-Fi connection status.</param>
        /// <returns>The error message.</returns>
        private string GetErrorMessage(WifiConnectionStatus status)
        {
            return status switch
            {
                WifiConnectionStatus.AccessRevoked => "Access to the network has been revoked",
                WifiConnectionStatus.InvalidCredential => "Invalid credential was presented",
                WifiConnectionStatus.NetworkNotAvailable => "Network is not available",
                WifiConnectionStatus.Timeout => "Connection attempt timed out",
                WifiConnectionStatus.UnspecifiedFailure => "Unspecified error [connection refused]",
                WifiConnectionStatus.UnsupportedAuthenticationProtocol => "Authentication protocol is not supported",
                _ => "Unknown error"
            };
        }

        private bool InitializeWifiAdapter()
        {
            try
            {
                _wifiAdapter = WifiAdapter.FindAllAdapters()[0];
                LogHelper.LogInformation("WiFi adapter initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"Failed to initialize WiFi adapter: {ex.Message}");
                return false;
            }
        }

        private void RaiseConnectionRestored() => ConnectionRestored?.Invoke(this, EventArgs.Empty);
        private void RaiseConnectionLost() => ConnectionLost?.Invoke(this, EventArgs.Empty);
    }
}
