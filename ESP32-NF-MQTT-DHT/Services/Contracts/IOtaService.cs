namespace ESP32_NF_MQTT_DHT.Services.Contracts
{
    /// <summary>
    /// OTA service contract for handling OTA commands and optional background checks.
    /// </summary>
    public interface IOtaService
    {
        /// <summary>
        /// Handle an OTA command payload. Supports raw URL or JSON with {"url": "..."}.
        /// </summary>
        /// <param name="payload">MQTT message payload.</param>
        void HandleOtaCommand(string payload);
    }
}
