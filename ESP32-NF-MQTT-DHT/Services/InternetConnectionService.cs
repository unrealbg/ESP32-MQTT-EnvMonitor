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
        private static readonly IPAddress GoogleDns = IPAddress.Parse("8.8.8.8");
        private readonly ManualResetEvent _stopSignal = new ManualResetEvent(false);
        private bool _isInternetThreadRunning = false;
        private Thread _internetCheckThread;

        public event EventHandler InternetLost;

        public event EventHandler InternetRestored;

        public bool IsInternetAvailable()
        {
            try
            {
                TcpClient tcpClient = new TcpClient();
                tcpClient.Connect(GoogleDns, 53);
                tcpClient.Close();
                return true;
            }
            catch
            {
                if (!_isInternetThreadRunning)
                {
                    _isInternetThreadRunning = true;
                    LogHelper.LogWarning("No internet connection, starting internet check thread...");
                    this.OnInternetLost();

                    _internetCheckThread = new Thread(new ThreadStart(this.CheckInternetConnectionLoop));
                    _internetCheckThread.Start();
                }
                return false;
            }
        }

        private void CheckInternetConnectionLoop()
        {
            while (!this.TryCheckInternet())
            {
                LogHelper.LogWarning("Internet not available, checking again in 10 seconds...");
                _stopSignal.WaitOne(10000, false);
            }

            LogHelper.LogInformation("Internet is back.");
            OnInternetRestored();
            _isInternetThreadRunning = false;
        }

        private bool TryCheckInternet()
        {
            try
            {
                TcpClient tcpClient = new TcpClient();
                tcpClient.Connect(GoogleDns, 53);
                tcpClient.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void OnInternetLost()
        {
            if (this.InternetLost != null)
            {
                this.InternetLost(this, EventArgs.Empty);
            }
        }

        private void OnInternetRestored()
        {
            if (this.InternetRestored != null)
            {
                this.InternetRestored(this, EventArgs.Empty);
            }
        }

        public void StopService()
        {
            _stopSignal.Set();
            if (_internetCheckThread != null && _internetCheckThread.IsAlive)
            {
                _internetCheckThread.Join();
            }
        }
    }
}
