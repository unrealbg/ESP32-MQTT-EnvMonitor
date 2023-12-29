namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    using nanoFramework.Runtime.Native;

    using Contracts;

    using static Settings.TcpSettings;
    using static Settings.DeviceSettings;
    using static Helpers.TimeHelper;

    /// <summary>
    /// Provides services for TCP listener functionalities including accepting and handling incoming TCP connections.
    /// </summary>
    public class TcpListenerService : ITcpListenerService
    {
        private const int TcpPort = 1234;
        private const int Timeout = 1000;

        private readonly IUptimeService _uptimeService;
        private readonly IMqttClientService _mqttClient;
        private readonly IDhtService _dhtService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpListenerService"/> class.
        /// </summary>
        /// <param name="uptimeService">Service to get uptime information.</param>
        /// <param name="mqttClient">Service to handle MQTT client functionalities.</param>
        /// <param name="dhtService">Service to interact with a DHT sensor.</param>
        public TcpListenerService(IUptimeService uptimeService, IMqttClientService mqttClient, IDhtService dhtService)
        {
            _uptimeService = uptimeService;
            _mqttClient = mqttClient;
            _dhtService = dhtService;
        }

        /// <summary>
        /// Starts the TCP listener service.
        /// </summary>
        /// <remarks>
        /// This method initializes and starts a TCP listener that listens for incoming connections
        /// on a specified port. It handles the connections in a separate thread.
        /// </remarks>
        public void Start()
        {
            Thread listenerThread = new Thread(StartTcpListening);
            listenerThread.Start();
        }

        private void StartTcpListening()
        {
            var localIP = NetworkInterface.GetAllNetworkInterfaces()[0].IPv4Address;
            TcpListener listener = new TcpListener(IPAddress.Any, TcpPort);
            listener.Server.ReceiveTimeout = Timeout;
            listener.Server.SendTimeout = Timeout;
            listener.Start(2);

            while (true)
            {
                Debug.WriteLine($"[{GetCurrentTimestamp()}] Waiting for an incoming connection on {localIP} port {TcpPort}");

                try
                {
                    using TcpClient client = listener.AcceptTcpClient();
                    IPEndPoint remoteIpEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                    string clientIp = remoteIpEndPoint!.Address.ToString();
                    Debug.WriteLine($"[{GetCurrentTimestamp()}] Client connected on port {TcpPort} from {clientIp}");

                    using NetworkStream stream = client.GetStream();
                    using StreamReader streamReader = new StreamReader(stream);
                    using StreamWriter sw = new StreamWriter(stream);

                    AuthenticateClient(streamReader, sw, clientIp);
                    ProcessClientCommands(streamReader, sw, client, clientIp);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{GetCurrentTimestamp()}] Exception:-{ex.Message}");
                }
            }
        }

        private void AuthenticateClient(StreamReader streamReader, StreamWriter sw, string clientIp)
        {
            sw.Write($"[{GetCurrentTimestamp()}] login as: ");
            sw.Flush();
            var usernameInput = streamReader.ReadLine();
            sw.Write($"[{GetCurrentTimestamp()}] {usernameInput}@{clientIp} password: ");
            sw.Flush();
            var passwordInput = streamReader.ReadLine();

            if (ClientUsername != usernameInput || ClientPassword != passwordInput)
            {
                throw new ArgumentException($"[{GetCurrentTimestamp()}] Invalid credentials.");
            }
        }

        private void ProcessClientCommands(StreamReader streamReader, StreamWriter sw, TcpClient client, string clientIp)
        {
            WriteToStream(sw, "Welcome to EspDuino-32 HW-729 (OpenSocket connection)\r\n\r\n *"
                                + " Documentation:  https://github.com/unrealbg/NF.Esp32.Mqtt.Dht21\r\n *"
                                + " Management:     https://github.com/unrealbg/NF.Esp32.Mqtt.Dht21\r\n *"
                                + " Support:        https://github.com/unrealbg/NF.Esp32.Mqtt.Dht21\r\n\r\n  "
                                + $"System information as of {DateTime.Today} {DateTime.Today.Date} {DateTime.Today.Day} EET {DateTime.Today.Year}\r\n\r\n  "
                                + "Available commands: uptime, temp, publishUptime, exit, reboot\r\n\r\n  "
                                + $"Users logged in:       1  IPv4 address for eth0: {NetworkInterface.GetAllNetworkInterfaces()[0].IPv4Address}\r\n");

            sw.Write($"[{GetCurrentTimestamp()}] [{ClientUsername}@{clientIp}]:~# ");
            sw.Flush();

            while (streamReader.Peek() > -1)
            {
                var command = streamReader.ReadLine();
                Debug.WriteLine($"[{GetCurrentTimestamp()}] [u] {command}");

                if (ProcessCommand(command, sw, client))
                {
                    break;
                }

                sw.Write($"[{GetCurrentTimestamp()}] [{ClientUsername}@{clientIp}] ");
                sw.Flush();
            }
        }

        private bool ProcessCommand(string command, StreamWriter sw, TcpClient client)
        {
            switch (command)
            {
                case "uptime":
                    WriteToStream(sw, _uptimeService.GetUptime());
                    return false;
                case "temp":
                    WriteToStream(sw, _dhtService.GetTemp().ToString());
                    return false;
                case "publishUptime":
                    _mqttClient.MqttClient.Publish($"home/{DeviceName}/uptime", Encoding.UTF8.GetBytes(_uptimeService.GetUptime()));
                    return false;
                case "exit":
                    Debug.WriteLine($"[{GetCurrentTimestamp()}] Client disconnected!");
                    client.Close();
                    return true;
                case "reboot":
                    Power.RebootDevice();
                    return true;
                default:
                    WriteToStream(sw, $"[{GetCurrentTimestamp()}] Unrecognized command");
                    return false;
            }
        }

        private void WriteToStream(StreamWriter writer, string text)
        {
            writer.WriteLine(text);
            writer.Flush();
        }
    }
}