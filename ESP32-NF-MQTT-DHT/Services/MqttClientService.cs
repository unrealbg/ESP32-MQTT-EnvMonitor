namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Net.Sockets;
    using System.Threading;

    using Contracts;

    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Services.MQTT;
    using ESP32_NF_MQTT_DHT.Services.MQTT.Contracts;

    using nanoFramework.M2Mqtt;
    using nanoFramework.M2Mqtt.Messages;
    using nanoFramework.Runtime.Native;

    using static Settings.MqttSettings;

    using IMqttClientService = Contracts.IMqttClientService;

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

        private readonly LogHelper _logHelper;
        private readonly ManualResetEvent _stopSignal = new ManualResetEvent(false);

        private bool _isRunning = true;
        private bool _isConnecting = false;
        private bool _isSensorDataThreadRunning = false;

        private Thread _connectionThread;
        private Thread _sensorDataThread;
        private readonly object _threadLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientService"/> class.
        /// </summary>
        /// <param name="connectionService">Service that manages network connections.</param>
        /// <param name="internetConnectionService">Service that checks for internet availability.</param>
        /// <param name="mqttMessageHandler">Service to handle incoming MQTT messages.</param>
        /// <param name="mqttPublishService">Service to publish MQTT messages to the broker.</param>
        public MqttClientService(IConnectionService connectionService,
                                 IInternetConnectionService internetConnectionService,
                                 MqttMessageHandler mqttMessageHandler,
                                 IMqttPublishService mqttPublishService)
        {
            _connectionService = connectionService;
            _logHelper = new LogHelper();
            _internetConnectionService = internetConnectionService ?? throw new ArgumentNullException(nameof(internetConnectionService));
            _mqttMessageHandler = mqttMessageHandler;
            _mqttPublishService = mqttPublishService;

            _internetConnectionService.InternetLost += OnInternetLost;
            _internetConnectionService.InternetRestored += OnInternetRestored;
        }

        /// <summary>
        /// Gets the current instance of the MQTT client.
        /// </summary>
        public MqttClient MqttClient { get; private set; }

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
                    _logHelper.LogWithTimestamp("Already attempting to connect to the broker.");
                    return;
                }

                _isConnecting = true;
            }

            _isRunning = true;
            int attemptCount = 0;
            int delayBetweenAttempts = 5000;
            Random random = new Random();

            _connectionService.CheckConnection();

            while (_isRunning && attemptCount < MaxAttempts)
            {
                if (this.AttemptBrokerConnection())
                {
                    _logHelper.LogWithTimestamp("Starting sensor data thread...");
                    this.StartSensorDataThread();
                    _isConnecting = false;
                    return;
                }

                attemptCount++;
                _logHelper.LogWithTimestamp($"Attempt {attemptCount} failed. Retrying in {delayBetweenAttempts / 1000} seconds...");

                int randomValue = random.Next() % (4000 - 1000 + 1) + 1000;
                _stopSignal.WaitOne(delayBetweenAttempts + randomValue, false);

                delayBetweenAttempts = Math.Min(delayBetweenAttempts * 2, MaxReconnectDelay);
            }

            this._logHelper.LogWithTimestamp("Max reconnect attempts reached. Rebooting device...");
            Power.RebootDevice();
        }

        /// <summary>
        /// Starts a separate thread to handle sensor data publishing at regular intervals.
        /// </summary>
        private void StartSensorDataThread()
        {
            lock (_threadLock)
            {
                if (_sensorDataThread != null && _sensorDataThread.IsAlive)
                {
                    _logHelper.LogWithTimestamp("Sensor data thread is already running.");
                    return;
                }

                _sensorDataThread = new Thread(this.SensorDataLoop);
                _isSensorDataThreadRunning = true;

                try
                {
                    _sensorDataThread.Start();
                    _logHelper.LogWithTimestamp("Sensor data thread started successfully.");
                }
                catch (Exception ex)
                {
                    _logHelper.LogWithTimestamp($"Failed to start sensor data thread: {ex.Message}");
                    _isSensorDataThreadRunning = false;
                }
            }
        }

        /// <summary>
        /// Stops the thread that handles sensor data publishing.
        /// </summary>
        private void StopSensorDataThread()
        {
            _logHelper.LogWithTimestamp("Stopping sensor data thread...");
            _isSensorDataThreadRunning = false;

            if (_sensorDataThread != null && _sensorDataThread.IsAlive)
            {
                try
                {
                    if (!_sensorDataThread.Join(65000))
                    {
                        _logHelper.LogWithTimestamp("Sensor data thread did not stop in time.");
                    }
                }
                catch (Exception ex)
                {
                    _logHelper.LogWithTimestamp($"Error while stopping sensor data thread: {ex.Message}");
                }
                finally
                {
                    _sensorDataThread = null;
                    _logHelper.LogWithTimestamp("Sensor data thread stopped.");
                }
            }
        }

        /// <summary>
        /// The loop that continuously publishes sensor data to the MQTT broker.
        /// </summary>
        private void SensorDataLoop()
        {
            while (_isSensorDataThreadRunning)
            {
                try
                {
                    _mqttPublishService.PublishSensorData();
                    _stopSignal.WaitOne(SensorDataInterval, false);
                }
                catch (Exception ex)
                {
                    _logHelper.LogWithTimestamp($"SensorDataLoop Exception: {ex.Message}");
                    _mqttPublishService.PublishError($"SensorDataLoop Exception: {ex.Message}");
                    _stopSignal.WaitOne(ErrorInterval, false);
                }
            }

            _logHelper.LogWithTimestamp("Sensor data thread has stopped.");
        }

        /// <summary>
        /// Handles reconnection logic when the connection to the MQTT broker is lost.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ConnectionClosed(object sender, EventArgs e)
        {
            _logHelper.LogWithTimestamp("Lost connection to MQTT broker, attempting to reconnect...");

            this.StopSensorDataThread();

            if (!this._internetConnectionService.IsInternetThreadRunning)
            {
                this.EstablishBrokerConnection();
            }
            else
            {
                _logHelper.LogWithTimestamp("Internet check thread is running, waiting for it to finish...");
            }
        }

        /// <summary>
        /// Starts the MQTT client service when the internet connection is restored.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnInternetRestored(object sender, EventArgs e)
        {
            this.Start();
        }

        /// <summary>
        /// Stops the sensor data thread when the internet connection is lost.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnInternetLost(object sender, EventArgs e)
        {
            this.StopSensorDataThread();
        }

        /// <summary>
        /// Attempts to connect to the MQTT broker. Checks the internet connection,
        /// and handles exceptions in the connection process.
        /// </summary>
        /// <returns><c>true</c> if the connection is successful, otherwise <c>false</c>.</returns>
        private bool AttemptBrokerConnection()
        {
            if (!this.CheckInternetConnection())
            {
                return false;
            }

            this.DisposeMqttClient();

            try
            {
                this.ConnectToMqttBroker();

                if (MqttClient.IsConnected)
                {
                    this.InitializeMqttClient();
                    return true;
                }
            }
            catch (SocketException ex)
            {
                _logHelper.LogWithTimestamp($"SocketException while connecting to MQTT broker: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logHelper.LogWithTimestamp($"Exception while connecting to MQTT broker: {ex.Message}");
            }

            this.DisposeMqttClient();
            return false;
        }

        /// <summary>
        /// Disposes of the current MQTT client and ensures disconnection.
        /// </summary>
        private void DisposeMqttClient()
        {
            if (this.MqttClient != null)
            {
                using (this.MqttClient)
                {
                    if (this.MqttClient.IsConnected)
                    {
                        _logHelper.LogWithTimestamp("Disposing current MQTT client...");
                        this.MqttClient.Disconnect();
                    }
                }

                this.MqttClient.Dispose();
                this.MqttClient = null;
            }
        }

        /// <summary>
        /// Initializes the MQTT client by subscribing to topics and setting event handlers for incoming messages.
        /// </summary>
        private void InitializeMqttClient()
        {
            this.MqttClient.ConnectionClosed += this.ConnectionClosed;
            this.MqttClient.Subscribe(new[] { "#" }, new[] { MqttQoSLevel.AtLeastOnce });
            this.MqttClient.MqttMsgPublishReceived += _mqttMessageHandler.HandleIncomingMessage;
            _logHelper.LogWithTimestamp("MQTT client setup complete");
        }

        /// <summary>
        /// Checks whether the internet connection is available.
        /// </summary>
        /// <returns><c>true</c> if the internet is available, otherwise <c>false</c>.</returns>
        private bool CheckInternetConnection()
        {
            if (!_internetConnectionService.IsInternetAvailable())
            {
                _logHelper.LogWithTimestamp("No internet connection, cannot connect to MQTT broker.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Connects the MQTT client to the specified broker using provided credentials.
        /// </summary>
        private void ConnectToMqttBroker()
        {
            _logHelper.LogWithTimestamp($"Attempting to connect to MQTT broker: {Broker}");
            MqttClient = new MqttClient(Broker);
            MqttClient.Connect(ClientId, ClientUsername, ClientPassword);

            _mqttMessageHandler.SetMqttClient(MqttClient);
            _mqttPublishService.SetMqttClient(MqttClient);
        }

        /// <summary>
        /// Stops the MQTT client service by disconnecting the MQTT client and stopping the sensor data thread.
        /// </summary>
        private void Stop()
        {
            _isRunning = false;
            StopSensorDataThread();

            if (this.MqttClient != null && this.MqttClient.IsConnected)
            {
                this.MqttClient.Disconnect();
            }

            this.MqttClient?.Dispose();
            _stopSignal.Set();
        }
    }
}
