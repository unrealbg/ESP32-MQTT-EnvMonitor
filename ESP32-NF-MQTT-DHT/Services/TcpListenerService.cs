namespace ESP32_NF_MQTT_DHT.Services
{
    using Contracts;

    using ESP32_NF_MQTT_DHT.Helpers;

    using nanoFramework.Hardware.Esp32;
    using nanoFramework.Runtime.Native;

    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    using static Settings.DeviceSettings;
    using static Settings.TcpSettings;

    using GC = nanoFramework.Runtime.Native.GC;

    // Delegate for command handlers.
    public delegate bool CommandHandler(string[] args, StreamWriter writer);

    /// <summary>
    /// Provides TCP listener services – handles incoming TCP connections and commands.
    /// </summary>
    public class TcpListenerService : ITcpListenerService
    {
        private const int TcpPort = 31337;
        private const int Timeout = 1000;
        private const int MaxAuthAttempts = 3;

        private readonly IUptimeService _uptimeService;
        private readonly IMqttClientService _mqttClient;
        private readonly ISensorService _sensorService;
        private readonly IRelayService _relayService;
        private readonly NetworkInterface _networkInterface;

        private bool _isRunning;
        private Thread _listenerThread;

        private int _sensorInterval = 1000;

        private readonly Hashtable _commandDescriptions = new Hashtable();

        /// <summary>
        /// Initializes a new instance of the TcpListenerService class.
        /// </summary>
        public TcpListenerService(IUptimeService uptimeService, IMqttClientService mqttClient, ISensorService sensorService, IRelayService relayService)
        {
            _uptimeService = uptimeService;
            _mqttClient = mqttClient;
            _sensorService = sensorService;
            _relayService = relayService;
            _networkInterface = NetworkInterface.GetAllNetworkInterfaces()[0];
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
            _commandDescriptions.Add("exit", "Exits the session.");
            _commandDescriptions.Add("reboot", "Reboots the device.");
        }

        /// <summary>
        /// Starts the TCP listener in a separate thread.
        /// </summary>
        public void Start()
        {
            _isRunning = true;
            _listenerThread = new Thread(StartTcpListening);
            _listenerThread.Start();
        }

        /// <summary>
        /// Stops the TCP listener gracefully.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            // Optionally wait for the listener thread to finish.
            if (_listenerThread != null)
            {
                _listenerThread.Join();
            }
        }

        /// <summary>
        /// Main method for listening to incoming connections.
        /// </summary>
        private void StartTcpListening()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, TcpPort);
            listener.Server.ReceiveTimeout = Timeout;
            listener.Server.SendTimeout = Timeout;
            listener.Start(2);

            while (_isRunning)
            {
                LogHelper.LogInformation("Waiting for an incoming connection on " + _networkInterface.IPv4Address + " port " + TcpPort);
                try
                {
                    // Accept a client connection (synchronously).
                    TcpClient client = listener.AcceptTcpClient();
                    IPEndPoint remoteIpEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                    string clientIp = remoteIpEndPoint != null ? remoteIpEndPoint.Address.ToString() : "Unknown";
                    LogHelper.LogInformation("Client connected on port " + TcpPort + " from " + clientIp);

                    using (client)
                    {
                        using (NetworkStream stream = client.GetStream())
                        {
                            // Use default constructors for StreamReader/StreamWriter.
                            using (StreamReader streamReader = new StreamReader(stream))
                            {
                                using (StreamWriter sw = new StreamWriter(stream))
                                {
                                    // Authenticate the client with limited attempts.
                                    if (!this.AuthenticateClient(streamReader, sw, clientIp))
                                    {
                                        client.Close();
                                        continue;
                                    }
                                    // Send a welcome message.
                                    this.SendWelcomeMessage(sw, clientIp);
                                    // Process client commands.
                                    this.ProcessClientCommands(streamReader, sw, clientIp);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogError("Exception occurred while processing client request: " + ex.Message);
                }
            }
            listener.Stop();
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
                    this.WriteInline(sw, LogMessages.TimeStamp + " login as: ");
                    string usernameInput = streamReader.ReadLine();
                    this.WriteInline(sw, LogMessages.TimeStamp + " " + usernameInput + "@" + clientIp + " password: ");
                    string passwordInput = streamReader.ReadLine();

                    if (usernameInput == ClientUsername && passwordInput == ClientPassword)
                    {
                        return true;
                    }
                    else
                    {
                        this.WriteToStream(sw, "Login incorrect");
                        LogHelper.LogError("Login incorrect for " + clientIp);
                        // Delay to slow down brute force attempts.
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogError("Authentication error: " + ex.Message);
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
                _mqttClient.MqttClient.Publish("home/" + DeviceName + "/temperature", Encoding.UTF8.GetBytes(_sensorService.GetTemp().ToString()));
                return false;
            }));
            commands.Add("status", new CommandHandler((args, writer) =>
            {
                this.WriteToStream(writer, "Temp: " + _sensorService.GetTemp() + " C, Humidity: " + _sensorService.GetHumidity() + " %, Uptime: " + _uptimeService.GetUptime());
                return false;
            }));
            commands.Add("publishuptime", new CommandHandler((args, writer) =>
            {
                _mqttClient.MqttClient.Publish("home/" + DeviceName + "/uptime", Encoding.UTF8.GetBytes(_uptimeService.GetUptime()));
                return false;
            }));
            commands.Add("getipaddress", new CommandHandler((args, writer) => { this.WriteToStream(writer, _networkInterface.IPv4Address); return false; }));
            commands.Add("help", new CommandHandler((args, writer) => { this.DisplayHelp(writer); return false; }));
            commands.Add("info", new CommandHandler((args, writer) =>
            {
                Version firmwareVersion = SystemInfo.Version;
                string versionString = $"{firmwareVersion.Major}.{firmwareVersion.Minor}.{firmwareVersion.Build}.{firmwareVersion.Revision}";
                string info = "Device: " + DeviceName + "\r\n" +
                              "Firmware Version: " + versionString + "\r\n" +
                              "IP: " + _networkInterface.IPv4Address + "\r\n" +
                              "Uptime: " + _uptimeService.GetUptime() + "\r\n" +
                              "Sensor Interval: " + _sensorInterval + " ms";
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
                string input = streamReader.ReadLine();
                if (input == null)
                    break;

                LogHelper.LogInformation("Command received: " + input);

                string[] parts = input.Trim().Split(' ');
                if (parts.Length == 0)
                {
                    this.WritePrompt(sw, clientIp);
                    continue;
                }

                string commandKey = parts[0].ToLower();
                bool shouldExit = false;

                if (commandHandlers.Contains(commandKey))
                {
                    CommandHandler handler = (CommandHandler)commandHandlers[commandKey];
                    shouldExit = handler(parts, sw);
                }
                else
                {
                    this.WriteToStream(sw, "Unrecognized command");
                }

                if (shouldExit)
                    break;

                this.WritePrompt(sw, clientIp);
            }
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
            writer.WriteLine(text);
            writer.Flush();
        }

        /// <summary>
        /// Writes text to the stream without a newline and flushes.
        /// </summary>
        private void WriteInline(StreamWriter writer, string text)
        {
            writer.Write(text);
            writer.Flush();
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
                "* IP Address: " + _networkInterface.IPv4Address + "\r\n" +
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
                " - setInterval <ms>: Sets sensor read interval.\r\n" +
                " - relay on|off|status : Controls the relay.\r\n" +
                " - diagnostic      : Displays diagnostic info (free memory).\r\n" +
                " - exit            : Exits the session.\r\n" +
                " - reboot          : Reboots the device.\r\n" +
                "\r\n" +
                "Type a command:";
            this.WriteToStream(sw, welcomeMessage);
        }
    }
}
