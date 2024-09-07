# ESP32-MQTT Environmental Monitor

This project showcases the integration of an ESP32 device with DHT21 and AHT10 temperature and humidity sensors, employing C# and the nanoFramework for development. Featuring MQTT client capabilities, it facilitates remote data communication, allowing for real-time monitoring and data publishing over MQTT. Additionally, the project includes a WebServer component, providing RESTful API endpoints for direct sensor data access and device control via HTTP requests. This dual functionality enhances the project's ability to interact with sensors and control device operations remotely, making it an ideal framework for IoT applications that require a versatile communication interface.

Visit our portal at: [portal.unrealbg.com](http://portal.unrealbg.com)  
**Username:** demo@unrealbg.com  
**Password:** Demo123@

As a programming enthusiast, I welcome any feedback, guidance, or suggestions for improvement.

## Requirements

- ESP32 development board
- DHT21 or AHT10 temperature and humidity sensor
- MQTT broker (e.g., Mosquitto)

## Setup

1. **NanoFramework Installation:**  
   Install the C# nanoFramework on your ESP32 device. Detailed instructions are available [here](https://docs.nanoframework.net/content/getting-started-guides/getting-started-managed.html).

2. **Sensor Connection:**  
   Connect the DHT21/AHT10 sensor to your ESP32 device as per the sensor's documentation.

3. **MQTT Broker Setup:**  
   Set up an MQTT broker (like Mosquitto) on your network. Note down the hostname and port number.

4. **Code Configuration:**  
   In the code, replace `<YOUR_MQTT_BROKER_HOSTNAME>` and `<YOUR_MQTT_BROKER_PORT>` with your MQTT broker's hostname and port number.

5. **Code Deployment:**  
   Compile and upload the code to your ESP32 device.

## Usage

After completing the setup, the ESP32 device will start monitoring and publishing temperature and humidity data from the DHT21/AHT10 sensor to an MQTT topic. Subscribe to this topic to receive real-time updates. The implementation also supports publishing custom messages to the MQTT topic.

## Troubleshooting

- **MQTT Connection Issues:**  
  Ensure the MQTT broker hostname and port number in the code are correct if connection issues arise.

- **Sensor Data Accuracy:**  
  If temperature and humidity readings are inaccurate or unstable, verify the DHT21/AHT10 sensor's connections and calibration.

## WebServer with Controllers

This project now includes a WebServer that serves API endpoints, making use of Controllers for structured handling of HTTP requests. This enhancement allows for remote interaction with the ESP32 device, offering a RESTful interface to access sensor data and control device functionalities.

### WebServer Features

- **API Endpoints**: The WebServer exposes several API endpoints to interact with the DHT21/AHT10 sensor and other functionalities.
- **RESTful Design**: Adhering to REST principles, it enables easy integration with various client-side applications or services.
- **Real-Time Data Access**: Offers endpoints to fetch real-time temperature and humidity data from the DHT21/AHT10 sensor.
- **Device Control**: Additional endpoints provide control over certain functionalities of the ESP32 device.

### API Endpoints Usage

1. **Temperature Data**:  
   - Endpoint: `/api/temperature`  
   - Method: `GET`  
   - Description: Returns the current temperature reading from the DHT21/AHT10 sensor in JSON format.

2. **Humidity Data**:  
   - Endpoint: `/api/humidity`  
   - Method: `GET`  
   - Description: Returns the current humidity reading from the DHT21/AHT10 sensor in JSON format.

3. **Sensor Data**:  
   - Endpoint: `/api/data`  
   - Method: `GET`  
   - Description: Returns both temperature and humidity readings in a structured JSON format.

### Integration

The WebServer is designed to be scalable and easily integrable with other systems, providing a seamless interface for data communication and device control. This makes it an ideal solution for IoT applications requiring real-time sensor data monitoring and device management over HTTP.

## Additional Functionalities

### Relay Control
This project now includes the ability to control a relay module, enabling the ESP32 to manage power to connected devices. You can switch devices on or off based on sensor data or through MQTT commands, allowing for automated environmental control or remote power management.

### AHT10 Sensor Support
Alongside the DHT21 sensor, the ESP32 is now compatible with the AHT10 sensor. This provides flexibility in choosing the sensor based on your precision and calibration needs for temperature and humidity monitoring.

## Project Images

### Without Breadboard Power Supply
![Without Breadboard Power Supply](https://user-images.githubusercontent.com/3398536/201364419-9ba27b3e-6638-490f-90f5-0e380fbc2900.png)

### With Breadboard Power Supply
![With Breadboard Power Supply](https://user-images.githubusercontent.com/3398536/201362770-067d8fe3-254e-48e2-8cec-10766898c3e6.png)

### Additional Project Image
<img src="https://user-images.githubusercontent.com/3398536/200621001-ac09d95d-9f0f-4ef7-bf87-8b352f5f1a17.jpg" width="200" /> <img src="https://github.com/unrealbg/NF.Esp32.Mqtt.Dht21/assets/3398536/4cde056b-9c80-467d-a05f-481e5dae26ea" width="200" /> <img src="https://github.com/unrealbg/NF.Esp32.Mqtt.Dht21/assets/3398536/1e2c80ee-9d03-45d0-8034-ce2004eb4cf1" width="200" />
