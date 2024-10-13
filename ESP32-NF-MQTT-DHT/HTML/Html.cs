namespace ESP32_NF_MQTT_DHT.HTML
{
    public static class Html
    {
        public static string GetIndexContent()
        {
            return @"<!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Sensor Data</title>
                <style>
                    body {
                        font-family: Arial, sans-serif;
                        background-color: #f4f4f4;
                        margin: 0;
                        padding: 20px;
                        text-align: center;
                    }

                    .container {
                        background-color: #fff;
                        padding: 20px;
                        border-radius: 10px;
                        box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
                    }

                    h1 {
                        color: #333;
                    }

                    p {
                        font-size: 18px;
                    }

                    button {
                        padding: 10px 20px;
                        font-size: 16px;
                        border: none;
                        border-radius: 5px;
                        cursor: pointer;
                        color: white;
                    }

                    .btn-on {
                        background-color: #4CAF50;
                    }

                    .btn-off {
                        background-color: #f44336;
                    }
                </style>
            </head>
            <body>
                <div class=""container"">
                    <h1>Sensor Data</h1>
                    <p>Temperature: <span id=""temperature"">Loading...</span> °C</p>
                    <p>Humidity: <span id=""humidity"">Loading...</span> %</p>
                    <p>Date: <span id=""date"">Loading...</span></p>
                    <p>Time: <span id=""time"">Loading...</span></p>
                    <p>Sensor Type: <span id=""sensorType"">Loading...</span></p>
                    <button id=""relayButton"" class=""btn-on"" onclick=""toggleRelay()"">Loading...</button>
                </div>

                <script>
                    async function fetchData() {
                        try {
                            const response = await fetch('/api/data');
                            const data = await response.json();

                            document.getElementById('temperature').textContent = data.Data.Temp.toFixed(2);
                            document.getElementById('humidity').textContent = data.Data.Humid;

                            // Parsing new DateTime format
                            const dateTime = new Date(data.Data.DateTime);
                            if (!isNaN(dateTime.getTime())) {
                                document.getElementById('date').textContent = dateTime.toLocaleDateString();
                                document.getElementById('time').textContent = dateTime.toLocaleTimeString();
                            } else {
                                document.getElementById('date').textContent = 'Invalid Date';
                                document.getElementById('time').textContent = 'Invalid Time';
                            }


                            const sensorType = data.Data.SensorType ? data.Data.SensorType : 'Not available';
                            document.getElementById('sensorType').textContent = sensorType;

                            fetchRelayStatus();
                        } catch (error) {
                            console.error('Error fetching sensor data:', error);
                        }
                    }

                    async function fetchRelayStatus() {
                        try {
                            const response = await fetch('/api/relay-status');
                            const data = await response.json();
                            updateRelayButton(data.isRelayOn);
                        } catch (error) {
                            console.error('Error fetching relay status:', error);
                        }
                    }

                    async function toggleRelay() {
                        try {
                            const response = await fetch('/api/toggle-relay', { method: 'POST' });
                            const data = await response.json();
                            updateRelayButton(data.isRelayOn);
                        } catch (error) {
                            console.error('Error toggling relay:', error);
                        }
                    }

                    function updateRelayButton(isRelayOn) {
                        const relayButton = document.getElementById('relayButton');
                        if (isRelayOn) {
                            relayButton.textContent = 'Turn Off';
                            relayButton.className = 'btn-off';
                        } else {
                            relayButton.textContent = 'Turn On';
                            relayButton.className = 'btn-on';
                        }
                    }

                    fetchData();
                </script>
            </body>
            </html>";
        }
    }
}
