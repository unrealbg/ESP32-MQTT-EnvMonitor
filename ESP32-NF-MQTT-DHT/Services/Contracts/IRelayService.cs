namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    /// <summary>
    /// Interface for relay control services.
    /// </summary>
    public interface IRelayService
    {
        /// <summary>
        /// Turns the relay on.
        /// </summary>
        void TurnOn();

        /// <summary>
        /// Turns the relay off.
        /// </summary>
        void TurnOff();

        /// <summary>
        /// Checks if the relay is on.
        /// </summary>
        /// <returns></returns>
        bool IsRelayOn();
    }
}