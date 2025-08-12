namespace ESP32_NF_MQTT_DHT.Settings
{
    /// <summary>
    /// Holds embedded OTA TLS certificate(s) so they are deployed with the firmware.
    /// </summary>
    public static class OtaCertificates
    {
        // Embedded PEM content. Update this to the correct CA (root or intermediate) as needed.
    public const string RootCaPem = @"-----BEGIN CERTIFICATE-----
Cert here
-----END CERTIFICATE-----";
    }
}