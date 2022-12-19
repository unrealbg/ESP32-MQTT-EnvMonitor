# C# nanoframework for ESP32 with DHT21 sensor and MQTT client
# Portal address: https://www.unrealbg.com <br/> user: demo@unrealbg.com , pass: Demo123@
Programming is my hobby, I'd appreciate it if you could help improve the code.

This repository contains C# code for an ESP32 device using nanoframework, a DHT21 sensor for temperature and humidity sensing, and an MQTT client for remote communication. The code allows for real-time monitoring of temperature and humidity data, as well as the ability to publish and subscribe to data over the MQTT protocol.

## **Requirements**
+ ESP32 development board
+ DHT21 sensor
+ MQTT broker (e.g. Mosquitto)

## Setup
1. Install the C# nanoframework on your ESP32 device following the instructions here.
2. Connect the DHT21 sensor to your ESP32 device according to the sensor's documentation.
3. Set up an MQTT broker on your network (e.g. Mosquitto) and make note of the hostname and port number.
4. Replace <YOUR_MQTT_BROKER_HOSTNAME> and <YOUR_MQTT_BROKER_PORT> in the code with the hostname and port number of your MQTT broker.
5. Compile and upload the code to your ESP32 device.

## Usage
Upon successful setup, the ESP32 device will begin monitoring temperature and humidity data from the DHT21 sensor and publishing this data to an MQTT topic. You can subscribe to this topic to receive real-time updates of the data. The code also includes functions for publishing custom messages to the MQTT topic.

## Troubleshooting
+ If you are unable to connect to the MQTT broker, check the hostname and port number in the code to ensure they are correct.
+ If the temperature and humidity readings are inaccurate or unstable, check the connections to the DHT21 sensor and ensure that the sensor is properly calibrated.

<br/>
<br/>
w/o breadboard power supply
<br/>
<img src="https://user-images.githubusercontent.com/3398536/201364419-9ba27b3e-6638-490f-90f5-0e380fbc2900.png" width="400">
<br/>
<br/>
With breadboard power supply
<br/>
<img src="https://user-images.githubusercontent.com/3398536/201362770-067d8fe3-254e-48e2-8cec-10766898c3e6.png" width="400">
<br/>
<br/>
<img src="https://user-images.githubusercontent.com/3398536/200621001-ac09d95d-9f0f-4ef7-bf87-8b352f5f1a17.jpg" width="400">
<br/>


