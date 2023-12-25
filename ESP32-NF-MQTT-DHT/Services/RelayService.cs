
namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Device.Gpio;
    using System.Diagnostics;

    using Contracts;

    using Microsoft.Extensions.Logging;

    public class RelayService : IRelayService
    {
        private const int RelayPinNumber = 6;
        private readonly GpioController _gpioController;
        private readonly ILogger _logger;
        private GpioPin _relayPin;

        public RelayService(ILoggerFactory loggerFactory)
        {
            _gpioController = new GpioController();
            _logger = loggerFactory?.CreateLogger(nameof(RelayService)) ?? throw new ArgumentNullException(nameof(loggerFactory));

            this.InitializeRelayPin();
        }

        private void InitializeRelayPin()
        {
            _relayPin = _gpioController.OpenPin(RelayPinNumber, PinMode.Output);
        }

        public void TurnOn()
        {
            _relayPin.Write(PinValue.High);
            _logger.LogInformation("Relay turned ON");
        }

        public void TurnOff()
        {
            _relayPin.Write(PinValue.Low);
            _logger.LogInformation("Relay turned OFF");
        }
    }
}
