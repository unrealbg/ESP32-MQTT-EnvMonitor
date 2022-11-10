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
            var wifiAdapter = WifiAdapter.FindAllAdapters()[0];

            var ipAddress = NetworkInterface.GetAllNetworkInterfaces()[0].IPv4Address;
            var needToConnect = string.IsNullOrEmpty(ipAddress) || (ipAddress == "0.0.0.0");

            // Checking that we are not already connected
            if (!needToConnect)
            {
                Debug.WriteLine("[+] The device is already connected...");
                Thread.Sleep(5000);
                return;
            }

            var count = 0;

            while (true)
            {
                Debug.WriteLine($"[*] Connecting... [Attempt {++count}]");

                // Connect to network
                var result = wifiAdapter.Connect(
                    Constants.SSID,
                    WifiReconnectionKind.Automatic,
                    Constants.WIFI_PASSWORD);

                if (result.ConnectionStatus == WifiConnectionStatus.Success)
                {
                    Debug.WriteLine($"[+] Connected to Wifi network {Constants.SSID}.");
                    Thread.Sleep(2000);
                    break;
                }
                else
                {
                    // more detailed error message
                    var errorMsg = string.Empty;

                    switch (result.ConnectionStatus.ToString())
                    {
                        case "0":
                            errorMsg = "Access to the network has been revoked.";
                            break;
                        case "1":
                            errorMsg = "Invalid credential was presented.";
                            break;
                        case "2":
                            errorMsg = "Network is not available.";
                            break;
                        case "4":
                            errorMsg = "Connection attempt timed out.";
                            break;
                        case "5":
                            errorMsg = "Unspecified error [connection refused]";
                            break;
                        case "6":
                            errorMsg = "Authentication protocol is not supported.";
                            break;
                    }

                    Debug.WriteLine(
                        $"[-] Connection failed [{errorMsg}]");


                    // wait 10 seconds before the next attempt
                    Thread.Sleep(10000);
                }
            }
            
            // Get IPv4 Address
            ipAddress = NetworkInterface.GetAllNetworkInterfaces()[0].IPv4Address;
            Debug.WriteLine($"[+] Connected to Wifi network {Constants.SSID} with IP address {ipAddress} ");
        }
    }
}
