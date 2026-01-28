using System;
using UnityEditor;
using UnityEngine;
using UnityMCPBridge.Server;
using UnityMCPBridge.Services;
using UnityMCPBridge.Settings;

namespace UnityMCPBridge.Core
{
    /// <summary>
    /// Main entry point and service locator for MCP Bridge.
    /// Manages lifecycle of all services and the HTTP server.
    /// </summary>
    [InitializeOnLoad]
    public static class MCPBridge
    {
        private static ILogService _logService;
        private static ICompilationService _compilationService;
        private static IScreenshotService _screenshotService;
        private static IHttpServer _httpServer;
        private static bool _initialized;

        /// <summary>
        /// Gets the log service instance.
        /// </summary>
        public static ILogService LogService => _logService;

        /// <summary>
        /// Gets the compilation service instance.
        /// </summary>
        public static ICompilationService CompilationService => _compilationService;

        /// <summary>
        /// Gets the screenshot service instance.
        /// </summary>
        public static IScreenshotService ScreenshotService => _screenshotService;

        /// <summary>
        /// Gets the HTTP server instance.
        /// </summary>
        public static IHttpServer HttpServer => _httpServer;

        /// <summary>
        /// Gets whether the bridge is fully initialized.
        /// </summary>
        public static bool IsInitialized => _initialized;

        /// <summary>
        /// Gets whether all services are running.
        /// </summary>
        public static bool IsRunning => _httpServer?.IsRunning ?? false;

        /// <summary>
        /// Event raised when the bridge starts.
        /// </summary>
        public static event Action OnBridgeStarted;

        /// <summary>
        /// Event raised when the bridge stops.
        /// </summary>
        public static event Action OnBridgeStopped;

        static MCPBridge()
        {
            // Defer initialization to avoid issues during domain reload
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            if (_initialized) return;

            // Create services
            _logService = new LogService();
            _compilationService = new CompilationService();
            _screenshotService = new ScreenshotService();
            _httpServer = new HttpServer(_logService, _compilationService, _screenshotService);

            // Subscribe to domain unload for cleanup
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
            EditorApplication.quitting += OnEditorQuitting;

            _initialized = true;
            Debug.Log("[MCP Bridge] Initialized");

            // Auto-start if enabled
            if (MCPBridgeSettings.AutoStart)
            {
                Start();
            }
        }

        /// <summary>
        /// Starts all services and the HTTP server.
        /// </summary>
        public static void Start()
        {
            if (!_initialized)
            {
                Debug.LogError("[MCP Bridge] Cannot start - not initialized");
                return;
            }

            if (IsRunning)
            {
                Debug.LogWarning("[MCP Bridge] Already running");
                return;
            }

            _logService.Start();
            _compilationService.Start();
            _screenshotService.Start();
            _httpServer.Start();

            OnBridgeStarted?.Invoke();
        }

        /// <summary>
        /// Stops all services and the HTTP server.
        /// </summary>
        public static void Stop()
        {
            if (!IsRunning) return;

            _httpServer?.Stop();
            _screenshotService?.Stop();
            _compilationService?.Stop();
            _logService?.Stop();

            OnBridgeStopped?.Invoke();
        }

        private static void OnDomainUnload(object sender, EventArgs e)
        {
            Cleanup();
        }

        private static void OnEditorQuitting()
        {
            Cleanup();
        }

        private static void Cleanup()
        {
            Stop();
            
            if (_httpServer is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _httpServer = null;
            _screenshotService = null;
            _compilationService = null;
            _logService = null;
            _initialized = false;
        }
    }
}
