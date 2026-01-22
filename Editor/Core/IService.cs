namespace UnityMCPBridge.Core
{
    /// <summary>
    /// Base interface for all services in the MCP Bridge system.
    /// Provides lifecycle management for services.
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Gets whether the service is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Starts the service and begins collecting/processing data.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the service and releases resources.
        /// </summary>
        void Stop();
    }
}
