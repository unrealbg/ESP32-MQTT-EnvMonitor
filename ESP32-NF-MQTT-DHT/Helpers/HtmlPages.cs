namespace ESP32_NF_MQTT_DHT.Helpers
{
    public static class HtmlPages
    {
        public static string IndexPage = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>ESP32 NanoFramework Sensor API Documentation</title>
    <style>
        body { font-family: Arial, sans-serif; }
        h1, h2 { color: #333; }
        pre { background-color: #f4f4f4; padding: 15px; }
        .endpoint { background-color: #eee; padding: 10px; margin: 10px 0; }
    </style>
</head>
<body>
    <h1>ESP32 NanoFramework Sensor API Documentation</h1>
    <p>This documentation describes the API endpoints for interacting with the ESP32 NanoFramework Sensor Controller.</p>

    <section class=""endpoint"">
        <h2>1. Get Index Page</h2>
        <p><strong>Endpoint:</strong> /</p>
        <p><strong>Method:</strong> GET</p>
        <p><strong>Description:</strong> Returns the main index page for the ESP32 nanoFramework Web Server.</p>
        <p><strong>Response Type:</strong> text/html</p>
        <pre>&lt;p&gt;ESP32 nanoFramework Web Server&lt;/p&gt;</pre>
    </section>

    <section class=""endpoint"">
        <h2>2. Get Temperature</h2>
        <p><strong>Endpoint:</strong> /api/temperature</p>
        <p><strong>Method:</strong> GET</p>
        <p><strong>Description:</strong> Fetches the current temperature reading.</p>
        <p><strong>Response Type:</strong> application/json</p>
        <pre>{
    ""temperature"": 23.45
}</pre>
    </section>

    <section class=""endpoint"">
        <h2>3. Get Humidity</h2>
        <p><strong>Endpoint:</strong> /api/humidity</p>
        <p><strong>Method:</strong> GET</p>
        <p><strong>Description:</strong> Fetches the current humidity level.</p>
        <p><strong>Response Type:</strong> application/json</p>
        <pre>{
    ""humidity"": 56.7
}</pre>
    </section>

    <section class=""endpoint"">
        <h2>4. Get Sensor Data</h2>
        <p><strong>Endpoint:</strong> /api/data</p>
        <p><strong>Method:</strong> GET</p>
        <p><strong>Description:</strong> Fetches the current temperature and humidity readings along with date and time.</p>
        <p><strong>Response Type:</strong> application/json</p>
        <pre>{
    ""Data"": {
        ""Date"": ""dd/MM/yyyy"",
        ""Time"": ""HH:mm:ss"",
        ""Temp"": temperatureValue,
        ""Humid"": humidityValue
    }
}</pre>
    </section>
</body>
</html>";
    }
}


