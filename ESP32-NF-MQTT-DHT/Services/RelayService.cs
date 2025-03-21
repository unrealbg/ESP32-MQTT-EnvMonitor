﻿namespace ESP32_NF_MQTT_DHT.Services
{
    using System;
    using System.Device.Gpio;

    using Contracts;

    using Helpers;

    /// <summary>
    /// Provides methods to control a relay connected to a specific GPIO pin.
    /// </summary>
    public class RelayService : IRelayService, IDisposable
    {
        // Must change this to the actual GPIO pin number where the relay is connected.
        // On the ESP32 DevKit V1, the relay is connected to GPIO 32.
        // On the ESP32_S3 DevKitC, the relay is connected to GPIO 3.
        // On other boards, the relay may be connected to a different GPIO pin.
        private const int RelayPinNumber = 3;

        private readonly GpioController _gpioController;
        private readonly object _syncLock = new object();

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
            lock (_syncLock)
            {
                if (_relayPin == null)
                {
                    LogHelper.LogError("Relay pin is not initialized.");
                    return;
                }
                _relayPin.Write(PinValue.High);
                _isRelayOn = true;
                LogHelper.LogInformation("Relay turned ON");
            }
        }

        /// <summary>
        /// Turns the relay off by setting the GPIO pin low.
        /// </summary>
        public void TurnOff()
        {
            lock (_syncLock)
            {
                if (_relayPin == null)
                {
                    LogHelper.LogError("Relay pin is not initialized.");
                    return;
                }
                _relayPin.Write(PinValue.Low);
                _isRelayOn = false;
                LogHelper.LogInformation("Relay turned OFF");
            }
        }

        /// <summary>
        /// Checks if the relay is on.
        /// </summary>
        /// <returns></returns>
        public bool IsRelayOn()
        {
            lock (_syncLock)
            {
                return _isRelayOn;
            }
        }

        /// <summary>
        /// Releases resources used by the RelayService.
        /// </summary>
        public void Dispose()
        {
            lock (_syncLock)
            {
                _relayPin?.Dispose();
                _gpioController?.Dispose();
            }
        }

        private void InitializeRelayPin()
        {
            _relayPin = _gpioController.OpenPin(RelayPinNumber, PinMode.Output);
        }
    }
}
