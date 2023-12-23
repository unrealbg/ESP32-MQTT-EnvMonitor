using ESP32_NF_MQTT_DHT.Services.Contracts;

using nanoFramework.WebServer;

namespace ESP32_NF_MQTT_DHT.Services
{
    public class WebServerService : IWebServerService
    {
        private WebServer server;
        private readonly IDhtService _dhtService;

        public WebServerService(int port, IDhtService dhtService)
        {
            _dhtService = dhtService;
            server = new WebServer(port, HttpProtocol.Http);
            server.CommandReceived += ServerCommandReceived;
        }

        private void ServerCommandReceived(object sender, WebServerEventArgs e)
        {
            switch (e.Context.Request.RawUrl)
            {
                case "/api/temperature":
                    var temperature = _dhtService.GetTemp();
                    WebServer.OutPutStream(e.Context.Response, temperature);
                    break;
                case "/api/humidity":
                    var humidity = _dhtService.GetHumidity();
                    WebServer.OutPutStream(e.Context.Response, humidity);
                    break;
            }
        }

        public void Start()
        {
            server.Start();
        }
    }
}
