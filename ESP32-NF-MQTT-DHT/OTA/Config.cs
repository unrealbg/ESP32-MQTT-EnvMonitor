namespace ESP32_NF_MQTT_DHT.OTA
{
    internal static class Config
    {
        // Identity
    public static string DeviceId = ESP32_NF_MQTT_DHT.Settings.DeviceSettings.DeviceName;

        // MQTT broker
    public static string BrokerHost = ESP32_NF_MQTT_DHT.Settings.MqttSettings.Broker;
    public static int BrokerPort = ESP32_NF_MQTT_DHT.Settings.MqttSettings.Port;
    public static bool BrokerTls = ESP32_NF_MQTT_DHT.Settings.MqttSettings.UseTls;
    public static string BrokerUser = ESP32_NF_MQTT_DHT.Settings.MqttSettings.ClientUsername;
    public static string BrokerPass = ESP32_NF_MQTT_DHT.Settings.MqttSettings.ClientPassword;

        // Topics
    public static string TopicCmd = "home/" + DeviceId + "/ota/cmd";
    public static string TopicStatus = "home/" + DeviceId + "/ota/status";

        // Periodic check URL (empty to disable)
        public static string PeriodicManifestUrl = ""; // e.g., https://cdn.example.com/env/manifest-latest.json

        // Storage
    public const string AppDir = "I:/data/app";
    public const string VersionFile = "I:/data/app/CurrentVersion.txt";
    public const string ModulesDir = AppDir + "/modules";

        // Behavior
        public static bool RebootAfterApply = true;
        public static bool CleanAfterApply = true;

        // Main entry assembly name (should match manifest name)
    public const string MainAppName = "App.pe";

    // Entry point configuration for the loaded App.pe
    // If your Entry class is namespaced, set fully qualified name, e.g. "MyApp.Boot.Entry"
    public static string EntryTypeName = "Entry";
    public static string EntryMethodName = "Start";
    }
}