using System;

namespace UnityMCPBridge.Server
{
    /// <summary>
    /// Interface for HTTP server that handles MCP requests.
    /// </summary>
    public interface IHttpServer
    {
        /// <summary>
        /// Gets whether the server is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the port the server is listening on.
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Event raised when the server starts.
        /// </summary>
        event Action OnServerStarted;

        /// <summary>
        /// Event raised when the server stops.
        /// </summary>
        event Action OnServerStopped;

        /// <summary>
        /// Event raised when an error occurs.
        /// </summary>
        event Action<string> OnServerError;

        /// <summary>
        /// Starts the HTTP server.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the HTTP server.
        /// </summary>
        void Stop();
    }
}
