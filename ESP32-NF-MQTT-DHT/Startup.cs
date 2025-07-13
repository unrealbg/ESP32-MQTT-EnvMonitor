namespace ESP32_NF_MQTT_DHT
{
    using System;

    using ESP32_NF_MQTT_DHT.Configuration;
    using ESP32_NF_MQTT_DHT.Exceptions;
    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Services;
    using ESP32_NF_MQTT_DHT.Services.Contracts;

    /// <summary>
    /// Represents the startup process of the application.
    /// </summary>
    public class Startup
    {
        private readonly IConnectionService _connectionService;
        private readonly IServiceStartupManager _serviceStartupManager;
        private readonly IPlatformService _platformService;

        public Startup(
            IConnectionService connectionService,
            IServiceStartupManager serviceStartupManager,
            IPlatformService platformService)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            _serviceStartupManager = serviceStartupManager ?? throw new ArgumentNullException(nameof(serviceStartupManager));
            _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));

            LogHelper.LogInformation("Initializing application...");
            this.LogPlatformInfo();
        }

        public void Run()
        {
            try
            {
                this.ValidateSystemRequirements();
                this.EstablishConnection();
                this.StartServices();
                
                LogHelper.LogInformation("Application startup completed successfully.");
            }
            catch (InsufficientMemoryException memEx)
            {
                LogHelper.LogError($"Memory validation failed: {memEx.Message}");
                LogService.LogCritical("Insufficient memory for startup", memEx);
                throw;
            }
            catch (ServiceStartupException serviceEx)
            {
                LogHelper.LogError($"Service startup failed: {serviceEx.Message}");
                LogService.LogCritical($"Failed to start service: {serviceEx.ServiceName}", serviceEx);
                throw;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"Unexpected startup failure: {ex.Message}");
                LogService.LogCritical("Critical error during startup", ex);
                throw;
            }
        }

        private void ValidateSystemRequirements()
        {
            LogHelper.LogInformation("Validating system requirements...");
            
            var availableMemory = _platformService.GetAvailableMemory();
            var requiredMemory = StartupConfiguration.RequiredMemory;
            
            if (!_platformService.HasSufficientMemory(requiredMemory))
            {
                throw new InsufficientMemoryException(requiredMemory, availableMemory);
            }
            
            LogHelper.LogInformation($"System requirements validated. Memory: {availableMemory}/{requiredMemory} bytes");
        }

        private void EstablishConnection()
        {
            LogHelper.LogInformation("Establishing connection...");
            
            try
            {
                _connectionService.Connect();
                LogHelper.LogInformation("Connection established successfully.");
            }
            catch (Exception ex)
            {
                throw new ServiceStartupException("ConnectionService", $"Failed to establish connection: {ex.Message}", ex);
            }
        }

        private void StartServices()
        {
            LogHelper.LogInformation("Starting application services...");
            
            try
            {
                _serviceStartupManager.StartAllServices();
                LogHelper.LogInformation("All application services started successfully.");
            }
            catch (Exception ex)
            {
                throw new ServiceStartupException("ServiceStartupManager", $"Failed to start services: {ex.Message}", ex);
            }
        }

        private void LogPlatformInfo()
        {
            LogHelper.LogInformation($"Platform: {_platformService.PlatformName}");
            LogHelper.LogInformation($"Available Memory: {_platformService.GetAvailableMemory()} bytes");
            LogHelper.LogInformation($"WebServer Support: {_platformService.SupportsWebServer()}");
        }
    }
}
