namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Device.Gpio;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    using Microsoft.Extensions.Logging;

    using nanoFramework.M2Mqtt;
    using nanoFramework.M2Mqtt.Exceptions;
    using nanoFramework.M2Mqtt.Messages;
    using nanoFramework.Runtime.Native;

    using Contracts;

    using static Constants.Constants;

    using IMqttClientService = Contracts.IMqttClientService;

    /// <summary>
    /// Service to handle MQTT client functionalities including connecting to the broker,
    /// handling messages, and managing a relay pin.
    /// </summary>
    internal class MqttClientService : IMqttClientService
    {
        private readonly string _uptimeTopic = $"home/{Device}/uptime";
        private readonly string _relayTopic = $"home/{Device}/switch";
        private readonly string _systemTopic = $"home/{Device}/system";
        
        private const int MaxReconnectAttempts = 20;
        private const int ReconnectDelay = 10000;
        private const int ErrorDelay = 15000;
        private const int UptimeDelay = 60000;

        private int _attemptCount = 1;

        private static GpioController _gpioController;
        private readonly IUptimeService _uptimeService;
        private readonly IConnectionService _connectionService;
        private readonly ILogger _logger;
        private readonly IRelayService _relayService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientService"/> class.
        /// </summary>
        /// <param name="uptimeService">Service to get uptime information.</param>
        /// <param name="connectionService">Service to manage network connections.</param>
        /// <param name="loggerFactory">Factory to create a logger for this service.</param>
        /// <param name="relayService">Service to control and manage the relay operations.</param>
        /// <exception cref="ArgumentNullException">Thrown if loggerFactory is null.</exception>
        public MqttClientService(IUptimeService uptimeService, IConnectionService connectionService, ILoggerFactory loggerFactory, IRelayService relayService)
        {
            _uptimeService = uptimeService;
            _connectionService = connectionService;
            _relayService = relayService ?? throw new ArgumentNullException(nameof(relayService));
            _logger = loggerFactory?.CreateLogger(nameof(MqttClientService)) ?? throw new ArgumentNullException(nameof(loggerFactory));
            _gpioController = new GpioController();
        }

        /// <summary>
        /// Gets the MQTT client instance.
        /// </summary>
        public MqttClient MqttClient { get; private set; }

        /// <summary>
        /// Starts the MQTT client service, initializing the relay pin and connecting to the MQTT broker.
        /// </summary>
        public void Start()
        {
            Thread connectionThread = new Thread(ConnectToBroker);
            connectionThread.Start();
        }

        private void ConnectToBroker()
        {
            while (true)
            {
                try
                {
                    _logger.LogInformation($"[c] Attempting to connect to MQTT broker: {Broker} [Attempt: {_attemptCount}]");
                    this.MqttClient = new MqttClient(Broker);
                    this.MqttClient.Connect(ClientId, MqttClientUsername, MqttClientPassword);

                    if (MqttClient.IsConnected)
                    {
                        this.MqttClient.ConnectionClosed += ConnectionClosed;
                        this.MqttClient.Subscribe(new[] { "#" }, new[] { MqttQoSLevel.AtLeastOnce });
                        this.MqttClient.MqttMsgPublishReceived += HandleIncomingMessage;
                        _logger.LogInformation("[+] Connected to MQTT broker.");
                        Thread uptimeThread = new Thread(UptimeLoop);
                        uptimeThread.Start();
                        break;
                    }

                    Thread.Sleep(ReconnectDelay);
                }
                catch (SocketException ex)
                {
                    _logger.LogError("[ex] Socket ERROR: " + ex.Message);
                    HandleReconnection();
                    _attemptCount++;
                }
                catch (MqttCommunicationException ex)
                {
                    _logger.LogError("[ex] MQTT Communication ERROR: " + ex.Message);
                    HandleReconnection();
                    _attemptCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError("[ex] ERROR: " + ex.Message);
                    HandleReconnection();
                    _attemptCount++;
                }
            }
        }

        private void ConnectionClosed(object sender, EventArgs e)
        {
            _logger.LogWarning("[-] Lost connection to MQTT broker, attempting to reconnect...");
            Thread reconnectThread = new Thread(() =>
            {
                Thread.Sleep(ReconnectDelay);
                _connectionService.Connect();
                ConnectToBroker();
            });

            reconnectThread.Start();
        }

        private void UptimeLoop()
        {
            while (true)
            {
                try
                {
                    string uptimeMessage = _uptimeService.GetUptime();
                    this.MqttClient.Publish(_uptimeTopic, Encoding.UTF8.GetBytes(uptimeMessage));
                    _logger.LogInformation(uptimeMessage);
                    Thread.Sleep(UptimeDelay);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[e] ERROR: {ex.Message}");
                    Thread.Sleep(ErrorDelay);
                    // optional
                    // Power.RebootDevice();
                }
            }
        }

        private void HandleIncomingMessage(object sender, MqttMsgPublishEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);

            if (e.Topic == _relayTopic)
            {
                if (message.Contains("on"))
                {
                    _relayService.TurnOn();
                    this.MqttClient.Publish(_relayTopic + "/relay", Encoding.UTF8.GetBytes("ON"));
                }
                else if (message.Contains("off"))
                {
                    _relayService.TurnOff();
                    this.MqttClient.Publish(_relayTopic + "/relay", Encoding.UTF8.GetBytes("OFF"));
                }
            }
            else if (e.Topic == _systemTopic)
            {
                if (message.Contains("uptime"))
                {
                    this.MqttClient.Publish($"home/{Device}/uptime", Encoding.UTF8.GetBytes(this._uptimeService.GetUptime()));
                }
                else if (message.Contains("reboot"))
                {
                    this.MqttClient.Publish($"home/{Device}/maintenance", Encoding.UTF8.GetBytes($"Manual reboot at: {DateTime.UtcNow.ToString("HH:mm:ss")}"));
                    Thread.Sleep(2000);
                    Power.RebootDevice();
                }
            }
        }

        private void HandleReconnection()
        {
            if (_attemptCount > MaxReconnectAttempts)
            {
                Thread.Sleep(300000);
                //Power.RebootDevice();
                _attemptCount = 1;
            }

            Thread.Sleep(ReconnectDelay);
        }
    }
}
