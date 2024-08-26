namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Text;
    using System.Threading;

    using Contracts;
    using ESP32_NF_MQTT_DHT.Helpers;

    using Microsoft.Extensions.Logging;

    using Models;

    using nanoFramework.Json;
    using nanoFramework.M2Mqtt;
    using nanoFramework.M2Mqtt.Messages;
    using nanoFramework.Runtime.Native;

    using static Helpers.TimeHelper;
    using static Settings.DeviceSettings;
    using static Settings.MqttSettings;

    using IMqttClientService = ESP32_NF_MQTT_DHT.Services.Contracts.IMqttClientService;

    /// <summary>
    /// Service to handle MQTT client functionalities including connecting to the broker,
    /// handling messages, and managing a relay pin.
    /// </summary>
    internal class MqttClientService : IMqttClientService
    {
        private const int MaxReconnectAttempts = 20;
        private const int ReconnectDelay = 10000;
        private const int ErrorInterval = 10000;
        private const int SensorDataInterval = 300000;
        private const double InvalidTemperature = -50;
        private const double InvalidHumidity = -100;

        private static readonly string UptimeTopic = $"home/{DeviceName}/uptime";
        private static readonly string RelayTopic = $"home/{DeviceName}/switch";
        private static readonly string SystemTopic = $"home/{DeviceName}/system";
        private static readonly string DataTopic = $"home/{DeviceName}/messages";
        private static readonly string ErrorTopic = $"home/{DeviceName}/errors";

        private readonly IUptimeService _uptimeService;
        private readonly IConnectionService _connectionService;
        private readonly IDhtService _dhtService;
        private readonly IAhtSensorService _ahtSensorService;
        private readonly IShtc3SensorService _shtc3SensorService;

        private readonly LogHelper _logHelper;
        private readonly IRelayService _relayService;
        private readonly ManualResetEvent _stopSignal = new ManualResetEvent(false);

        private int _attemptCount = 1;
        private bool _isRunning = true;

        private Thread _sensorDataThread;
        private bool _isSensorDataThreadRunning = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientService"/> class.
        /// </summary>
        /// <param name="uptimeService">Service to get uptime information.</param>
        /// <param name="connectionService">Service to manage network connections.</param>
        /// <param name="loggerFactory">Factory to create a logger for this service.</param>
        /// <param name="relayService">Service to control and manage the relay operations.</param>
        /// <param name="dhtService"> Service to read data from the DHT sensor.</param>
        /// <param name="ahtSensorService"> Service to read data from the AHT sensor.</param>
        /// <param name="shtc3SensorService">Service to read data from the SHTC3 sensor.</param>
        /// <exception cref="ArgumentNullException">Thrown if loggerFactory is null.</exception>
        public MqttClientService(IUptimeService uptimeService,
                                 IConnectionService connectionService,
                                 ILoggerFactory loggerFactory,
                                 IRelayService relayService,
                                 IDhtService dhtService,
                                 IAhtSensorService ahtSensorService,
                                 IShtc3SensorService shtc3SensorService)
        {
            _uptimeService = uptimeService;
            _connectionService = connectionService;
            _relayService = relayService ?? throw new ArgumentNullException(nameof(relayService));
            _logHelper = new LogHelper(loggerFactory, nameof(MqttClientService));
            _dhtService = dhtService ?? throw new ArgumentNullException(nameof(dhtService));
            _ahtSensorService = ahtSensorService ?? throw new ArgumentNullException(nameof(ahtSensorService));
            _shtc3SensorService = shtc3SensorService ?? throw new ArgumentNullException(nameof(shtc3SensorService));
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
            Thread connectionThread = new Thread(this.ConnectToBroker);
            connectionThread.Start();
        }

        /// <summary>
        /// Connects to the MQTT broker.
        /// </summary>
        public void ConnectToBroker()
        {
            _isRunning = true;
            int attemptCount = 0;
            const int MaxAttempts = 1000;
            int delayBetweenAttempts = 5000;
            Random random = new Random();

            while (_isRunning && attemptCount < MaxAttempts)
            {
                if (this.TryConnectToBroker())
                {
                    this._logHelper.LogWithTimestamp(LogLevel.Information, "Starting sensor data thread...");
                    this.StartSensorDataThread();
                    return;
                }

                attemptCount++;
                _logHelper.LogWithTimestamp(LogLevel.Warning, $"Attempt {attemptCount} failed. Retrying in {delayBetweenAttempts / 1000} seconds...");

                int randomValue = random.Next() % 4000 + 1000;
                _stopSignal.WaitOne(delayBetweenAttempts + randomValue, false);

                delayBetweenAttempts = Math.Min(delayBetweenAttempts * 2, 120000);
            }

            this._logHelper.LogWithTimestamp(LogLevel.Warning, "Rebooting device...");
            Power.RebootDevice();
        }


        private void StartSensorDataThread()
        {
            if (_isSensorDataThreadRunning || (_sensorDataThread != null && _sensorDataThread.IsAlive))
            {
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

            this.ConnectToBroker();
        }

        private bool TryConnectToBroker()
        {
            this.DisposeCurrentClient();

            _connectionService.CheckConnection();

            try
            {
                _logHelper.LogWithTimestamp(LogLevel.Information, $"Attempting to connect to MQTT broker: {Broker}");
                this.MqttClient = new MqttClient(Broker);
                this.MqttClient.Connect(ClientId, ClientUsername, ClientPassword);

                if (MqttClient.IsConnected)
                {
                    this.SetupMqttClient();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogWithTimestamp(LogLevel.Error, $"Attempting to connect to MQTT broker: {ex.Message}");
            }

            return false;
        }

        private void DisposeCurrentClient()
        {
            if (this.MqttClient != null)
            {
                if (this.MqttClient.IsConnected)
                {
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
            this.MqttClient.MqttMsgPublishReceived += this.HandleIncomingMessage;
            _logHelper.LogWithTimestamp(LogLevel.Information, "MQTT client setup complete");
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
                    _logHelper.LogWithTimestamp(LogLevel.Warning, "Rebooting device...");
                    Thread.Sleep(2000);
                    Power.RebootDevice();
                }
            }
        }

        private void SensorDataLoop()
        {
            while (_isRunning && _isSensorDataThreadRunning)
            {
                try
                {
                    this.PublishSensorData();
                }
                catch (Exception ex)
                {
                    _logHelper.LogWithTimestamp(LogLevel.Error, $"SensorDataLoop Exception: {ex.Message}");
                    this.PublishError($"SensorDataLoop Exception: {ex.Message}");
                }

                _stopSignal.WaitOne(ErrorInterval, false);
            }
        }

        private void PublishError(string errorMessage)
        {
            this.MqttClient.Publish(ErrorTopic, Encoding.UTF8.GetBytes(errorMessage));
            _stopSignal.WaitOne(ErrorInterval, false);
        }

        private bool IsSensorDataValid(double[] data)
        {
            return !(data[0] == InvalidTemperature || data[1] == InvalidHumidity);
        }

        private void PublishSensorData()
        {
            double[] data;

            //data = _dhtService.GetData();
            //data = _ahtSensorService.GetData();
            data = _shtc3SensorService.GetData();

            if (this.IsSensorDataValid(data))
            {
                this.PublishValidSensorData(data);
                _logHelper.LogWithTimestamp(LogLevel.Information, $"Temperature: {data[0]:f2}°C, Humidity: {data[1]:f1}%");
                _stopSignal.WaitOne(SensorDataInterval, false);
            }
            else
            {
                this.PublishError($"[{GetCurrentTimestamp()}] Unable to read sensor data");
                _logHelper.LogWithTimestamp(LogLevel.Warning, "Unable to read sensor data");
                _stopSignal.WaitOne(ErrorInterval, false);
            }
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
                    SensorType = SensorType,
                    Date = DateTime.UtcNow.Date.ToString("dd/MM/yyyy"),
                    Time = GetCurrentTimestamp(),
                    Temp = temp,
                    Humid = (int)data[1],
                }
            };
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
