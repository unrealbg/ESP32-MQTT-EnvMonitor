# ESP32-MQTT Environmental Monitor

## Table of Contents
- [Introduction](#introduction)
- [Requirements](#requirements)
- [Setup](#setup)
- [Usage](#usage)
- [Troubleshooting](#troubleshooting)
- [WebServer with API Endpoints](#webserver-with-api-endpoints)
- [Index Page](#index-page)
- [Relay Control](#relay-control)
- [Additional Functionalities](#additional-functionalities)
- [Project Images](#project-images)
- [Contributing](#contributing)
- [Acknowledgements](#acknowledgements)
- [Changelog](#changelog)
- [License](#license)

## Introduction

This project integrates an ESP32 device with temperature and humidity sensors (DHT21, AHT10, SHTC3) using C# and the nanoFramework for development. It features MQTT client capabilities for real-time remote data communication and a WebServer component that provides RESTful API endpoints for sensor data access and device control via HTTP requests. The system is ideal for IoT applications requiring flexible communication and interaction interfaces.

Note: Only one sensor is active at a time. The sensor (DHT21, AHT10, or SHTC3) must be selected during the initial setup, and they do not work simultaneously.

Visit our demo portal at: [iot.unrealbg.com](https://iot.unrealbg.com)

I acknowledge that the code may not be perfect and there are certainly areas that can be improved. I am a self-taught programmer, and coding is my hobby. I welcome any constructive criticism and feedback.

## Requirements

- ESP32 development board
- DHT21, AHT10, or SHTC3 temperature and humidity sensor
- MQTT broker (e.g., Mosquitto)

## Tested Hardware

This project has been tested and confirmed to work with the following hardware:

### ✅ Supported ESP32 Boards
- ESP32-S3  
- ESP32 Wroom32 DevKit
- ESP32-WROOM-32 DevKit (38 Pins)
- ESP-WROOM-32 ESP-32S
- Wemos/Lolin D32 ESP32

### ❌ Not Working With:
- ESP32-C3 (known compatibility issues)

### ✅ Supported Sensors:
- AHT10  
- AHT20 + BMP280  
- SHTC3  
- DHT21  

If you test the project with other hardware, feel free to contribute feedback!

## Setup

1. **NanoFramework Installation:**  
   Install the C# nanoFramework on your ESP32 device. Detailed instructions are available [here](https://docs.nanoframework.net/content/getting-started-guides/getting-started-managed.html).

2. **Sensor Connection:**  
   Connect the DHT21, AHT10, or SHTC3 sensor to your ESP32 device as per the sensor's documentation. Select the sensor in the code during setup.

3. **MQTT Broker Setup:**  
   Set up an MQTT broker (like Mosquitto) on your network. Note down the hostname and port number.

4. **Code Configuration:**  
   In the code, configure the necessary settings in the `DeviceSettings`, `MqttSettings`, and `WiFiSettings` classes:
   - ***DeviceSettings***: Set values for `DeviceName`, `Location`, and `SensorType`.
   - ***MqttSettings***: Provide the `broker address`, `username`, and `password` for your MQTT broker.
   - ***Wi-Fi Settings***: Enter the `ssid` and `password` for your Wi-Fi network.
     
   These settings are defined in dedicated classes and should be modified according to your environment and setup requirements.

5. **Code Deployment:**  
   Compile and upload the code to your ESP32 device.

## Usage

Once the setup is complete, the ESP32 device will start publishing temperature and humidity data from the selected sensor (DHT21, AHT10, or SHTC3) to an MQTT topic. Subscribe to this topic to receive real-time updates. The system also supports publishing custom messages to the MQTT topic.

## Troubleshooting

- **MQTT Connection Issues:**  
  Ensure the MQTT broker hostname and port number are correctly set in the code.

- **Sensor Data Accuracy:**  
  If temperature and humidity readings are inaccurate, verify the sensor connections and calibrations.

## WebServer with API Endpoints

This project includes a web server that serves API endpoints for structured HTTP request handling. It allows for remote sensor data retrieval and device control via a RESTful interface.

**Note:** For optimal web server performance, it is recommended to use ESP devices with more memory, such as **ESP32-S3**. The project has been tested and works without issues on ESP32-S3, thanks to its increased storage and processing resources.

### WebServer Features

- **API Endpoints**: API endpoints for interacting with the DHT21, AHT10, and SHTC3 sensors and device control.
- **Real-Time Data Access**: Fetch real-time temperature and humidity data.
- **Device Control**: Endpoints for controlling the ESP32 device functionalities.

### API Endpoints

1. **Temperature Data**:  
   - Endpoint: `/api/temperature`  
   - Method: `GET`  
   - Description: Returns the current temperature reading from the selected sensor in JSON format.

2. **Humidity Data**:  
   - Endpoint: `/api/humidity`  
   - Method: `GET`  
   - Description: Returns the current humidity reading in JSON format.

3. **Complete Sensor Data**:  
   - Endpoint: `/api/data`  
   - Method: `GET`  
   - Description: Returns both temperature and humidity readings along with the sensor type in a structured JSON format.

## Index Page

An index page has been added for a user-friendly display of sensor data through a simple web interface. It dynamically updates the temperature, humidity, date, time, and sensor type using JavaScript.

### Index Page Content
The index page provides a clean and responsive UI for real-time monitoring:
- Displays temperature and humidity readings.
- Shows the current date, time, and sensor type.

## New Feature: Relay Control with Toggle Button

The project now includes a relay control feature accessible via the web interface. A button has been added to the index page to allow users to toggle the relay state between **ON** and **OFF**. The button’s label and color dynamically update based on the current relay state.

- **"Turn On"**: Green button appears when the relay is off.
- **"Turn Off"**: Red button appears when the relay is on.

### Relay Control API

You can control the relay using the following API endpoint:

1. **Toggle Relay**  
   - Endpoint: `/api/toggle-relay`  
   - Method: `POST`  
   - Description: Toggles the relay state between ON and OFF. The response returns the updated relay state (`isRelayOn`).

2. **Check Relay Status**  
   - Endpoint: `/api/relay-status`  
   - Method: `GET`  
   - Description: Returns the current state of the relay (ON or OFF) in JSON format.

### Updated Index Page

The web interface has been updated with a new **Relay Control** button. This button will dynamically update based on the state of the relay:

- **Turn On**: Green button displayed when the relay is off.
- **Turn Off**: Red button displayed when the relay is on.

### Example HTML:
```html
<button id="relayButton" class="btn-on" onclick="toggleRelay()">Loading...</button>
async function toggleRelay() {
    const response = await fetch('/api/toggle-relay', { method: 'POST' });
    const data = await response.json();
    updateRelayButton(data.isRelayOn);
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
```

## Additional Functionalities

### Relay Control
You can control a relay module to manage connected devices based on sensor data or MQTT commands. This enables automated environmental control or remote power management.

### SHTC3 Sensor Support
In addition to the DHT21 and AHT10 sensors, the project now supports the SHTC3 sensor, offering flexibility in sensor choice based on precision and calibration needs.

## Project Images

### Without Breadboard Power Supply
![Without Breadboard Power Supply](https://user-images.githubusercontent.com/3398536/201364419-9ba27b3e-6638-490f-90f5-0e380fbc2900.png)

### With Breadboard Power Supply
![With Breadboard Power Supply](https://user-images.githubusercontent.com/3398536/201362770-067d8fe3-254e-48e2-8cec-10766898c3e6.png)

### Additional Project Image
<img src="https://user-images.githubusercontent.com/3398536/200621001-ac09d95d-9f0f-4ef7-bf87-8b352f5f1a17.jpg" width="200" /> <img src="https://github.com/unrealbg/NF.Esp32.Mqtt.Dht21/assets/3398536/4cde056b-9c80-467d-a05f-481e5dae26ea" width="200" /> <img src="https://github.com/unrealbg/NF.Esp32.Mqtt.Dht21/assets/3398536/1e2c80ee-9d03-45d0-8034-ce2004eb4cf1" width="200" />

## Contributing

Contributions are welcome! Please follow these steps to contribute:

1. Fork the repository.
2. Create a new branch (`git checkout -b feature-branch`).
3. Make your changes.
4. Commit your changes (`git commit -m 'Add new feature'`).
5. Push to the branch (`git push origin feature-branch`).
6. Open a Pull Request.

## Acknowledgements

- [nanoFramework](https://www.nanoframework.net/)
- [Mosquitto MQTT](https://mosquitto.org/)
- Special thanks to everyone who contributed to this project.

## Changelog

### [v1.0.1] - 2024-12-13
- Added support for SHTC3 sensor.
- Improved MQTT connection stability.

### [v1.0.0] - 2024-11-01
- Initial release with DHT21 and AHT10 sensors support.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
