namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Net.Sockets;
    using System.Threading;

    using Contracts;

    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Services.MQTT;
    using ESP32_NF_MQTT_DHT.Services.MQTT.Contracts;

    using nanoFramework.Hardware.Esp32;
    using nanoFramework.M2Mqtt;
    using nanoFramework.M2Mqtt.Messages;
    using nanoFramework.Runtime.Native;

    using static Settings.MqttSettings;

    /// <summary>
    /// Service that manages MQTT client functionalities, including connecting to the broker,
    /// handling messages, managing sensor data, and reconnecting in case of errors.
    /// </summary>
    internal class MqttClientService : IMqttClientService
    {
        private const int MaxReconnectAttempts = 20;
        private const int ReconnectDelay = 10000;
        private const int SensorDataInterval = 300000;
        private const int MaxAttempts = 1000;
        private const int MaxReconnectDelay = 120000;
        private const int ErrorInterval = 10000;

        private readonly IConnectionService _connectionService;
        private readonly IInternetConnectionService _internetConnectionService;
        private readonly MqttMessageHandler _mqttMessageHandler;
        private readonly IMqttPublishService _mqttPublishService;

        private readonly ManualResetEvent _stopSignal = new ManualResetEvent(false);
        private readonly object _threadLock = new object();

        private bool _isRunning = true;
        private bool _isConnecting = false;
        private bool _isHeartbeatRunning = false;

        private Thread _connectionThread;

        private readonly MqttConnectionManager _connectionManager;
        private readonly SensorDataPublisher _sensorDataPublisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientService"/> class.
        /// </summary>
        public MqttClientService(
            IConnectionService connectionService,
            IInternetConnectionService internetConnectionService,
            MqttMessageHandler mqttMessageHandler,
            IMqttPublishService mqttPublishService)
        {
            _connectionService = connectionService;
            _internetConnectionService = internetConnectionService ?? throw new ArgumentNullException(nameof(internetConnectionService));
            _mqttMessageHandler = mqttMessageHandler;
            _mqttPublishService = mqttPublishService;

            _connectionManager = new MqttConnectionManager();
            _sensorDataPublisher = new SensorDataPublisher(this.SensorDataTimerCallback);

            _internetConnectionService.InternetLost += this.OnInternetLost;
            _internetConnectionService.InternetRestored += this.OnInternetRestored;
        }

        /// <summary>
        /// Gets the current instance of the MQTT client.
        /// </summary>
        public MqttClient MqttClient => _connectionManager.MqttClient;

        /// <summary>
        /// Starts the MQTT client service by establishing a connection to the MQTT broker.
        /// </summary>
        public void Start()
        {
            if (_internetConnectionService.IsInternetAvailable())
            {
                _connectionThread = new Thread(this.EstablishBrokerConnection);
                _connectionThread.Start();
            }
        }

        /// <summary>
        /// Establishes the connection to the MQTT broker, retrying if necessary.
        /// </summary>
        private void EstablishBrokerConnection()
        {
            lock (_threadLock)
            {
                if (_isConnecting)
                {
                    LogHelper.LogInformation("Already connecting.");
                    return;
                }
                _isConnecting = true;
            }

            try
            {
                _isRunning = true;
                int attemptCount = 0;
                int delayBetweenAttempts = 5000;
                Random random = new Random();

                _connectionService.CheckConnection();

                while (_isRunning && attemptCount < MaxAttempts)
                {
                    if (!_internetConnectionService.IsInternetAvailable())
                    {
                        LogHelper.LogWarning("No internet, pausing attempts.");
                        _stopSignal.WaitOne(30000, false);
                        continue;
                    }

                    if (this.AttemptBrokerConnection())
                    {
                        LogHelper.LogInformation("Connected, starting sensor timer.");
                        _sensorDataPublisher.Start(SensorDataInterval);
                        return;
                    }

                    attemptCount++;

                    if (attemptCount % 5 == 0)
                    {
                        LogHelper.LogInformation($"Attempt {attemptCount}/{MaxAttempts}, retry in {delayBetweenAttempts / 1000}s");
                    }

                    int jitter = random.Next() % 1500 + 500;
                    _stopSignal.WaitOne(delayBetweenAttempts + jitter, false);

                    delayBetweenAttempts = Math.Min(delayBetweenAttempts * 3 / 2, MaxReconnectDelay);
                }

                LogHelper.LogWarning("Max attempts reached, entering deep sleep to conserve power.");

                this.DisposeMqttClient();
                _sensorDataPublisher.Stop();

                _stopSignal.WaitOne(2000, false);

                TimeSpan deepSleepDuration = new TimeSpan(0, 5, 0); // 0 часа, 5 минути, 0 секунди

                Sleep.EnableWakeupByTimer(deepSleepDuration);
                Sleep.StartDeepSleep();
            }
            finally
            {
                _isConnecting = false;
            }
        }

        /// <summary>
        /// Attempts to connect to the MQTT broker. Checks the internet connection,
        /// and handles exceptions in the connection process.
        /// </summary>
        private bool AttemptBrokerConnection()
        {
            if (!_internetConnectionService.IsInternetAvailable())
            {
                return false;
            }

            this.DisposeMqttClient();

            try
            {
                bool isConnected = _connectionManager.Connect(Broker, ClientId, ClientUsername, ClientPassword);

                if (isConnected && this.MqttClient.IsConnected)
                {
                    this.InitializeMqttClient();
                    return true;
                }
            }
            catch (SocketException ex)
            {
                LogHelper.LogError($"Socket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"MQTT connect error: {ex.Message}");
            }

            this.DisposeMqttClient();
            return false;
        }

        /// <summary>
        /// Initializes the MQTT client by subscribing to topics and setting event handlers.
        /// </summary>
        private void InitializeMqttClient()
        {
            this.MqttClient.ConnectionClosed += this.ConnectionClosed;
            this.MqttClient.Subscribe(new[] { "#" }, new[] { MqttQoSLevel.AtLeastOnce });

            this.MqttClient.MqttMsgPublishReceived += _mqttMessageHandler.HandleIncomingMessage;

            _mqttMessageHandler.SetMqttClient(this.MqttClient);
            _mqttPublishService.SetMqttClient(this.MqttClient);

            if (!_isHeartbeatRunning)
            {
                _mqttPublishService.StartHeartbeat();
                _isHeartbeatRunning = true;
            }

            LogHelper.LogInformation("MQTT client setup complete");
        }

        /// <summary>
        /// Timer callback method that publishes sensor data.
        /// </summary>
        private void SensorDataTimerCallback(object state)
        {
            try
            {
                _mqttPublishService.PublishSensorData();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"SensorDataTimer Exception: {ex.Message}");
                _mqttPublishService.PublishError($"SensorDataTimer Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles reconnection logic when the connection to the MQTT broker is lost.
        /// </summary>
        private void ConnectionClosed(object sender, EventArgs e)
        {
            LogHelper.LogWarning("Lost connection to MQTT broker, attempting to reconnect...");

            this.DisposeMqttClient();
            _sensorDataPublisher.Stop();

            _connectionService.CheckConnection();

            if (!_connectionService.IsConnectionInProgress)
            {
                if (_internetConnectionService.IsInternetAvailable())
                {
                    this.EstablishBrokerConnection();
                }
                else
                {
                    LogHelper.LogInformation("Internet check thread is running, waiting for it to finish...");
                }
            }
        }

        /// <summary>
        /// Starts the MQTT client service when the internet connection is restored.
        /// </summary>
        private void OnInternetRestored(object sender, EventArgs e)
        {
            this.Start();
        }

        /// <summary>
        /// Stops the sensor data timer when the internet connection is lost.
        /// </summary>
        private void OnInternetLost(object sender, EventArgs e)
        {
            this.DisposeMqttClient();
            _sensorDataPublisher.Stop();
        }

        /// <summary>
        /// Disposes of the current MQTT client and ensures disconnection.
        /// </summary>
        private void DisposeMqttClient()
        {
            if (this.MqttClient != null)
            {
                try
                {
                    if (this.MqttClient.IsConnected)
                    {
                        this.MqttClient.Disconnect();
                    }

                    this.MqttClient.Dispose();
                    LogHelper.LogInformation("Disposing current MQTT client...");
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"Error while disposing MQTT client: {ex.Message}");
                }
                finally
                {
                    _isHeartbeatRunning = false;
                    _mqttPublishService.StopHeartbeat();
                    _connectionManager.Disconnect();
                }
            }
        }

        /// <summary>
        /// Stops the MQTT client service.
        /// </summary>
        private void Stop()
        {
            _isRunning = false;
            _sensorDataPublisher.Stop();

            if (MqttClient != null && MqttClient.IsConnected)
            {
                MqttClient.Disconnect();
            }

            MqttClient?.Dispose();
            _stopSignal.Set();
        }
    }
}
