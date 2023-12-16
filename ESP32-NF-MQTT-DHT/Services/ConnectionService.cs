namespace ESP32_NF_MQTT_DHT.Services
{
    using System.Device.Wifi;
    using System.Diagnostics;
    using System.Net.NetworkInformation;
    using System.Threading;

    using Constants;

    using Contracts;

    public class ConnectionService : IConnectionService
    {
        public void Connect()
        {
            if (IsAlreadyConnected())
            {
                Debug.WriteLine("[+] The device is already connected...");
                Thread.Sleep(5000);
                return;
            }

            var wifiAdapter = WifiAdapter.FindAllAdapters()[0];
            var count = 0;

            do
            {
                Debug.WriteLine($"[*] Connecting... [Attempt {++count}]");
                var result = wifiAdapter.Connect(Constants.SSID, WifiReconnectionKind.Automatic, Constants.WIFI_PASSWORD);
                if (result.ConnectionStatus == WifiConnectionStatus.Success)
                {
                    Debug.WriteLine($"[+] Connected to Wifi network {Constants.SSID}.");
                    Thread.Sleep(2000);
                    break;
                }
                else
                {
                    Debug.WriteLine($"[-] Connection failed [{GetErrorMessage(result.ConnectionStatus)}]");
                    Thread.Sleep(10000);
                }
            } while (true);

            var ipAddress = NetworkInterface.GetAllNetworkInterfaces()[0].IPv4Address;
            Debug.WriteLine($"[+] Connected to Wifi network {Constants.SSID} with IP address {ipAddress}");
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
