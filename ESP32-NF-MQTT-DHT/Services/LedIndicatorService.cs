namespace ESP32_NF_MQTT_DHT.Services
{
    using System.Drawing;
    using System.Threading;

    using Iot.Device.Ws28xx.Esp32;

    /// <summary>
    /// ONLY FOR ESP32-S3!!!
    /// Manages an LED indicator, allowing it to be started, stopped, and set to a specific color with adjustable
    /// brightness.
    /// Runs on a separate thread to update the LED state.
    /// </summary>
    public class LedIndicatorService
    {
        private readonly Ws2812c _ws2812;
        private Thread _ledThread;
        private bool _isRunning;
        private Color _currentColor = Color.Black;

        private readonly float _brightness;

        public LedIndicatorService(int ledPin, float brightness = 0.2f)
        {
            _brightness = brightness;
            _ws2812 = new Ws2812c(ledPin, 1);
        }

        /// <summary>
        /// Starts the LED thread if it is not already running. Sets the running state to true and initiates the thread
        /// execution.
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _ledThread = new Thread(this.Run);
            _ledThread.Start();
        }

        /// <summary>
        /// Stops the running process by setting the running state to false. Changes the color to black.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            this.SetColor(Color.Black);
        }

        /// <summary>
        /// Sets the current color and updates the display with the adjusted brightness.
        /// </summary>
        /// <param name="color">Specifies the color to be adjusted and set on the display.</param>
        public void SetColor(Color color)
        {
            _currentColor = this.AdjustBrightness(color, _brightness);
            _ws2812.Image.SetPixel(0, 0, _currentColor);
            _ws2812.Update();
        }

        private void Run()
        {
            while (_isRunning)
            {
                this.SetColor(_currentColor);
                Thread.Sleep(500);
            }
        }

        private Color AdjustBrightness(Color color, float brightness)
        {
            brightness = brightness < 0 ? 0 : brightness > 1 ? 1 : brightness;

            return Color.FromArgb(
                (byte)(color.R * brightness),
                (byte)(color.G * brightness),
                (byte)(color.B * brightness));
        }
    }
}
