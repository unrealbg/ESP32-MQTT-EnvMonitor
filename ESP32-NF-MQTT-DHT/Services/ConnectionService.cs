namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Device.Wifi;
    using System.Net.NetworkInformation;
    using System.Threading;
    
    using Contracts;

    using Microsoft.Extensions.Logging;

    using static Constants.WifiSettings;
    using static Helpers.TimeHelper;

    /// <summary>
    /// Service responsible for managing the network connection of the device.
    /// </summary>
    public class ConnectionService : IConnectionService
    {
        private readonly ILogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionService"/> class.
        /// </summary>
        /// <param name="loggerFactory">Factory to create a logger for this service.</param>
        /// <exception cref="ArgumentNullException">Thrown if loggerFactory is null.</exception>
        public ConnectionService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger(nameof(ConnectionService)) ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Initiates a connection to the network.
        /// </summary>
        public void Connect()
        {
            var wifiAdapter = WifiAdapter.FindAllAdapters()[0];
            var count = 0;

            while (!IsAlreadyConnected())
            {
                _logger.LogInformation($"[{GetCurrentTimestamp()}] Connecting... [Attempt {++count}]");
                var result = wifiAdapter.Connect(SSID, WifiReconnectionKind.Automatic, Password);

                if (result.ConnectionStatus == WifiConnectionStatus.Success)
                {
                    _logger.LogInformation($"[{GetCurrentTimestamp()}] Connected to Wifi network {SSID}.");
                    break;
                }

                _logger.LogError($"[{GetCurrentTimestamp()}] Connection failed [{GetErrorMessage(result.ConnectionStatus)}]");
                Thread.Sleep(10000);
            }

            var ipAddress = NetworkInterface.GetAllNetworkInterfaces()[0].IPv4Address;
            _logger.LogInformation($"[{GetCurrentTimestamp()}] Connected to Wifi network {SSID} with IP address {ipAddress}");
        }

        private bool IsAlreadyConnected()
        {
            var ipAddress = NetworkInterface.GetAllNetworkInterfaces()[0].IPv4Address;
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
