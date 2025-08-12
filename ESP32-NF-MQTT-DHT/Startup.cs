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
        private static System.Security.Cryptography.X509Certificates.X509Certificate _otaRootCaCert;

        /// <summary>
        /// Loads the OTA server root CA certificate for TLS.
        /// </summary>
        private static void LoadOtaRootCa()
        {
            try
            {
                // Try common device storage locations (internal flash is typically I:\ on nanoFramework)
                string[] candidates = new string[]
                {
                    @"I:\\ota_root_ca.pem",
                    @"I:\\Settings\\ota_root_ca.pem"
                };

                string caPath = null;
                for (int i = 0; i < candidates.Length; i++)
                {
                    try
                    {
                        if (System.IO.File.Exists(candidates[i]))
                        {
                            caPath = candidates[i];
                            break;
                        }
                    }
                    catch
                    {
                        /* ignore invalid path exceptions */
                    }
                }

                if (caPath != null)
                {
                    // Parse PEM file and select a root (self-signed) CA certificate
                    string pemText = System.IO.File.ReadAllText(caPath);
                    const string begin = "-----BEGIN CERTIFICATE-----";
                    const string end = "-----END CERTIFICATE-----";

                    System.Security.Cryptography.X509Certificates.X509Certificate selected = null;
                    int idx = 0;
                    int total = 0;
                    while (true)
                    {
                        int start = pemText.IndexOf(begin, idx);
                        if (start < 0) break;
                        int stop = pemText.IndexOf(end, start);
                        if (stop < 0) break;
                        stop += end.Length;

                        string block = pemText.Substring(start, stop - start);
                        total++;
                        var cert = new System.Security.Cryptography.X509Certificates.X509Certificate(block);
                        // Prefer self-signed (Issuer == Subject) as root CA
                        try
                        {
                            if (cert.Issuer == cert.Subject)
                            {
                                selected = cert;
                                break;
                            }
                        }
                        catch
                        {
                            // ignored
                        }

                        if (selected == null)
                        {
                            selected = cert; // fallback to first found
                        }

                        idx = stop;
                    }

                    _otaRootCaCert = selected;
                    if (_otaRootCaCert != null)
                    {
                        LogHelper.LogInformation($"TLS CA prepared from '{caPath}'. Found {total} cert(s); using: '{_otaRootCaCert.Subject}' issued by '{_otaRootCaCert.Issuer}'.");
                    }
                    else
                    {
                        LogHelper.LogWarning("No usable certificate found in PEM. HTTPS may fail.");
                    }
                }
                else
                {
                    // Fallback to embedded certificate(s) shipped with firmware
                    string pemText = Settings.OtaCertificates.RootCaPem;
                    if (!string.IsNullOrEmpty(pemText))
                    {
                        const string begin = "-----BEGIN CERTIFICATE-----";
                        const string end = "-----END CERTIFICATE-----";

                        System.Security.Cryptography.X509Certificates.X509Certificate selected = null;
                        int idx = 0;
                        int total = 0;
                        while (true)
                        {
                            int start = pemText.IndexOf(begin, idx);
                            if (start < 0)
                            {
                                break;
                            }

                            int stop = pemText.IndexOf(end, start);
                            if (stop < 0)
                            {
                                break;
                            }

                            stop += end.Length;

                            string block = pemText.Substring(start, stop - start);
                            total++;
                            var cert = new System.Security.Cryptography.X509Certificates.X509Certificate(block);
                            try
                            {
                                if (cert.Issuer == cert.Subject)
                                {
                                    selected = cert;
                                    break;
                                }
                            }
                            catch
                            {
                                // ignored
                            }

                            if (selected == null)
                            {
                                selected = cert;
                            }

                            idx = stop;
                        }

                        _otaRootCaCert = selected;
                        if (_otaRootCaCert != null)
                        {
                            LogHelper.LogInformation($"TLS CA prepared from embedded PEM. Found {total} cert(s); using: '{_otaRootCaCert.Subject}' issued by '{_otaRootCaCert.Issuer}'.");
                        }
                        else
                        {
                            LogHelper.LogWarning("Embedded PEM present but no usable certificate found. HTTPS may fail.");
                        }
                    }
                    else
                    {
                        LogHelper.LogWarning("Root CA PEM file not found on device and no embedded PEM present. Place it at I:\\ota_root_ca.pem.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Failed to load OTA root CA: " + ex.Message);
            }
        }

        /// <summary>
        /// Gets the prepared OTA root CA certificate (may be null).
        /// </summary>
        public static System.Security.Cryptography.X509Certificates.X509Certificate OtaRootCaCert => _otaRootCaCert;

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
            // Load OTA root CA for TLS before any HTTPS requests
            LoadOtaRootCa();
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
            catch (System.Exception ex)
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
