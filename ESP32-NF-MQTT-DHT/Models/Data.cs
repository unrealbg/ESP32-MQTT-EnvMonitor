using System;

namespace ESP32_NF_MQTT_DHT.Models
{
    public class Data
    {
        public string ESP32 { get; set; }

        public string Sensorname { get; set; }

        public string Date { get; set; }

        public string Time { get; set; }

        public int Sleep5Count { get; set; }

        public int BootCount { get; set; }

        public int Lux { get; set; }

        public double Temp { get; set; }

        public int Humid { get; set; }

        public int Soil { get; set; }

        public int SoilTemp { get; set; }

        public int Salt { get; set; }

        public string Saltadvice { get; set; }

        public int Bat { get; set; }

        public string Batcharge { get; set; }

        public string BatchargeDate { get; set; }

        public double DaysOnBattery { get; set; }

        public int Battvolt { get; set; }

        public double Battvoltage { get; set; }

        public int Pressure { get; set; }

        public int PlantValveNo { get; set; }

        public string? Wifissid { get; set; }

        public string? Rel { get; set; }

        public DateTime CurrentDate { get; set; }

        public int Messages { get; set; }
    }
}
