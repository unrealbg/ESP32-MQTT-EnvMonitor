namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Device.Gpio;

    using Contracts;

    using Helpers;

    /// <summary>
    /// Provides methods to control a relay connected to a specific GPIO pin.
    /// </summary>
    public class RelayService : IRelayService
    {
        private const int RelayPinNumber = 32;

        private readonly GpioController _gpioController;

        private GpioPin _relayPin;
        private bool _isRelayOn;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayService"/> class.
        /// </summary>
        /// <param name="loggerFactory">Factory to create a logger for this service.</param>
        /// <exception cref="ArgumentNullException">Thrown if loggerFactory is null.</exception>
        public RelayService()
        {
            _gpioController = new GpioController();
            this.InitializeRelayPin();
        }

        /// <summary>
        /// Turns the relay on by setting the GPIO pin high.
        /// </summary>
        public void TurnOn()
        {
            _relayPin.Write(PinValue.High);
            _isRelayOn = true;
            LogHelper.LogInformation("Relay turned ON");
        }

        /// <summary>
        /// Turns the relay off by setting the GPIO pin low.
        /// </summary>
        public void TurnOff()
        {
            _relayPin.Write(PinValue.Low);
            _isRelayOn = false;
            LogHelper.LogInformation("Relay turned OFF");
        }

        /// <summary>
        /// Checks if the relay is on.
        /// </summary>
        /// <returns></returns>
        public bool IsRelayOn() => _isRelayOn;

        private void InitializeRelayPin()
        {
            _relayPin = _gpioController.OpenPin(RelayPinNumber, PinMode.Output);
        }
    }
}
