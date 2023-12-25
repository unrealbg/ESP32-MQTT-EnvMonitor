namespace ESP32_NF_MQTT_DHT.Helpers
{
    public static class HtmlPages
    {
        public static string IndexPage = @"
            <html>
            <head>
                <title>ESP32 Sensor Dashboard</title>
                <style>
                    body {
                        font-family: Arial, sans-serif;
                        background-color: #f4f4f4;
                        text-align: center;
                        padding: 50px;
                    }
                    h1 {
                        color: #333;
                    }
                    .sensor-data {
                        margin-top: 20px;
                        padding: 10px;
                        background-color: #e0e0e0;
                        display: inline-block;
                    }
                </style>
            </head>
            <body>
                <h1>ESP32 Sensor Dashboard</h1>
                <div class='sensor-data'>
                    <h2>Temperature</h2>
                    <p id='temperature'>Loading...</p>
                </div>
                <div class='sensor-data'>
                    <h2>Humidity</h2>
                    <p id='humidity'>Loading...</p>
                </div>
                <script>
                    function fetchData(url, elementId) {
                        var xhr = new XMLHttpRequest();
                        xhr.onreadystatechange = function() {
                            if (xhr.readyState === 4 && xhr.status === 200) {
                                var data = JSON.parse(xhr.responseText);
                                document.getElementById(elementId).innerText = data[elementId] + (elementId === 'temperature' ? '°C' : '%');
                            }
                        };
                        xhr.open('GET', url, true);
                        xhr.send();
                    }
                    setInterval(function() {
                        fetchData('/api/temperature', 'temperature');
                        fetchData('/api/humidity', 'humidity');
                    }, 5000); // Fetch data every 5 seconds
                </script>
            </body>
            </html>";
    }
}
