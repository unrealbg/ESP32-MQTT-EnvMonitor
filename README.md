# C# NanoFramework for ESP32 with DHT21 Sensor and MQTT Client

This project demonstrates the integration of an ESP32 device with a DHT21 temperature and humidity sensor, using C# and the nanoFramework. It includes MQTT client capabilities for remote data communication, enabling real-time monitoring and data publishing over MQTT.

Visit our portal at: [portal.unrealbg.com](http://portal.unrealbg.com)  
**Username:** demo@unrealbg.com  
**Password:** Demo123@

As a programming enthusiast, I welcome any feedback, guidance, or suggestions for improvement.

## Requirements

- ESP32 development board
- DHT21 temperature and humidity sensor
- MQTT broker (e.g., Mosquitto)

## Setup

1. **NanoFramework Installation:**  
   Install the C# nanoFramework on your ESP32 device. Detailed instructions are available [here](https://docs.nanoframework.net/content/getting-started-guides/getting-started-managed.html).

2. **Sensor Connection:**  
   Connect the DHT21 sensor to your ESP32 device as per the sensor's documentation.

3. **MQTT Broker Setup:**  
   Set up an MQTT broker (like Mosquitto) on your network. Note down the hostname and port number.

4. **Code Configuration:**  
   In the code, replace `<YOUR_MQTT_BROKER_HOSTNAME>` and `<YOUR_MQTT_BROKER_PORT>` with your MQTT broker's hostname and port number.

5. **Code Deployment:**  
   Compile and upload the code to your ESP32 device.

## Usage

After completing the setup, the ESP32 device will start monitoring and publishing temperature and humidity data from the DHT21 sensor to an MQTT topic. Subscribe to this topic to receive real-time updates. The implementation also supports publishing custom messages to the MQTT topic.

## Troubleshooting

- **MQTT Connection Issues:**  
  Ensure the MQTT broker hostname and port number in the code are correct if connection issues arise.

- **Sensor Data Accuracy:**  
  If temperature and humidity readings are inaccurate or unstable, verify the DHT21 sensor's connections and calibration.

## Project Images

### Without Breadboard Power Supply
![Without Breadboard Power Supply](https://user-images.githubusercontent.com/3398536/201364419-9ba27b3e-6638-490f-90f5-0e380fbc2900.png)

### With Breadboard Power Supply
![With Breadboard Power Supply](https://user-images.githubusercontent.com/3398536/201362770-067d8fe3-254e-48e2-8cec-10766898c3e6.png)

### Additional Project Image
![Project Image](https://user-images.githubusercontent.com/3398536/200621001-ac09d95d-9f0f-4ef7-bf87-8b352f5f1a17.jpg)
