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

    using ESP32_NF_MQTT_DHT.Helpers;

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
        private readonly ISensorService _sensorService;
        private readonly LogHelper _logHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpListenerService"/> class.
        /// </summary>
        /// <param name="uptimeService">Service to get uptime information.</param>
        /// <param name="mqttClient">Service to handle MQTT client functionalities.</param>
        /// <param name="sensorService">Service to interact with a DHT sensor.</param>
        public TcpListenerService(IUptimeService uptimeService, IMqttClientService mqttClient, ISensorService sensorService, LogHelper logHelper)
        {
            _uptimeService = uptimeService;
            _mqttClient = mqttClient;
            _sensorService = sensorService;
            this._logHelper = logHelper;
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
                this._logHelper.LogWithTimestamp(
                    $"Waiting for an incoming connection on {localIP} port {TcpPort}");

                try
                {
                    using TcpClient client = listener.AcceptTcpClient();
                    IPEndPoint remoteIpEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                    string clientIp = remoteIpEndPoint!.Address.ToString();
                    this._logHelper
                        .LogWithTimestamp($"Client connected on port {TcpPort} from {clientIp}");

                    using NetworkStream stream = client.GetStream();
                    using StreamReader streamReader = new StreamReader(stream);
                    using StreamWriter sw = new StreamWriter(stream);

                    AuthenticateClient(streamReader, sw, clientIp);
                    ProcessClientCommands(streamReader, sw, client, clientIp);
                }
                catch (Exception ex)
                {
                    this._logHelper
                        .LogWithTimestamp($"Exception occurred while processing client request: {ex.Message}");
                }
            }
        }

        private void AuthenticateClient(StreamReader streamReader, StreamWriter sw, string clientIp)
        {
            try
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
            catch (Exception e)
            {
                this._logHelper
                    .LogWithTimestamp("Login incorrect");
                sw.WriteLine("Login incorrect");
                sw.Flush();
                throw;
            }

        }

        private void ProcessClientCommands(StreamReader streamReader, StreamWriter sw, TcpClient client, string clientIp)
        {
            SendWelcomeMessage(sw);

            sw.Write($"[{GetCurrentTimestamp()}] [{ClientUsername}@{clientIp}]:~# ");
            sw.Flush();

            while (streamReader.Peek() > -1)
            {
                var command = streamReader.ReadLine();
                this._logHelper
                    .LogWithTimestamp($"Command received: {command}");

                if (ProcessCommand(command, sw))
                {
                    break;
                }

                sw.Write($"[{GetCurrentTimestamp()}] [{ClientUsername}@{clientIp}] ");
                sw.Flush();
            }
        }

        private bool ProcessCommand(string command, StreamWriter sw)
        {
            switch (command)
            {
                case "uptime":
                    WriteToStream(sw, _uptimeService.GetUptime());
                    return false;
                case "temp":
                    WriteToStream(sw, _sensorService.GetTemp().ToString());
                    return false;
                case "humidity":
                    WriteToStream(sw, _sensorService.GetHumidity().ToString());
                    return false;
                case "publishTemp":
                    _mqttClient.MqttClient.Publish($"home/{DeviceName}/temperature", Encoding.UTF8.GetBytes(_sensorService.GetTemp().ToString()));
                    return false;
                case "status":
                    WriteToStream(sw, $"Temp: {_sensorService.GetTemp()} C, Humidity: {_sensorService.GetHumidity()} %, Uptime: {_uptimeService.GetUptime()}");
                    return false;
                case "publishUptime":
                    _mqttClient.MqttClient.Publish($"home/{DeviceName}/uptime", Encoding.UTF8.GetBytes(_uptimeService.GetUptime()));
                    return false;
                case "getIpAddress":
                    WriteToStream(sw, NetworkInterface.GetAllNetworkInterfaces()[0].IPv4Address);
                    return false;
                case "exit":
                    return true;
                case "reboot":
                    Power.RebootDevice();
                    return true;
                default:
                    WriteToStream(sw, "Unrecognized command");
                    return false;
            }
        }

        private void WriteToStream(StreamWriter writer, string text)
        {
            writer.WriteLine(text);
            writer.Flush();
        }

        private void SendWelcomeMessage(StreamWriter sw)
        {
            var welcomeMessage = $@"
             Welcome to ESP32-NF MQTT EnvMonitor!

             * Device: {DeviceName}
             * Firmware Version: 1.0.0
             * IP Address: {NetworkInterface.GetAllNetworkInterfaces()[0].IPv4Address}

             Available commands:
             - uptime          : Shows the system uptime
             - temp            : Displays the current temperature
             - humidity        : Displays the current humidity
             - publishTemp     : Publishes the temperature to MQTT
             - status          : Shows system status (temp, humidity, uptime)
             - publishUptime   : Publishes the uptime to MQTT
             - getIpAddress    : Returns the device IP address
             - exit            : Exits the session
             - reboot          : Reboots the device

             Type a command:
             ";

            WriteToStream(sw, welcomeMessage);
        }
    }
}