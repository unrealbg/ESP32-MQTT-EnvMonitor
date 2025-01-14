namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Services.Contracts;

    public class InternetConnectionService : IInternetConnectionService
    {
        private readonly ManualResetEvent _stopSignal = new ManualResetEvent(false);
        private bool _isInternetThreadRunning = false;
        private Thread _internetCheckThread;

        public event EventHandler InternetLost;

        public event EventHandler InternetRestored;

        public bool IsInternetAvailable()
        {
            try
            {
                using (TcpClient tcpClient = new TcpClient())
                {
                    var ipAddress = IPAddress.Parse("8.8.8.8");
                    tcpClient.Connect(ipAddress, 53);
                    return true;
                }
            }
            catch
            {
                if (!_isInternetThreadRunning)
                {
                    _isInternetThreadRunning = true;
                    LogHelper.LogWarning("No internet connection, starting internet check thread...");

                    this.OnInternetLost();

                    _internetCheckThread = new Thread(this.CheckInternetConnectionLoop);
                    _internetCheckThread.Start();
                }

                return false;
            }
        }

        private void CheckInternetConnectionLoop()
        {
            while (!this.IsInternetAvailable())
            {
                LogHelper.LogWarning("Internet not available, checking again in 10 seconds...");
                _stopSignal.WaitOne(10000, false);
            }

            LogHelper.LogInformation("Internet is back.");

            this.OnInternetRestored();

            _isInternetThreadRunning = false;
        }

        private void OnInternetLost()
        {
            this.InternetLost?.Invoke(this, EventArgs.Empty);
        }

        private void OnInternetRestored()
        {
            this.InternetRestored?.Invoke(this, EventArgs.Empty);
        }
    }
}
