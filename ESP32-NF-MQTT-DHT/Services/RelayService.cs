namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Device.Gpio;

    using Contracts;

    using Microsoft.Extensions.Logging;

    using static Helpers.TimeHelper;

    /// <summary>
    /// Provides methods to control a relay connected to a specific GPIO pin.
    /// </summary>
    public class RelayService : IRelayService
    {
        private const int RelayPinNumber = 32;

        private GpioPin _relayPin;

        private readonly GpioController _gpioController;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayService"/> class.
        /// </summary>
        /// <param name="loggerFactory">Factory to create a logger for this service.</param>
        /// <exception cref="ArgumentNullException">Thrown if loggerFactory is null.</exception>
        public RelayService(ILoggerFactory loggerFactory)
        {
            _gpioController = new GpioController();
            _logger = loggerFactory?.CreateLogger(nameof(RelayService)) ?? throw new ArgumentNullException(nameof(loggerFactory));

            InitializeRelayPin();
        }

        /// <summary>
        /// Turns the relay on by setting the GPIO pin high.
        /// </summary>
        public void TurnOn()
        {
            _relayPin.Write(PinValue.High);
            _logger.LogInformation($"[{GetCurrentTimestamp()}] Relay turned ON");
        }

        /// <summary>
        /// Turns the relay off by setting the GPIO pin low.
        /// </summary>
        public void TurnOff()
        {
            _relayPin.Write(PinValue.Low);
            _logger.LogInformation($"[{GetCurrentTimestamp()}] Relay turned OFF");
        }

        private void InitializeRelayPin()
        {
            _relayPin = _gpioController.OpenPin(RelayPinNumber, PinMode.Output);
        }
    }
}
