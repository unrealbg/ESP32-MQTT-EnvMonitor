namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Device.Gpio;
    using System.Diagnostics;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    using Constants;

    using nanoFramework.M2Mqtt;
    using nanoFramework.M2Mqtt.Exceptions;
    using nanoFramework.M2Mqtt.Messages;
    using nanoFramework.Runtime.Native;

    using Services.Contracts;

    internal class MqttClientService : IMqttClient
    {
        private readonly IUptimeService _uptimeService;

        private readonly IConnectionService _connectionService;

        private static GpioController _gpioController;

        public MqttClientService(IUptimeService uptimeService, IConnectionService connectionService)
        {
            this._uptimeService = uptimeService;
            this._connectionService = connectionService;
            _gpioController = new GpioController();
        }

        public GpioPin RelayPin { get; private set; }
        
        public MqttClient MqttClient { get; private set; }

        // start the client
        public void Start()
        {
            // Initialize the relay pin
            this.RelayPin = _gpioController.OpenPin(25, PinMode.Output);

            this.ClientConnect();
        }

        // Sends device uptime every minute // Demo method
        private void UptimeLoop()
        {
            string date = $"[s] The MQTT Client Is Started On - {DateTime.UtcNow.ToString("MM/dd/yyyy")}";
            string time = $" {DateTime.UtcNow.ToString("HH:mm:ss")}";

            string dateTime = date + time;

            Debug.WriteLine(dateTime);

            MqttClient.Publish("home/nf2/start/data", Encoding.UTF8.GetBytes(dateTime), MqttQoSLevel.AtLeastOnce, false);

            while (true)
            {
                try
                {
                    MqttClient.Publish(
                        "home/nf2/uptime",
                        Encoding.UTF8.GetBytes(this._uptimeService.GetUptime()),
                        MqttQoSLevel.AtLeastOnce,
                        false);

                    Debug.WriteLine(this._uptimeService.GetUptime());

                    Thread.Sleep(10000);
                }
                catch (OutOfMemoryException)
                {
                    Debug.WriteLine("[e] ERROR [Out of memory]");
                    Debug.WriteLine("[r] Restarting the device...");
                    Thread.Sleep(2000);
                    Power.RebootDevice();
                }
            }
        }

        private void ClientConnect()
        {
            int count = 0;

            while (true)
            {
                try
                {
                    Debug.WriteLine($"[c] Attempting to connect to the server [Attempt: {(++count).ToString()}]");
                    this.MqttClient = new MqttClient(Constants.BROKER);

                    this.MqttClient.Connect(Constants.CLIENT_ID, Constants.MQTT_CLIENT_USERNAME, Constants.MQTT_CLIENT_PASSWORD);

                    // Checks if the client connected successfully
                    if (MqttClient.IsConnected)
                    {
                        this.MqttClient.ConnectionClosed += this.ConnectionClosed;
                        this.MqttClient.MqttMsgPublishReceived += this.HandleIncomingMessage;

                        this.MqttClient.Subscribe(new[] { "#" }, new[] { MqttQoSLevel.AtLeastOnce });

                        Debug.WriteLine("[+] Successfully connected to MQTT Broker");
                        Thread.Sleep(2000);

                        Thread uptime = new Thread(this.UptimeLoop);
                        uptime.Start();

                        break;
                    }

                    Thread.Sleep(2000);
                }
                catch (MqttCommunicationException)
                {
                    Debug.WriteLine($"[ex] Mqtt Communication ERROR [Server down or wrong credentials]");
                    Thread.Sleep(5000);
                }
                catch (SocketException)
                {
                    Debug.WriteLine($"[ex] Communication ERROR [Server down or wrong credentials]");
                    Thread.Sleep(5000);
                }
                catch (Exception)
                {
                    Debug.WriteLine("[ex] ERROR [Lost connection to the server or the server has stopped]");

                    if (count > 20)
                    {
                        Power.RebootDevice();
                    }

                    Thread.Sleep(10000);
                }
            }

            Thread.Sleep(5000);
        }

        // if the connection to the server is lost we are trying to connect again
        private void ConnectionClosed(object sender, EventArgs e)
        {
            Debug.WriteLine("[-] Lost connection...");
            Debug.WriteLine("[r] Trying to reconnect...");
            Thread.Sleep(5000);

            this._connectionService.Connect();
            this.ClientConnect();
        }

        // handle incoming messages from the server
        private void HandleIncomingMessage(object sender, MqttMsgPublishEventArgs e)
        {
            //// Debug.WriteLine($"Message received: {Encoding.UTF8.GetString(e.Message, 0, e.Message.Length)}");

            var msg = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);

            // turns the relay on and off when a command is given
            if (e.Topic == "home/nf2/switch/Relay")
            {
                if (msg.Contains("on"))
                {
                    this.RelayPin.Write(PinValue.High);
                    Debug.WriteLine("ON");
                }
                else if (msg.Contains("off"))
                {
                    this.RelayPin.Write(PinValue.Low);
                    Debug.WriteLine("OFF");
                }
            }
        }
    }
}
