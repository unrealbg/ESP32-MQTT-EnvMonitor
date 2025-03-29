﻿namespace ESP32_NF_MQTT_DHT.Services
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

    using static Helpers.Constants;
    using static Settings.MqttSettings;

    /// <summary>
    /// Service that manages MQTT client functionalities, including connecting to the broker,
    /// handling messages, managing sensor data, and reconnecting in case of errors.
    /// </summary>
    internal class MqttClientService : IMqttClientService, IDisposable
    {
        private readonly IConnectionService _connectionService;
        private readonly IInternetConnectionService _internetConnectionService;
        private readonly MqttMessageHandler _mqttMessageHandler;
        private readonly IMqttPublishService _mqttPublishService;
        private readonly IMqttConnectionManager _connectionManager;
        private readonly ISensorDataPublisher _sensorDataPublisher;

        private readonly ManualResetEvent _stopSignal = new ManualResetEvent(false);
        private readonly object _connectionLock = new object();
        private readonly object _stateLock = new object();

        private bool _isRunning;
        private bool _isConnecting;
        private bool _isHeartbeatRunning;
        private bool _isDisposed;

        private Thread _connectionThread;

        private bool _circuitOpen;
        private DateTime _circuitResetTime;

        /// <summary>
        /// Initializes a new instance of the MqttClientService class.
        /// </summary>
        public MqttClientService(
            IConnectionService connectionService,
            IInternetConnectionService internetConnectionService,
            MqttMessageHandler mqttMessageHandler,
            IMqttPublishService mqttPublishService,
            IMqttConnectionManager connectionManager)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            _internetConnectionService = internetConnectionService ?? throw new ArgumentNullException(nameof(internetConnectionService));
            _mqttMessageHandler = mqttMessageHandler ?? throw new ArgumentNullException(nameof(mqttMessageHandler));
            _mqttPublishService = mqttPublishService ?? throw new ArgumentNullException(nameof(mqttPublishService));

            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _sensorDataPublisher = new SensorDataPublisher(this.SensorDataTimerCallback);

            _internetConnectionService.InternetLost += this.OnInternetLost;
            _internetConnectionService.InternetRestored += this.OnInternetRestored;

            this.SetIsRunning(true);
            _circuitOpen = false;
        }

        /// <summary>
        /// Gets the current instance of the MQTT client.
        /// </summary>
        public MqttClient MqttClient => _connectionManager.MqttClient;

        /// <summary>
        /// Starts the MQTT service by establishing a connection to the broker.
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

            if (_circuitOpen && DateTime.UtcNow < _circuitResetTime)
            {
                LogHelper.LogWarning("Circuit breaker is open. Delaying connection attempts until reset.");
                return;
            }

            if (this._circuitOpen)
            {
                this._circuitOpen = false;
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
        /// Stops the MQTT service.
        /// </summary>
        public void Stop()
        {
            this.SetIsRunning(false);
            _sensorDataPublisher.Stop();

            this.SafeDisconnect();

            _stopSignal.Set();

            if (_connectionThread != null && _connectionThread.IsAlive)
            {
                _connectionThread.Join(1000);
            }
        }

        /// <summary>
        /// Disposes the used resources.
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

            if (this.MqttClient != null)
            {
                this.MqttClient.ConnectionClosed -= this.ConnectionClosed;
                this.MqttClient.MqttMsgPublishReceived -= _mqttMessageHandler.HandleIncomingMessage;
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Establishes connection to the MQTT broker using an exponential backoff strategy
        /// and activates a simple circuit breaker if maximum attempts are reached.
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
                LogHelper.LogInformation("Checking internet connection before broker connection.");

                while (this.GetIsRunning() && attemptCount < MAX_TOTAL_ATTEMPTS)
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

                _circuitOpen = true;
                _circuitResetTime = DateTime.UtcNow.AddMinutes(5);
                LogHelper.LogWarning("Max connection attempts reached. Circuit breaker activated.");

                this.HandleMaxAttemptsReached();
            }
            finally
            {
                _isConnecting = false;
            }
        }

        /// <summary>
        /// Checks if the process is currently running by accessing the _isRunning variable in a thread-safe manner.
        /// </summary>
        /// <returns>Returns true if the process is running, otherwise false.</returns>
        private bool GetIsRunning()
        {
            lock (_stateLock)
            {
                return _isRunning;
            }
        }

        /// <summary>
        /// Sets the running state of the process in a thread-safe manner.
        /// </summary>
        /// <param name="value">Indicates whether the process should be in a running state or not.</param>
        private void SetIsRunning(bool value)
        {
            lock (_stateLock)
            {
                _isRunning = value;
            }
        }

        /// <summary>
        /// Handles actions when maximum connection attempts are reached – enters deep sleep.
        /// </summary>
        private void HandleMaxAttemptsReached()
        {
            LogHelper.LogWarning("Entering deep sleep to conserve power");

            this.SafeDisconnect();
            _sensorDataPublisher.Stop();

            _stopSignal.WaitOne(2000, false);

            TimeSpan deepSleepDuration = new TimeSpan(0, DEEP_SLEEP_MINUTES, 0);

            Sleep.EnableWakeupByTimer(deepSleepDuration);
            Sleep.StartDeepSleep();
        }

        /// <summary>
        /// Attempts to connect to the MQTT broker while checking internet connectivity
        /// and handling exceptions.
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

                if (isConnected && this.MqttClient != null && this.MqttClient.IsConnected)
                {
                    this.InitializeMqttClient();
                    return true;
                }
            }
            catch (SocketException ex)
            {
                LogHelper.LogError($"Socket error: {ex.Message}");
                LogService.LogCritical($"Socket error: {ex.Message}");
            }
            catch (NullReferenceException ex)
            {
                LogHelper.LogError($"Null reference encountered: {ex.Message}");
                LogService.LogCritical($"Null reference encountered: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"MQTT connect error: {ex.Message}");
                LogService.LogCritical($"MQTT connect error: {ex.Message}");
            }

            this.SafeDisconnect();
            return false;
        }

        /// <summary>
        /// Initializes the MQTT client by subscribing to topics and setting event handlers.
        /// </summary>
        private void InitializeMqttClient()
        {
            if (this.MqttClient == null)
            {
                LogHelper.LogError("Cannot initialize null MQTT client");
                return;
            }

            this.MqttClient.ConnectionClosed -= this.ConnectionClosed;
            this.MqttClient.MqttMsgPublishReceived -= _mqttMessageHandler.HandleIncomingMessage;

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
            if (_isDisposed || !this.GetIsRunning())
            {
                return;
            }

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
                catch (Exception innerEx)
                {
                    LogHelper.LogError($"Error publishing sensor data error: {innerEx.Message}");
                }

                LogService.LogCritical($"SensorDataTimer Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the loss of connection to the MQTT broker and attempts reconnection.
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
                    this.Start();
                }
                else
                {
                    LogHelper.LogInformation("Internet check thread is running, waiting for it to finish...");
                }
            }
        }

        /// <summary>
        /// Starts the service when internet connectivity is restored.
        /// </summary>
        private void OnInternetRestored(object sender, EventArgs e)
        {
            if (!_isDisposed && this.GetIsRunning())
            {
                this.Start();
            }
        }

        /// <summary>
        /// Stops sensor data publishing when the internet connection is lost.
        /// </summary>
        private void OnInternetLost(object sender, EventArgs e)
        {
            this.SafeDisconnect();
            _sensorDataPublisher.Stop();
        }

        /// <summary>
        /// Safely disconnects and disposes of the MQTT client.
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
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"Error while disconnecting MQTT client: {ex.Message}");
                LogService.LogCritical($"Error while disconnecting MQTT client: {ex.Message}");
            }
            finally
            {
                try
                {
                    this.MqttClient.Dispose();
                    LogHelper.LogInformation("MQTT client disconnected and disposed");
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"Error while disposing MQTT client: {ex.Message}");
                    LogService.LogCritical($"Error while disposing MQTT client: {ex.Message}");
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
}