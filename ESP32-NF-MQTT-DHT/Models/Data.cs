namespace ESP32_NF_MQTT_DHT.Models
{
    using System;

    using Constants;

    public class Data
    {
        public string Sensorname => "Esp32NF";

        public string Date { get; set; }

        public string Time { get; set; }

        public double Temp { get; set; }

        public int Humid { get; set; }

        public string Wifissid => Constants.SSID;

        public string Rel => "1.0 Init";
    }
}
