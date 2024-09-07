namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Threading;

    using Contracts;

    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Services.MQTT;
    using ESP32_NF_MQTT_DHT.Services.MQTT.Contracts;

    using Microsoft.Extensions.Logging;

    using nanoFramework.M2Mqtt;
    using nanoFramework.M2Mqtt.Messages;
    using nanoFramework.Runtime.Native;

    using static Settings.MqttSettings;

    using IMqttClientService = Contracts.IMqttClientService;

    /// <summary>
    /// Service to handle MQTT client functionalities including connecting to the broker,
    /// handling messages, and managing a relay pin.
    /// </summary>
    internal class MqttClientService : IMqttClientService
    {
        private const int MaxReconnectAttempts = 20;
        private const int ReconnectDelay = 10000;
        private const int MaxAttempts = 1000;
        private const int MaxReconnectDelay = 120000;
        private const int ErrorInterval = 10000;

        private readonly IConnectionService _connectionService;
        private readonly ISensorService _sensorService;
        private readonly IInternetConnectionService _internetConnectionService;
        private readonly MqttMessageHandler _mqttMessageHandler;
        private readonly IMqttPublishService _mqttPublishService;

        private readonly LogHelper _logHelper;
        private readonly ManualResetEvent _stopSignal = new ManualResetEvent(false);

        private int _attemptCount = 1;
        private bool _isRunning = true;

        private bool _isConnected = true;

        private Thread _connectionThread;

        private Thread _sensorDataThread;
        private bool _isSensorDataThreadRunning = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientService"/> class.
        /// </summary>
        /// <param name="connectionService">Service to manage network connections.</param>
        /// <param name="loggerFactory">Factory to create a logger for this service.</param>
        /// <param name="sensorService"> Service to read data from the sensor.</param>
        /// <exception cref="ArgumentNullException">Thrown if loggerFactory is null.</exception>
        public MqttClientService(IConnectionService connectionService,
                                 ILoggerFactory loggerFactory,
                                 ISensorService sensorService,
                                 IInternetConnectionService internetConnectionService,
                                 MqttMessageHandler mqttMessageHandler,
                                 IMqttPublishService mqttPublishService)
        {
            _connectionService = connectionService;
            _logHelper = new LogHelper(loggerFactory, nameof(MqttClientService));
            _sensorService = sensorService ?? throw new ArgumentNullException(nameof(sensorService));
            _internetConnectionService = internetConnectionService ?? throw new ArgumentNullException(nameof(internetConnectionService));
            _mqttMessageHandler = mqttMessageHandler;
            _mqttPublishService = mqttPublishService;

            _internetConnectionService.InternetLost += OnInternetLost;
            _internetConnectionService.InternetRestored += OnInternetRestored;
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
            if (_internetConnectionService.IsInternetAvailable())
            {
                _connectionThread = new Thread(this.ConnectToBroker);
                _connectionThread.Start();
            }
        }

        /// <summary>
        /// Connects to the MQTT broker.
        /// </summary>
        public void ConnectToBroker()
        {
            _isRunning = true;
            int attemptCount = 0;
            int delayBetweenAttempts = 5000;
            Random random = new Random();

            _connectionService.CheckConnection();

            while (_isRunning && attemptCount < MaxAttempts)
            {
                if (this.TryConnectToBroker())
                {
                    _logHelper.LogWithTimestamp(LogLevel.Information, "Starting sensor data thread...");
                    this.StartSensorDataThread();
                    return;
                }

                attemptCount++;
                _logHelper.LogWithTimestamp(LogLevel.Warning, $"Attempt {attemptCount} failed. Retrying in {delayBetweenAttempts / MaxAttempts} seconds...");

                int randomValue = random.Next() % 4000 + 1000;
                _stopSignal.WaitOne(delayBetweenAttempts + randomValue, false);

                delayBetweenAttempts = Math.Min(delayBetweenAttempts * 2, MaxReconnectDelay);
            }

            this._logHelper.LogWithTimestamp(LogLevel.Warning, "Max reconnect attempts reached. Rebooting device...");
            Power.RebootDevice();
        }

        private void StartSensorDataThread()
        {
            if (_isSensorDataThreadRunning || (_sensorDataThread != null && _sensorDataThread.IsAlive))
            {
                _logHelper.LogWithTimestamp(LogLevel.Warning, "Sensor data thread is already running.");
                return;
            }

            _sensorDataThread = new Thread(this.SensorDataLoop);
            _isSensorDataThreadRunning = true;
            _sensorDataThread.Start();
        }

        private void ConnectionClosed(object sender, EventArgs e)
        {
            _logHelper.LogWithTimestamp(LogLevel.Warning, "Lost connection to MQTT broker, attempting to reconnect...");

            if (_isSensorDataThreadRunning)
            {
                _isSensorDataThreadRunning = false;
            }

            if (!this._internetConnectionService.IsInternetThreadRunning)
            {
                this.ConnectToBroker();
            }
            else
            {
                _logHelper.LogWithTimestamp(LogLevel.Warning, "Internet check thread is running, waiting for it to finish...");
            }
        }

        private void OnInternetRestored(object sender, EventArgs e)
        {
            this.Start();
        }

        private void OnInternetLost(object sender, EventArgs e)
        {
            if (_sensorDataThread != null && _sensorDataThread.IsAlive)
            {
                _isSensorDataThreadRunning = false;
            }
        }

        private bool TryConnectToBroker()
        {
            if (!_internetConnectionService.IsInternetAvailable())
            {
                _logHelper.LogWithTimestamp(LogLevel.Warning, "No internet connection, cannot connect to MQTT broker.");
                return false;
            }

            this.DisposeCurrentClient();

            try
            {
                _logHelper.LogWithTimestamp(LogLevel.Information, $"Attempting to connect to MQTT broker: {Broker}");
                this.MqttClient = new MqttClient(Broker);
                this.MqttClient.Connect(ClientId, ClientUsername, ClientPassword);

                _mqttMessageHandler.SetMqttClient(this.MqttClient);
                _mqttPublishService.SetMqttClient(this.MqttClient);

                if (MqttClient.IsConnected)
                {
                    this.SetupMqttClient();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogWithTimestamp(LogLevel.Error, $"MQTT connection failed: {ex.Message}");
            }

            return false;
        }

        private void DisposeCurrentClient()
        {
            if (this.MqttClient != null)
            {
                if (this.MqttClient.IsConnected)
                {
                    _logHelper.LogWithTimestamp(LogLevel.Information, "Disposing current MQTT client...");
                    this.MqttClient.Disconnect();
                }

                this.MqttClient.Dispose();
                this.MqttClient = null;
            }
        }

        private void SetupMqttClient()
        {
            this.MqttClient.ConnectionClosed += this.ConnectionClosed;
            this.MqttClient.Subscribe(new[] { "#" }, new[] { MqttQoSLevel.AtLeastOnce });
            this.MqttClient.MqttMsgPublishReceived += _mqttMessageHandler.HandleIncomingMessage;
            _logHelper.LogWithTimestamp(LogLevel.Information, "MQTT client setup complete");
        }

        private void SensorDataLoop()
        {
            while (_isRunning && _isSensorDataThreadRunning)
            {
                try
                {
                    this._mqttPublishService.PublishSensorData();
                }
                catch (Exception ex)
                {
                    _logHelper.LogWithTimestamp(LogLevel.Error, $"SensorDataLoop Exception: {ex.Message}");
                    _mqttPublishService.PublishError($"SensorDataLoop Exception: {ex.Message}");
                }

                _stopSignal.WaitOne(ErrorInterval, false);
            }
        }

        private void Stop()
        {
            _isRunning = false;
            _isSensorDataThreadRunning = false;

            if (_sensorDataThread != null && _sensorDataThread.IsAlive)
            {
                _sensorDataThread.Join();
            }

            if (this.MqttClient != null && this.MqttClient.IsConnected)
            {
                this.MqttClient.Disconnect();
            }

            this.MqttClient?.Dispose();
            _stopSignal.Set();
        }
    }
}
