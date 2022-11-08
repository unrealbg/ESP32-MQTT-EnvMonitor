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

            // Begin network scan.
            wifiAdapter.ScanAsync();

            // While networks are being scan, continue on configuration. If networks were set previously, 
            // board may already be auto-connected, so reconnection is not even needed.
            var ipAddress = NetworkInterface.GetAllNetworkInterfaces()[0].IPv4Address;
            var isConnected = false;
            while (isConnected == false)
            {
                foreach (var network in wifiAdapter.NetworkReport.AvailableNetworks)
                {
                    // If its our Network then try to connect
                    if (network.Ssid == Constants.SSID)
                    {
                        var result = wifiAdapter.Connect(
                            network,
                            WifiReconnectionKind.Automatic,
                            Constants.WIFI_PASSWORD);

                        if (result.ConnectionStatus == WifiConnectionStatus.Success)
                        {
                            Debug.WriteLine($"[+] Connected to Wifi network {network.Ssid}.");
                            isConnected = true;
                        }
                        else
                        {
                            Debug.WriteLine(
                                $"[-] Error {result.ConnectionStatus} connecting to Wifi network {network.Ssid}.");
                        }
                    }
                }

                if (isConnected == false)
                {
                    wifiAdapter.ScanAsync();
                }

                Thread.Sleep(10000);
            }

            ipAddress = NetworkInterface.GetAllNetworkInterfaces()[0].IPv4Address;
            Debug.WriteLine($"[+] Connected to Wifi network {Constants.SSID} with IP address {ipAddress} ");
        }
    }
}
