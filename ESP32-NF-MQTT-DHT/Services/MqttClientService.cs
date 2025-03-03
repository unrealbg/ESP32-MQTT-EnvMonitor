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

    using static Settings.MqttSettings;

    /// <summary>
    /// Service that manages MQTT client functionalities, including connecting to the broker,
    /// handling messages, managing sensor data, and reconnecting in case of errors.
    /// </summary>
    internal class MqttClientService : IMqttClientService, IDisposable
    {
        private const int MAX_RECONNECT_ATTEMPTS = 20;
        private const int INITIAL_RECONNECT_DELAY = 5000;
        private const int MAX_RECONNECT_DELAY = 120000;
        private const int SENSOR_DATA_INTERVAL = 300000;
        private const int INTERNET_CHECK_INTERVAL = 30000;
        private const int DEEP_SLEEP_MINUTES = 5;
        private const int MAX_TOTAL_ATTEMPTS = 1000;
        private const int JITTER_BASE = 500;
        private const int JITTER_RANGE = 1500;

        private readonly IConnectionService _connectionService;
        private readonly IInternetConnectionService _internetConnectionService;
        private readonly MqttMessageHandler _mqttMessageHandler;
        private readonly IMqttPublishService _mqttPublishService;

        private readonly ManualResetEvent _stopSignal = new ManualResetEvent(false);
        private readonly object _connectionLock = new object();

        private bool _isRunning;
        private bool _isConnecting;
        private bool _isHeartbeatRunning;
        private bool _isDisposed;

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
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            _internetConnectionService = internetConnectionService ?? throw new ArgumentNullException(nameof(internetConnectionService));
            _mqttMessageHandler = mqttMessageHandler ?? throw new ArgumentNullException(nameof(mqttMessageHandler));
            _mqttPublishService = mqttPublishService ?? throw new ArgumentNullException(nameof(mqttPublishService));

            _connectionManager = new MqttConnectionManager();
            _sensorDataPublisher = new SensorDataPublisher(this.SensorDataTimerCallback);

            _internetConnectionService.InternetLost += this.OnInternetLost;
            _internetConnectionService.InternetRestored += this.OnInternetRestored;

            _isRunning = true;
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
            if (_isDisposed)
            {
                LogHelper.LogWarning("Cannot start disposed MQTT client service");
                return;
            }

            if (_connectionThread != null && _connectionThread.IsAlive)
            {
                LogHelper.LogInformation("Connection thread already running");
                return;
            }

            if (_internetConnectionService.IsInternetAvailable())
            {
                _connectionThread = new Thread(this.EstablishBrokerConnection);
                _connectionThread.Start();
            }
            else
            {
                LogHelper.LogWarning("Internet not available on startup. Waiting for internet restoration...");
            }
        }

        /// <summary>
        /// Stops the MQTT client service.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _sensorDataPublisher.Stop();

            this.SafeDisconnect();

            _stopSignal.Set();

            if (_connectionThread != null && _connectionThread.IsAlive)
            {
                _connectionThread.Join(1000);
            }
        }

        /// <summary>
        /// Disposes resources used by the service
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            this.Stop();

            _internetConnectionService.InternetLost -= this.OnInternetLost;
            _internetConnectionService.InternetRestored -= this.OnInternetRestored;

            _stopSignal.Set();

            _isDisposed = true;
        }

        /// <summary>
        /// Establishes the connection to the MQTT broker, retrying if necessary.
        /// </summary>
        private void EstablishBrokerConnection()
        {
            lock (_connectionLock)
            {
                if (_isConnecting)
                {
                    LogHelper.LogInformation("Already connecting to broker");
                    return;
                }

                _isConnecting = true;
            }

            try
            {
                int attemptCount = 0;
                int delayBetweenAttempts = INITIAL_RECONNECT_DELAY;
                Random random = new Random();

                _connectionService.CheckConnection();

                while (_isRunning && attemptCount < MAX_TOTAL_ATTEMPTS)
                {
                    if (!_internetConnectionService.IsInternetAvailable())
                    {
                        LogHelper.LogWarning("Internet not available, pausing connection attempts");
                        _stopSignal.WaitOne(INTERNET_CHECK_INTERVAL, false);
                        continue;
                    }

                    if (this.AttemptBrokerConnection())
                    {
                        LogHelper.LogInformation("Connected to MQTT broker. Starting sensor data publisher");
                        _sensorDataPublisher.Start(SENSOR_DATA_INTERVAL);
                        return;
                    }

                    attemptCount++;

                    if (attemptCount % 5 == 0)
                    {
                        LogHelper.LogInformation($"Attempt {attemptCount}/{MAX_TOTAL_ATTEMPTS}. Retry in {delayBetweenAttempts / 1000}s");
                    }

                    int jitter = random.Next(JITTER_RANGE) + JITTER_BASE;
                    _stopSignal.WaitOne(delayBetweenAttempts + jitter, false);

                    delayBetweenAttempts = Math.Min(delayBetweenAttempts * 3 / 2, MAX_RECONNECT_DELAY);
                }

                this.HandleMaxAttemptsReached();
            }
            finally
            {
                _isConnecting = false;
            }
        }

        /// <summary>
        /// Handles the case when maximum connection attempts are reached
        /// </summary>
        private void HandleMaxAttemptsReached()
        {
            LogHelper.LogWarning("Max connection attempts reached. Entering deep sleep to conserve power");

            this.SafeDisconnect();
            _sensorDataPublisher.Stop();

            _stopSignal.WaitOne(2000, false);

            TimeSpan deepSleepDuration = new TimeSpan(0, DEEP_SLEEP_MINUTES, 0);

            Sleep.EnableWakeupByTimer(deepSleepDuration);
            Sleep.StartDeepSleep();
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

            this.SafeDisconnect();

            try
            {
                bool isConnected = _connectionManager.Connect(Broker, ClientId, ClientUsername, ClientPassword);

                if (isConnected && MqttClient != null && MqttClient.IsConnected)
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

            this.SafeDisconnect();
            return false;
        }

        /// <summary>
        /// Initializes the MQTT client by subscribing to topics and setting event handlers.
        /// </summary>
        private void InitializeMqttClient()
        {
            if (MqttClient == null)
            {
                LogHelper.LogError("Cannot initialize null MQTT client");
                return;
            }

            MqttClient.ConnectionClosed += this.ConnectionClosed;
            MqttClient.Subscribe(new[] { "#" }, new[] { MqttQoSLevel.AtLeastOnce });
            MqttClient.MqttMsgPublishReceived += _mqttMessageHandler.HandleIncomingMessage;

            _mqttMessageHandler.SetMqttClient(MqttClient);
            _mqttPublishService.SetMqttClient(MqttClient);

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
            if (_isDisposed || !_isRunning)
                return;

            try
            {
                _mqttPublishService.PublishSensorData();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"SensorDataTimer Exception: {ex.Message}");

                try
                {
                    _mqttPublishService.PublishError($"SensorDataTimer Exception: {ex.Message}");
                }
                catch
                {
                    // Ignore errors when publishing errors
                }
            }
        }

        /// <summary>
        /// Handles reconnection logic when the connection to the MQTT broker is lost.
        /// </summary>
        private void ConnectionClosed(object sender, EventArgs e)
        {
            LogHelper.LogWarning("Lost connection to MQTT broker, attempting to reconnect...");

            this.SafeDisconnect();
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
            if (!_isDisposed && _isRunning)
            {
                this.Start();
            }
        }

        /// <summary>
        /// Stops the sensor data timer when the internet connection is lost.
        /// </summary>
        private void OnInternetLost(object sender, EventArgs e)
        {
            this.SafeDisconnect();
            _sensorDataPublisher.Stop();
        }

        /// <summary>
        /// Safely disconnects and disposes of the current MQTT client
        /// </summary>
        private void SafeDisconnect()
        {
            if (this.MqttClient == null)
            {
                return;
            }

            try
            {
                if (this.MqttClient.IsConnected)
                {
                    this.MqttClient.Disconnect();
                }

                this.MqttClient.Dispose();
                LogHelper.LogInformation("MQTT client disconnected and disposed");
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
}