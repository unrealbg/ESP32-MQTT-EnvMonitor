namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Device.Gpio;
    using System.Diagnostics;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    using Constants;

    using Microsoft.Extensions.Logging;

    using nanoFramework.M2Mqtt;
    using nanoFramework.M2Mqtt.Exceptions;
    using nanoFramework.M2Mqtt.Messages;
    using nanoFramework.Runtime.Native;

    using Services.Contracts;

    using IMqttClient = Contracts.IMqttClient;

    internal class MqttClientService : IMqttClient
    {
        private readonly string uptimeTopic = $"home/{Constants.DEVICE}/uptime";
        private readonly string relayTopic = $"home/{Constants.DEVICE}/switch/relay";

        private static GpioController _gpioController;
        private readonly IUptimeService _uptimeService;
        private readonly IConnectionService _connectionService;
        private readonly ILogger _logger;

        public MqttClientService(IUptimeService uptimeService, IConnectionService connectionService, ILoggerFactory loggerFactory)
        {
            _uptimeService = uptimeService;
            _connectionService = connectionService;
            _logger = loggerFactory?.CreateLogger(nameof(MqttClientService)) ?? throw new ArgumentNullException(nameof(loggerFactory));
            _gpioController = new GpioController();
        }

        public GpioPin RelayPin { get; private set; }
        public MqttClient MqttClient { get; private set; }

        public void Start()
        {
            InitializeRelayPin();
            ConnectToBroker();
        }

        private void InitializeRelayPin()
        {
            RelayPin = _gpioController.OpenPin(25, PinMode.Output);
        }

        private void ConnectToBroker()
        {
            int attemptCount = 0;
            while (true)
            {
                try
                {
                    _logger.LogInformation($"[c] Attempting to connect to MQTT broker: {Constants.BROKER} [Attempt: {++attemptCount}]");
                    MqttClient = new MqttClient(Constants.BROKER);
                    MqttClient.Connect(Constants.CLIENT_ID, Constants.MQTT_CLIENT_USERNAME, Constants.MQTT_CLIENT_PASSWORD);

                    if (MqttClient.IsConnected)
                    {
                        MqttClient.ConnectionClosed += ConnectionClosed;
                        MqttClient.Subscribe(new[] { "#" }, new[] { MqttQoSLevel.AtLeastOnce });
                        MqttClient.MqttMsgPublishReceived += HandleIncomingMessage;
                        _logger.LogInformation("[+] Connected to MQTT broker.");
                        Thread uptimeThread = new Thread(UptimeLoop);
                        uptimeThread.Start();
                        break;
                    }

                    Thread.Sleep(2000);
                }
                catch (MqttCommunicationException)
                {
                    _logger.LogError("[ex] MQTT Communication ERROR");
                    Thread.Sleep(5000);
                }
                catch (SocketException)
                {
                    _logger.LogError("[ex] Communication ERROR");
                    Thread.Sleep(5000);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[ex] ERROR: {ex.Message}");

                    if (attemptCount > 20)
                    {
                        Power.RebootDevice();
                    }

                    Thread.Sleep(10000);
                }
            }
        }

        private void ConnectionClosed(object sender, EventArgs e)
        {
            _logger.LogWarning("[-] Lost connection to MQTT broker, attempting to reconnect...");
            Thread.Sleep(5000);
            _connectionService.Connect();
            ConnectToBroker();
        }

        private void UptimeLoop()
        {
            while (true)
            {
                try
                {
                    string uptimeMessage = _uptimeService.GetUptime();
                    MqttClient.Publish(uptimeTopic, Encoding.UTF8.GetBytes(uptimeMessage));
                    _logger.LogInformation(uptimeMessage);
                    Thread.Sleep(60000); // 1 minute
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[e] ERROR: {ex.Message}");
                    Thread.Sleep(2000);
                    // optional
                    // Power.RebootDevice();
                }
            }
        }

        private void HandleIncomingMessage(object sender, MqttMsgPublishEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);
            if (e.Topic == relayTopic)
            {
                if (message.Contains("on"))
                {
                    RelayPin.Write(PinValue.High);
                    _logger.LogInformation("Relay turned ON");
                }
                else if (message.Contains("off"))
                {
                    RelayPin.Write(PinValue.Low);
                    _logger.LogInformation("Relay turned OFF");
                }
            }
        }
    }
}
