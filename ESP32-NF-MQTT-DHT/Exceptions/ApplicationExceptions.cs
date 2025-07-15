namespace ESP32_NF_MQTT_DHT.Exceptions
{
    using System;

    /// <summary>
    /// Exception thrown when there is insufficient memory for an operation.
    /// </summary>
    public class InsufficientMemoryException : Exception
    {
        public long RequiredMemory { get; }
        public long AvailableMemory { get; }

        public InsufficientMemoryException(long requiredMemory, long availableMemory)
            : base($"Insufficient memory. Required: {requiredMemory} bytes, Available: {availableMemory} bytes")
        {
            this.RequiredMemory = requiredMemory;
            this.AvailableMemory = availableMemory;
        }

        public InsufficientMemoryException(long requiredMemory, long availableMemory, Exception innerException)
            : base($"Insufficient memory. Required: {requiredMemory} bytes, Available: {availableMemory} bytes", innerException)
        {
            this.RequiredMemory = requiredMemory;
            this.AvailableMemory = availableMemory;
        }
    }

    /// <summary>
    /// Exception thrown when a service fails to start.
    /// </summary>
    public class ServiceStartupException : Exception
    {
        public string ServiceName { get; }

        public ServiceStartupException(string serviceName, string message)
            : base($"Failed to start service '{serviceName}': {message}")
        {
            this.ServiceName = serviceName;
        }

        public ServiceStartupException(string serviceName, string message, Exception innerException)
            : base($"Failed to start service '{serviceName}': {message}", innerException)
        {
            this.ServiceName = serviceName;
        }
    }

    /// <summary>
    /// Exception thrown when a platform operation is not supported.
    /// </summary>
    public class PlatformNotSupportedException : Exception
    {
        public string PlatformName { get; }
        public string Operation { get; }

        public PlatformNotSupportedException(string platformName, string operation)
            : base($"Operation '{operation}' is not supported on platform '{platformName}'")
        {
            this.PlatformName = platformName;
            this.Operation = operation;
        }

        public PlatformNotSupportedException(string platformName, string operation, Exception innerException)
            : base($"Operation '{operation}' is not supported on platform '{platformName}'", innerException)
        {
            this.PlatformName = platformName;
            this.Operation = operation;
        }
    }
}