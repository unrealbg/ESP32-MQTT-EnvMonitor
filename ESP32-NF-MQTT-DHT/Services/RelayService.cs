namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Device.Gpio;

    using Contracts;

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Provides methods to control a relay connected to a specific GPIO pin.
    /// </summary>
    public class RelayService : IRelayService
    {
        /// <summary>
        /// GPIO pin number to which the relay is connected.
        /// </summary>
        private const int RelayPinNumber = 32;

        /// <summary>
        /// Controller for managing GPIO pins.
        /// </summary>
        private readonly GpioController _gpioController;

        /// <summary>
        /// Logger for logging relay operation information.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// GPIO pin used to control the relay.
        /// </summary>
        private GpioPin _relayPin;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayService"/> class.
        /// </summary>
        /// <param name="loggerFactory">Factory to create a logger for this service.</param>
        /// <exception cref="ArgumentNullException">Thrown if loggerFactory is null.</exception>
        public RelayService(ILoggerFactory loggerFactory)
        {
            _gpioController = new GpioController();
            _logger = loggerFactory?.CreateLogger(nameof(RelayService)) ?? throw new ArgumentNullException(nameof(loggerFactory));

            this.InitializeRelayPin();
        }

        /// <summary>
        /// Initializes the GPIO pin used for relay control.
        /// </summary>
        private void InitializeRelayPin()
        {
            _relayPin = _gpioController.OpenPin(RelayPinNumber, PinMode.Output);
        }

        /// <summary>
        /// Turns the relay on by setting the GPIO pin high.
        /// </summary>
        public void TurnOn()
        {
            _relayPin.Write(PinValue.High);
            _logger.LogInformation("Relay turned ON");
        }

        /// <summary>
        /// Turns the relay off by setting the GPIO pin low.
        /// </summary>
        public void TurnOff()
        {
            _relayPin.Write(PinValue.Low);
            _logger.LogInformation("Relay turned OFF");
        }
    }
}
