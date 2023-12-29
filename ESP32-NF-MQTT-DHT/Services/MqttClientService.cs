namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Text;
    using System.Threading;

    using Microsoft.Extensions.Logging;

    using nanoFramework.M2Mqtt;
    using nanoFramework.M2Mqtt.Messages;
    using nanoFramework.Runtime.Native;
    using nanoFramework.Json;

    using Contracts;
    using Models;

    using static Settings.DeviceSettings;
    using static Settings.MqttSettings;
    using static Helpers.TimeHelper;

    using IMqttClientService = Contracts.IMqttClientService;

    /// <summary>
    /// Service to handle MQTT client functionalities including connecting to the broker,
    /// handling messages, and managing a relay pin.
    /// </summary>
    internal class MqttClientService : IMqttClientService
    {
        private const int MaxReconnectAttempts = 20;
        private const int ReconnectDelay = 10000;
        private const int ErrorInterval = 10000;

        private static readonly string UptimeTopic = $"home/{DeviceName}/uptime";
        private static readonly string RelayTopic = $"home/{DeviceName}/switch";
        private static readonly string SystemTopic = $"home/{DeviceName}/system";
        private static readonly string DataTopic = $"home/{DeviceName}/messages";
        private static readonly string ErrorTopic = $"home/{DeviceName}/errors";

        private int _attemptCount = 1;
        private bool _isRunning = true;

        private readonly IUptimeService _uptimeService;
        private readonly IConnectionService _connectionService;
        private readonly IDhtService _dhtService;
        private readonly IAhtSensorService _ahtSensorService;
        private readonly ILogger _logger;
        private readonly IRelayService _relayService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientService"/> class.
        /// </summary>
        /// <param name="uptimeService">Service to get uptime information.</param>
        /// <param name="connectionService">Service to manage network connections.</param>
        /// <param name="loggerFactory">Factory to create a logger for this service.</param>
        /// <param name="relayService">Service to control and manage the relay operations.</param>
        /// <param name="dhtService"> Service to read data from the DHT sensor.</param>
        /// <param name="ahtSensorService"> Service to read data from the AHT sensor.</param>
        /// <exception cref="ArgumentNullException">Thrown if loggerFactory is null.</exception>
        public MqttClientService(IUptimeService uptimeService, IConnectionService connectionService, ILoggerFactory loggerFactory, IRelayService relayService, IDhtService dhtService, IAhtSensorService ahtSensorService)
        {
            _uptimeService = uptimeService;
            _connectionService = connectionService;
            _relayService = relayService ?? throw new ArgumentNullException(nameof(relayService));
            _logger = loggerFactory?.CreateLogger(nameof(MqttClientService)) ?? throw new ArgumentNullException(nameof(loggerFactory));
            _dhtService = dhtService ?? throw new ArgumentNullException(nameof(dhtService));
            _ahtSensorService = ahtSensorService ?? throw new ArgumentNullException(nameof(ahtSensorService));
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
            while (_isRunning)
            {
                try
                {
                    _logger.LogInformation($"[{GetCurrentTimestamp()}] Attempting to connect to MQTT broker: {Broker} [Attempt: {_attemptCount}]");
                    this.MqttClient = new MqttClient(Broker);
                    this.MqttClient.Connect(ClientId, ClientUsername, ClientPassword);

                    if (MqttClient.IsConnected)
                    {
                        this.MqttClient.ConnectionClosed += ConnectionClosed;
                        this.MqttClient.Subscribe(new[] { "#" }, new[] { MqttQoSLevel.AtLeastOnce });
                        this.MqttClient.MqttMsgPublishReceived += HandleIncomingMessage;
                        _logger.LogInformation($"[{GetCurrentTimestamp()}] Connected to MQTT broker.");

                        Thread sensorDataThread = new Thread(this.SensorDataLoop);
                        sensorDataThread.Start();

                        break;
                    }

                    Thread.Sleep(ReconnectDelay);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[{GetCurrentTimestamp()}] ERROR: {ex.Message}");
                    this.HandleReconnection();
                    _attemptCount++;
                }
            }
        }

        private void ConnectionClosed(object sender, EventArgs e)
        {
            _logger.LogWarning($"[{GetCurrentTimestamp()}] Lost connection to MQTT broker, attempting to reconnect...");
            Thread reconnectThread = new Thread(() =>
            {
                Thread.Sleep(ReconnectDelay);
                _connectionService.Connect();
                this.ConnectToBroker();
            });

            reconnectThread.Start();
        }

        private void HandleIncomingMessage(object sender, MqttMsgPublishEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);

            if (e.Topic == RelayTopic)
            {
                if (message.Contains("on"))
                {
                    _relayService.TurnOn();
                    this.MqttClient.Publish(RelayTopic + "/relay", Encoding.UTF8.GetBytes("ON"));
                }
                else if (message.Contains("off"))
                {
                    _relayService.TurnOff();
                    this.MqttClient.Publish(RelayTopic + "/relay", Encoding.UTF8.GetBytes("OFF"));
                }
            }
            else if (e.Topic == SystemTopic)
            {
                if (message.Contains("uptime"))
                {
                    this.MqttClient.Publish(UptimeTopic, Encoding.UTF8.GetBytes(this._uptimeService.GetUptime()));
                }
                else if (message.Contains("reboot"))
                {
                    this.MqttClient.Publish($"home/{DeviceName}/maintenance", Encoding.UTF8.GetBytes($"Manual reboot at: {DateTime.UtcNow.ToString("HH:mm:ss")}"));
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

        private void SensorDataLoop()
        {
            while (_isRunning)
            {
                try
                {
                    double[] data;

                    data = _dhtService.GetData();
                    //data = _ahtSensorService.GetData();

                    if (this.IsSensorDataValid(data))
                    {
                        this.PublishValidSensorData(data);
                        Thread.Sleep(300000);
                    }
                    else
                    {
                        this.PublishError($"[{GetCurrentTimestamp()}] ERROR:  Unable to read sensor data");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    this.PublishError(ex.Message);
                }
            }
        }

        private void PublishError(string errorMessage)
        {
            this.MqttClient.Publish(ErrorTopic, Encoding.UTF8.GetBytes(errorMessage));
            Thread.Sleep(ErrorInterval);
        }

        private bool IsSensorDataValid(double[] data)
        {
            return !(data[0] == -50 || data[1] == -100);
        }

        private void PublishValidSensorData(double[] data)
        {
            var sensorData = this.CreateSensorData(data);
            var message = JsonSerializer.SerializeObject(sensorData);
            this.MqttClient.Publish(DataTopic, Encoding.UTF8.GetBytes(message));
        }

        private Sensor CreateSensorData(double[] data)
        {
            var temp = data[0];
            temp = (int)((temp * 100) + 0.5) / 100.0;

            return new Sensor
            {
                Data = new Data
                {
                    Date = DateTime.UtcNow.Date.ToString("dd/MM/yyyy"),
                    Time = GetCurrentTimestamp(),
                    Temp = temp,
                    Humid = (int)data[1]
                }
            };
        }

        private void Stop()
        {
            _isRunning = false;
        }
    }
}
