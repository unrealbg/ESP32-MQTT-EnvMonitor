namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Device.Gpio;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;

    using Constants;

    using nanoFramework.M2Mqtt;
    using nanoFramework.M2Mqtt.Messages;
    using nanoFramework.Runtime.Native;

    using Services.Contracts;

    internal class MqttClientService : IMqttClient
    {
        private readonly IUptimeService _uptimeService;
        private static GpioController _gpioController;

        public GpioPin RelayPin { get; private set; }

        public MqttClientService(IUptimeService uptimeService)
        {
            this._uptimeService = uptimeService;
            _gpioController = new GpioController();
        }

        public MqttClient MqttClient { get; private set; }

        // start the client
        public void Start()
        {
            this.MqttClient = new MqttClient(Constants.BROKER);

            this.RelayPin = _gpioController.OpenPin(25, PinMode.Output);

            this.MqttClient.Connect(
                Constants.CLIENT_ID,
                Constants.MQTT_CLIENT_USERNAME,
                Constants.MQTT_CLIENT_PASSWORD);

            MqttClient.ConnectionClosed += this.ConnectionClosed;

            this.MqttClient.Subscribe(new[] { "#" }, new[] { MqttQoSLevel.AtLeastOnce });
            this.MqttClient.MqttMsgPublishReceived += this.HandleIncomingMessage;
            Debug.WriteLine("[+] MQTT Client Connected!");

            this.SendUptimePer1S();
        }

        // if the connection to the server is lost restart the device
        private void ConnectionClosed(object sender, EventArgs e)
        {
            Debug.WriteLine("[-] Lost Connection...");
            Debug.WriteLine("[r] Rebooting the device...");
            Thread.Sleep(5000);
            Power.RebootDevice();
        }

        // receives the messages from the server
        private void HandleIncomingMessage(object sender, MqttMsgPublishEventArgs e)
        {
            //// Debug.WriteLine($"Message received: {Encoding.UTF8.GetString(e.Message, 0, e.Message.Length)}");

            var msg = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);

            // turns a relay on and off
            if (e.Topic == "home/nf2/switch/Relay")
            {
                if (msg.Contains("true"))
                {
                    this.RelayPin.Write(PinValue.High);
                    Debug.WriteLine("ON");
                }
                else if (msg.Contains("false"))
                {
                    this.RelayPin.Write(PinValue.Low);
                    Debug.WriteLine("OFF");
                }
            }
        }

        // Sends uptime every second
        private void SendUptimePer1S()
        {
            while (true)
            {
                MqttClient.Publish(
                    "home/nf2/uptime",
                    Encoding.UTF8.GetBytes(this._uptimeService.GetUptime()),
                    MqttQoSLevel.AtLeastOnce,
                    false);
                Thread.Sleep(1000);
            }
        }
    }
}
