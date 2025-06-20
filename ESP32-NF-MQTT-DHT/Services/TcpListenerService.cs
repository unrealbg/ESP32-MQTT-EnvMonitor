namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    using Contracts;

    using ESP32_NF_MQTT_DHT.Helpers;

    using nanoFramework.Runtime.Native;

    using static Settings.DeviceSettings;
    using static Settings.TcpSettings;

    using GC = nanoFramework.Runtime.Native.GC;

    public delegate bool CommandHandler(string[] args, StreamWriter writer);

    /// <summary>
    /// Provides TCP listener services – handles incoming TCP connections and commands.
    /// </summary>
    public class TcpListenerService : ITcpListenerService, IDisposable
    {
        private const int TcpPort = 31337;
        private const int Timeout = 5000;
        private const int MaxAuthAttempts = 3;

        private readonly IUptimeService _uptimeService;
        private readonly IMqttClientService _mqttClient;
        private readonly ISensorService _sensorService;
        private readonly IRelayService _relayService;
        private readonly IConnectionService _connectionService;

        private bool _isRunning;
        private Thread _listenerThread;
        private TcpListener _listener;
        private bool _disposed = false;

        // TODO: Not applied to sensor service
        private int _sensorInterval = 1000; 

        private readonly Hashtable _commandDescriptions = new Hashtable();
        private readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the TcpListenerService class.
        /// </summary>
        public TcpListenerService(IUptimeService uptimeService, IMqttClientService mqttClient, ISensorService sensorService, IRelayService relayService, IConnectionService connectionService)
        {
            _uptimeService = uptimeService;
            _mqttClient = mqttClient;
            _sensorService = sensorService;
            _relayService = relayService;
            _connectionService = connectionService;

            _isRunning = false;

            _commandDescriptions.Add("uptime", "Displays the device uptime.");
            _commandDescriptions.Add("temp", "Displays the current temperature.");
            _commandDescriptions.Add("humidity", "Displays the current humidity.");
            _commandDescriptions.Add("publishtemp", "Publishes the temperature via MQTT.");
            _commandDescriptions.Add("status", "Displays device status (temperature, humidity, uptime).");
            _commandDescriptions.Add("publishuptime", "Publishes the device uptime via MQTT.");
            _commandDescriptions.Add("getipaddress", "Displays the device's IP address.");
            _commandDescriptions.Add("help", "Lists available commands.");
            _commandDescriptions.Add("info", "Displays additional device information.");
            _commandDescriptions.Add("ping", "Sends a ping to a specified IP. Usage: ping <IP> (Not supported on this platform)");
            _commandDescriptions.Add("setinterval", "Sets the sensor read interval in milliseconds. Usage: setInterval <milliseconds>");
            _commandDescriptions.Add("relay", "Controls the relay. Usage: relay on|off|status");
            _commandDescriptions.Add("diagnostic", "Displays diagnostic info (free memory).");
            _commandDescriptions.Add("getlogs", "Retrieves the device logs.");
            _commandDescriptions.Add("clearlogs", "Clears the device logs.");
            _commandDescriptions.Add("exit", "Exits the session.");
            _commandDescriptions.Add("reboot", "Reboots the device.");

            _connectionService.ConnectionLost += this.ConnectionLost;
            _connectionService.ConnectionRestored += this.ConnectionRestored;
        }

        /// <summary>
        /// Starts the TCP listener in a separate thread.
        /// </summary>
        public void Start()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TcpListenerService));
            }

            lock (_lock)
            {
                if (_isRunning)
                {
                    LogHelper.LogInformation("TCP Listener already running");
                    return;
                }
                _isRunning = true;
            }

            _listenerThread = new Thread(this.StartTcpListening);
            _listenerThread.Start();

            LogHelper.LogInformation("TCP listener started");
        }

        /// <summary>
        /// Stops the TCP listener gracefully.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                _isRunning = false;
            }

            if (_listener != null)
            {
                try
                {
                    _listener.Stop();
                }
                catch (Exception ex)
                {
                    LogHelper.LogError("Error stopping TCP listener: " + ex.Message);
                }
            }

            if (_listenerThread != null)
            {
                _listenerThread.Join(5000);
            }

            LogHelper.LogInformation("TCP listener stopped");
        }

        private static string GetCurrentIpAddress()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            if (interfaces == null || interfaces.Length == 0)
            {
                return string.Empty;
            }

            var networkInterface = interfaces[0];
            var ipAddress = networkInterface.IPv4Address;
            return ipAddress;
        }

        /// <summary>
        /// Main method for listening to incoming connections.
        /// </summary>
        private void StartTcpListening()
        {
            try
            {
                int retryCount = 0;
                string ipAddress = GetCurrentIpAddress();
                while ((string.IsNullOrEmpty(ipAddress) || ipAddress == "0.0.0.0") && _isRunning && retryCount < 60)
                {
                    LogHelper.LogWarning("TCPListener delayed: No valid IP address yet. Retrying...");
                    Thread.Sleep(5000);
                    retryCount++;
                    ipAddress = GetCurrentIpAddress();
                }
                if (string.IsNullOrEmpty(ipAddress) || ipAddress == "0.0.0.0")
                {
                    LogHelper.LogWarning("TCPListener could not start: No valid IP address after retries.");
                    return;
                }

                _listener = new TcpListener(IPAddress.Any, TcpPort);
                _listener.Server.ReceiveTimeout = Timeout;
                _listener.Server.SendTimeout = Timeout;
                _listener.Start(2);

                while (_isRunning)
                {
                    LogHelper.LogInformation("Waiting for an incoming connection on " + GetCurrentIpAddress() + " port " + TcpPort);
                    try
                    {
                        // Accept a client connection (synchronously).
                        TcpClient client = _listener.AcceptTcpClient();
                        IPEndPoint remoteIpEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                        string clientIp = remoteIpEndPoint != null ? remoteIpEndPoint.Address.ToString() : "Unknown";
                        LogHelper.LogInformation("Client connected on port " + TcpPort + " from " + clientIp);

                        using (client)
                        {
                            using (NetworkStream stream = client.GetStream())
                            {
                                if (!stream.CanRead || !stream.CanWrite)
                                {
                                    LogHelper.LogError("Stream not properly accessible");
                                    client.Close();
                                    continue;
                                }

                                using (StreamReader streamReader = new StreamReader(stream))
                                {
                                    using (StreamWriter sw = new StreamWriter(stream))
                                    {
                                        try
                                        {
                                            if (!this.AuthenticateClient(streamReader, sw, clientIp))
                                            {
                                                LogHelper.LogInformation("Authentication failed for client: " + clientIp);
                                                client.Close();
                                                continue;
                                            }

                                            try
                                            {
                                                this.SendWelcomeMessage(sw, clientIp);
                                            }
                                            catch (Exception welcome_ex)
                                            {
                                                LogHelper.LogError("Error sending welcome message: " + welcome_ex.Message);
                                                client.Close();
                                                continue;
                                            }

                                            this.ProcessClientCommands(streamReader, sw, clientIp);
                                        }
                                        catch (Exception sessionEx)
                                        {
                                            LogHelper.LogError("Exception during client session: " + sessionEx.Message);
                                        }
                                    }
                                }
                            }

                            LogHelper.LogInformation("Client disconnected: " + clientIp);
                        }
                    }
                    catch (SocketException socketEx)
                    {
                        if (_isRunning) // Only log if we're still supposed to be running
                        {
                            LogHelper.LogError("Socket exception while accepting client: " + socketEx.Message);
                        }
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        if (_isRunning) // Only log if we're still supposed to be running
                        {
                            LogHelper.LogError("Exception occurred while processing client request: " + ex.Message);
                        }
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception initEx)
            {
                LogHelper.LogError("Fatal error in TCP listener: " + initEx.Message);
                LogService.LogCritical("Fatal error in TCP listener: " + initEx.Message);
            }
            // Cleanup is now handled by Dispose()
        }

        /// <summary>
        /// Authenticates the client with a limited number of attempts.
        /// Prompts are written inline so the user input appears on the same line.
        /// </summary>
        private bool AuthenticateClient(StreamReader streamReader, StreamWriter sw, string clientIp)
        {
            for (int attempt = 1; attempt <= MaxAuthAttempts; attempt++)
            {
                try
                {
                    if (!streamReader.BaseStream.CanRead)
                    {
                        LogHelper.LogError("Stream not readable during authentication");
                        return false;
                    }

                    this.WriteInline(sw, LogMessages.TimeStamp + " login as: ");
                    string usernameInput = streamReader.ReadLine();

                    if (string.IsNullOrEmpty(usernameInput))
                    {
                        LogHelper.LogError("Client disconnected during authentication - no username provided");
                        return false;
                    }

                    this.WriteInline(sw, LogMessages.TimeStamp + " " + usernameInput + "@" + clientIp + " password: ");
                    string passwordInput = streamReader.ReadLine();

                    if (string.IsNullOrEmpty(passwordInput))
                    {
                        LogHelper.LogError("Client disconnected during authentication - no password provided");
                        return false;
                    }

                    if (usernameInput == ClientUsername && passwordInput == ClientPassword)
                    {
                        return true;
                    }

                    this.WriteToStream(sw, "Login incorrect");
                    LogHelper.LogError("Login incorrect for " + clientIp);
                    Thread.Sleep(1000);
                }
                catch (IOException ex)
                {
                    LogHelper.LogError("IO Exception during authentication: " + ex.Message);
                    return false;
                }
                catch (SocketException ex)
                {
                    LogHelper.LogError("Socket Exception during authentication: " + ex.Message);
                    return false;
                }
                catch (Exception ex)
                {
                    LogHelper.LogError("Authentication error: " + ex.Message);
                    LogService.LogCritical("Authentication error: " + ex.Message);
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Registers available commands and returns a Hashtable of command handlers.
        /// </summary>
        private Hashtable RegisterCommands()
        {
            Hashtable commands = new Hashtable();
            commands.Add("uptime", new CommandHandler((args, writer) => { this.WriteToStream(writer, _uptimeService.GetUptime()); return false; }));
            commands.Add("temp", new CommandHandler((args, writer) => { this.WriteToStream(writer, _sensorService.GetTemp().ToString()); return false; }));
            commands.Add("humidity", new CommandHandler((args, writer) => { this.WriteToStream(writer, _sensorService.GetHumidity().ToString()); return false; }));
            commands.Add("publishtemp", new CommandHandler((args, writer) =>
            {
                if (_mqttClient.MqttClient == null)
                {
                    LogHelper.LogError("MQTT client is not initialized.");
                    this.WriteToStream(writer, "MQTT client is not initialized.");
                    return false;
                }

                try
                {
                    _mqttClient.MqttClient.Publish("home/" + DeviceName + "/temperature", Encoding.UTF8.GetBytes(_sensorService.GetTemp().ToString()));
                }
                catch (Exception ex)
                {
                    LogHelper.LogError("Error publishing temperature: " + ex.Message);
                    this.WriteToStream(writer, "Error publishing temperature: " + ex.Message);
                }

                return false;
            }));
            commands.Add("status", new CommandHandler((args, writer) =>
            {
                this.WriteToStream(writer, "Temp: " + _sensorService.GetTemp() + " C, Humidity: " + _sensorService.GetHumidity() + " %, Uptime: " + _uptimeService.GetUptime());
                return false;
            }));
            commands.Add("publishuptime", new CommandHandler((args, writer) =>
            {
                if (_mqttClient.MqttClient == null)
                {
                    LogHelper.LogError("MQTT client is not initialized.");
                    this.WriteToStream(writer, "MQTT client is not initialized.");
                    return false;
                }

                try
                {
                    _mqttClient.MqttClient.Publish("home/" + DeviceName + "/uptime", Encoding.UTF8.GetBytes(_uptimeService.GetUptime()));
                }
                catch (Exception ex)
                {
                    LogHelper.LogError("Error publishing uptime: " + ex.Message);
                    this.WriteToStream(writer, "Error publishing uptime: " + ex.Message);
                }

                return false;
            }));
            commands.Add("getipaddress", new CommandHandler((args, writer) => { this.WriteToStream(writer, GetCurrentIpAddress()); return false; }));
            commands.Add("help", new CommandHandler((args, writer) => { this.DisplayHelp(writer); return false; }));
            commands.Add("info", new CommandHandler((args, writer) =>
            {
                Version firmwareVersion = SystemInfo.Version;
                string versionString = $"{firmwareVersion.Major}.{firmwareVersion.Minor}.{firmwareVersion.Build}.{firmwareVersion.Revision}";
                string processor = SystemInfo.OEMString;
                string familyName = SystemInfo.TargetName;
                uint freeMemory = GC.Run(false);

                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                string ip = interfaces != null && interfaces.Length > 0 ? interfaces[0].IPv4Address : "N/A";
                string mac = interfaces != null && interfaces.Length > 0 ? BitConverter.ToString(interfaces[0].PhysicalAddress) : "N/A";

                string info = $"Device: {DeviceName}\r\n" +
                              $"Firmware Version: {versionString}\r\n" +
                              $"Platform: {SystemInfo.Platform}\r\n" +
                              $"Family: {familyName}\r\n" +
                              $"CPU: {processor}\r\n" +
                              $"Free RAM: {freeMemory} bytes\r\n" +
                              $"IP: {ip}\r\n" +
                              $"MAC: {mac}\r\n" +
                              $"Uptime: {_uptimeService.GetUptime()}\r\n" +
                              $"Sensor Interval: {_sensorInterval} ms (TODO: Not applied to sensor service)";
                this.WriteToStream(writer, info);
                return false;
            }));
            commands.Add("ping", new CommandHandler((args, writer) =>
            {
                if (args.Length < 2)
                {
                    this.WriteToStream(writer, "Usage: ping <IP>");
                }
                else
                {
                    this.WriteToStream(writer, "Ping command is not supported on this platform.");
                }

                return false;
            }));
            commands.Add("setinterval", new CommandHandler((args, writer) =>
            {
                if (args.Length < 2)
                {
                    this.WriteToStream(writer, "Usage: setInterval <milliseconds>");
                }
                else
                {
                    int newInterval;
                    if (int.TryParse(args[1], out newInterval))
                    {
                        _sensorInterval = newInterval;
                        this.WriteToStream(writer, "Sensor interval set to " + _sensorInterval + " ms");
                    }
                    else
                    {
                        this.WriteToStream(writer, "Invalid interval value");
                    }
                }

                return false;
            }));
            commands.Add("relay", new CommandHandler((args, writer) =>
            {
                if (args.Length < 2)
                {
                    this.WriteToStream(writer, "Usage: relay on|off|status");
                }
                else
                {
                    string relayCommand = args[1].ToLower();
                    if (relayCommand == "on")
                    {
                        this.WriteToStream(writer, "Relay turned ON");
                        _relayService.TurnOn();
                    }
                    else if (relayCommand == "off")
                    {
                        this.WriteToStream(writer, "Relay turned OFF");
                        _relayService.TurnOff();
                    }
                    else if (relayCommand == "status")
                    {
                        this.WriteToStream(writer, "Relay status: " + (_relayService.IsRelayOn() ? "ON" : "OFF"));
                    }
                    else
                    {
                        this.WriteToStream(writer, "Usage: relay on|off|status");
                    }
                }
                return false;
            }));
            commands.Add("diagnostic", new CommandHandler((args, writer) =>
            {
                long freeMemory = GC.Run(true);
                this.WriteToStream(writer, "Free memory: " + freeMemory + " bytes");
                return false;
            }));
            commands.Add("getlogs", new CommandHandler((args, writer) =>
            {
                string logs = LogService.ReadLatestLogs();
                this.WriteToStream(writer, logs);
                return false;
            }));
            commands.Add("clearlogs", new CommandHandler((args, writer) =>
            {
                LogService.ClearLogs();
                this.WriteToStream(writer, "Logs cleared");
                return false;
            }));
            commands.Add("exit", new CommandHandler((args, writer) => { return true; }));
            commands.Add("reboot", new CommandHandler((args, writer) =>
            {
                Power.RebootDevice();
                return true;
            }));
            return commands;
        }

        /// <summary>
        /// Processes client commands.
        /// </summary>
        private void ProcessClientCommands(StreamReader streamReader, StreamWriter sw, string clientIp)
        {
            Hashtable commandHandlers = this.RegisterCommands();

            this.WritePrompt(sw, clientIp);

            while (true)
            {
                try
                {
                    if (!streamReader.BaseStream.CanRead)
                    {
                        LogHelper.LogError("Stream no longer readable during command processing");
                        break;
                    }

                    Thread.Sleep(100);

                    string input = null;
                    try
                    {
                        input = streamReader.ReadLine();
                    }
                    catch (IOException ioEx)
                    {
                        LogHelper.LogError("IO Exception while reading command: " + ioEx.Message);
                        break;
                    }
                    catch (SocketException sockEx)
                    {
                        LogHelper.LogError("Socket Exception while reading command: " + sockEx.Message);
                        break;
                    }

                    if (input == null)
                    {
                        LogHelper.LogInformation("Client disconnected or timeout occurred");
                        break;
                    }

                    LogHelper.LogInformation("Command received: " + input);

                    string[] parts = input.Trim().Split(' ');
                    if (parts.Length == 0)
                    {
                        this.WritePrompt(sw, clientIp);
                        continue;
                    }

                    string commandKey = parts[0].ToLower();
                    bool shouldExit = false;

                    try
                    {
                        if (commandHandlers.Contains(commandKey))
                        {
                            CommandHandler handler = (CommandHandler)commandHandlers[commandKey];
                            shouldExit = handler(parts, sw);
                        }
                        else
                        {
                            LogHelper.LogError($"Unrecognized command: {commandKey}");
                            this.WriteToStream(sw, "Unrecognized command");
                        }
                    }
                    catch (Exception cmdEx)
                    {
                        LogHelper.LogError("Error executing command '" + commandKey + "': " + cmdEx.Message);
                        try
                        {
                            this.WriteToStream(sw, "Error executing command: " + cmdEx.Message);
                        }
                        catch
                        {
                            break;
                        }
                    }

                    if (shouldExit)
                    {
                        break;
                    }

                    try
                    {
                        this.WritePrompt(sw, clientIp);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogError("Error writing prompt: " + ex.Message);
                        break;
                    }
                }
                catch (IOException ex)
                {
                    LogHelper.LogError("IO Exception during command processing: " + ex.Message);
                    break;
                }
                catch (SocketException ex)
                {
                    LogHelper.LogError("Socket Exception during command processing: " + ex.Message);
                    break;
                }
                catch (Exception ex)
                {
                    LogHelper.LogError("Exception during command processing: " + ex.Message);
                    LogService.LogCritical("Exception during command processing: " + ex.Message);
                    break;
                }
            }

            LogHelper.LogInformation("Command processing ended for client: " + clientIp);
        }

        /// <summary>
        /// Displays the list of available commands and their descriptions.
        /// </summary>
        private void DisplayHelp(StreamWriter sw)
        {
            StringBuilder helpText = new StringBuilder();
            helpText.Append("Available commands:\r\n");
            foreach (DictionaryEntry entry in _commandDescriptions)
            {
                helpText.Append(entry.Key + " - " + entry.Value + "\r\n");
            }

            this.WriteToStream(sw, helpText.ToString());
        }

        /// <summary>
        /// Writes a prompt to the client using inline output.
        /// </summary>
        private void WritePrompt(StreamWriter sw, string clientIp)
        {
            this.WriteInline(sw, LogMessages.TimeStamp + " " + ClientUsername + "@" + clientIp + "]:~# ");
        }

        /// <summary>
        /// Writes text to the stream with a newline and flushes.
        /// </summary>
        private void WriteToStream(StreamWriter writer, string text)
        {
            try
            {
                writer.WriteLine(text);
                writer.Flush();
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Error writing to stream: " + ex.Message);
                LogService.LogCritical("Error writing to stream: " + ex.Message);
            }
        }

        /// <summary>
        /// Writes text to the stream without a newline and flushes.
        /// </summary>
        private void WriteInline(StreamWriter writer, string text)
        {
            try
            {
                writer.Write(text);
                writer.Flush();
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Error writing inline to stream: " + ex.Message);
                LogService.LogCritical("Error writing inline to stream: " + ex.Message);
            }
        }

        /// <summary>
        /// Sends a welcome message with a list of available commands.
        /// </summary>
        private void SendWelcomeMessage(StreamWriter sw, string clientIp)
        {
            Version firmwareVersion = SystemInfo.Version;
            string versionString = $"{firmwareVersion.Major}.{firmwareVersion.Minor}.{firmwareVersion.Build}.{firmwareVersion.Revision}";
            string welcomeMessage =
                "Welcome to ESP32-NF MQTT EnvMonitor!\r\n" +
                "\r\n" +
                "* Device: " + DeviceName + "\r\n" +
                "* Firmware Version: " + versionString + "\r\n" +
                "* IP Address: " + GetCurrentIpAddress() + "\r\n" +
                "\r\n" +
                "Available commands:\r\n" +
                " - uptime          : Displays the device uptime.\r\n" +
                " - temp            : Displays the current temperature.\r\n" +
                " - humidity        : Displays the current humidity.\r\n" +
                " - publishTemp     : Publishes the temperature via MQTT.\r\n" +
                " - status          : Displays device status (temperature, humidity, uptime).\r\n" +
                " - publishUptime   : Publishes the device uptime via MQTT.\r\n" +
                " - getIpAddress    : Displays the device's IP address.\r\n" +
                " - help            : Lists available commands.\r\n" +
                " - info            : Displays additional device information.\r\n" +
                " - ping <IP>       : Sends a ping request. (Not supported)\r\n" +
                " - relay on|off    : on|off|status, Controls the relay.\r\n" +
                " - diagnostic      : Displays diagnostic info (free memory).\r\n" +
                " - getlogs         : Retrieves the device logs.\r\n" +
                " - clearlogs       : Clears the device logs.\r\n" +
                " - exit            : Exits the session.\r\n" +
                " - reboot          : Reboots the device.\r\n" +
                "\r\n" +
                "Type a command:";
            this.WriteToStream(sw, welcomeMessage);
        }

        private void ConnectionRestored(object sender, EventArgs e)
        {
            if (!_isRunning && !_disposed)
            {
                LogHelper.LogInformation("Wi-Fi connection restored. Starting TCP listener...");
                this.Start();
            }
        }

        private void ConnectionLost(object sender, EventArgs e)
        {
            if (!_disposed)
            {
                this.Stop();
            }
        }

        #region IDisposable Implementation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases the unmanaged resources and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Stop the service
                    Stop();

                    // Unsubscribe from events to prevent memory leaks
                    if (_connectionService != null)
                    {
                        _connectionService.ConnectionLost -= this.ConnectionLost;
                        _connectionService.ConnectionRestored -= this.ConnectionRestored;
                    }

                    // Dispose managed resources
                    if (_listener != null)
                    {
                        try
                        {
                            _listener.Stop();
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogError("Error disposing TCP listener: " + ex.Message);
                        }

                        _listener = null;
                    }

                    // Clean up thread reference
                    _listenerThread = null;

                    // Clear command descriptions
                    if (_commandDescriptions != null)
                    {
                        _commandDescriptions.Clear();
                    }
                }

                _disposed = true;
                LogHelper.LogInformation("TcpListenerService disposed");
            }
        }

        #endregion
    }
}