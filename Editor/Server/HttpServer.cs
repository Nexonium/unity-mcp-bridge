using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityMCPBridge.Core;
using UnityMCPBridge.Settings;

namespace UnityMCPBridge.Server
{
    /// <summary>
    /// HTTP server that listens for requests from MCP clients.
    /// Uses a hybrid approach: read-only requests are handled directly,
    /// while actions requiring main thread use a command queue.
    /// </summary>
    public sealed class HttpServer : IHttpServer, IDisposable
    {
        private HttpListener _listener;
        private Thread _listenerThread;
        private volatile bool _isRunning;
        private readonly RequestHandler _requestHandler;
        
        // Command queue for main-thread operations
        private readonly ConcurrentQueue<PendingCommand> _commandQueue = new();
        
        public bool IsRunning => _isRunning;
        public int Port { get; private set; }

        public event Action OnServerStarted;
        public event Action OnServerStopped;
        public event Action<string> OnServerError;

        public HttpServer(ILogService logService, ICompilationService compilationService)
        {
            _requestHandler = new RequestHandler(logService, compilationService);
            Port = MCPBridgeSettings.Port;
        }

        public void Start()
        {
            if (_isRunning)
            {
                Debug.LogWarning("[MCP Bridge] Server is already running");
                return;
            }

            Port = MCPBridgeSettings.Port;

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
                _listener.Start();

                _isRunning = true;
                _listenerThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "MCP Bridge HTTP Server"
                };
                _listenerThread.Start();

                // Subscribe to editor update for processing command queue
                EditorApplication.update += ProcessCommandQueue;

                Debug.Log($"[MCP Bridge] HTTP server started on port {Port}");
                OnServerStarted?.Invoke();
            }
            catch (Exception ex)
            {
                _isRunning = false;
                var errorMessage = $"Failed to start HTTP server on port {Port}: {ex.Message}";
                Debug.LogError($"[MCP Bridge] {errorMessage}");
                OnServerError?.Invoke(errorMessage);
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            EditorApplication.update -= ProcessCommandQueue;

            try
            {
                _listener?.Stop();
                _listener?.Close();
                _listenerThread?.Join(1000);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCP Bridge] Error stopping server: {ex.Message}");
            }
            finally
            {
                _listener = null;
                _listenerThread = null;
                
                // Clear pending commands and signal waiting threads
                while (_commandQueue.TryDequeue(out var cmd))
                {
                    try
                    {
                        cmd.WaitHandle?.Set();
                    }
                    catch (ObjectDisposedException)
                    {
                        // WaitHandle may already be disposed by the requesting thread
                    }
                }
                
                Debug.Log("[MCP Bridge] HTTP server stopped");
                OnServerStopped?.Invoke();
            }
        }

        private void ListenLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch (HttpListenerException) when (!_isRunning)
                {
                    // Expected when stopping the server
                }
                catch (ObjectDisposedException) when (!_isRunning)
                {
                    // Expected when stopping the server
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Debug.LogError($"[MCP Bridge] Listener error: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Processes pending commands on the main thread.
        /// Called by EditorApplication.update.
        /// </summary>
        private void ProcessCommandQueue()
        {
            // Process up to 10 commands per frame to avoid blocking
            var processed = 0;
            while (processed < 10 && _commandQueue.TryDequeue(out var command))
            {
                try
                {
                    var result = _requestHandler.HandleRequest(
                        command.Method,
                        command.Path,
                        command.Body
                    );
                    command.StatusCode = result.statusCode;
                    command.ContentType = result.contentType;
                    command.ResponseBody = result.body;
                }
                catch (Exception ex)
                {
                    command.StatusCode = 500;
                    command.ContentType = "application/json";
                    command.ResponseBody = JsonSerializer.Serialize(new { error = ex.Message });
                }
                finally
                {
                    var handle = command.WaitHandle;
                    if (handle != null)
                    {
                        // Signal completion and dispose the handle (we own its lifecycle)
                        try
                        {
                            handle.Set();
                        }
                        catch (ObjectDisposedException)
                        {
                            // Should not happen anymore, but keep as safety net
                        }
                        finally
                        {
                            handle.Dispose();
                        }
                    }
                    processed++;
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                // Add CORS headers
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                // Handle preflight
                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 204;
                    response.Close();
                    return;
                }

                // Read request body
                string body = null;
                if (request.HasEntityBody)
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    body = reader.ReadToEnd();
                }

                var path = request.Url.AbsolutePath.ToLowerInvariant();
                
                int statusCode;
                string contentType;
                string responseBody;

                // Determine if this request can be handled directly or needs main thread
                if (RequiresMainThread(request.HttpMethod, path))
                {
                    // Queue for main thread processing
                    // Note: WaitHandle is NOT disposed here - main thread owns its lifecycle
                    var waitHandle = new ManualResetEvent(false);
                    var command = new PendingCommand
                    {
                        Method = request.HttpMethod,
                        Path = request.Url.AbsolutePath,
                        Body = body,
                        WaitHandle = waitHandle
                    };

                    _commandQueue.Enqueue(command);

                    // Wait for processing (with timeout)
                    if (!waitHandle.WaitOne(10000))
                    {
                        // Timeout - mark handle as timed out so main thread knows to dispose it
                        command.TimedOut = true;
                        statusCode = 504;
                        contentType = "application/json";
                        responseBody = JsonSerializer.Serialize(new { 
                            error = "Request timeout - Unity Editor may be busy or not in focus"
                        });
                    }
                    else
                    {
                        statusCode = command.StatusCode;
                        contentType = command.ContentType;
                        responseBody = command.ResponseBody;
                    }
                }
                else
                {
                    // Handle directly in background thread (read-only operations)
                    try
                    {
                        var result = _requestHandler.HandleRequest(
                            request.HttpMethod,
                            request.Url.AbsolutePath,
                            body
                        );
                        statusCode = result.statusCode;
                        contentType = result.contentType;
                        responseBody = result.body;
                    }
                    catch (Exception ex)
                    {
                        statusCode = 500;
                        contentType = "application/json";
                        responseBody = JsonSerializer.Serialize(new { error = ex.Message });
                    }
                }

                // Send response
                response.StatusCode = statusCode;
                response.ContentType = contentType ?? "application/json";

                var buffer = Encoding.UTF8.GetBytes(responseBody ?? "{}");
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Bridge] Request handling error: {ex.Message}");
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch { /* Ignore */ }
            }
        }

        /// <summary>
        /// Determines if a request requires main thread processing.
        /// Read-only operations can run in background, actions need main thread.
        /// </summary>
        private static bool RequiresMainThread(string method, string path)
        {
            // POST requests typically modify state and need main thread
            if (method == "POST") return true;

            // These paths involve Unity API that may need main thread
            return path switch
            {
                // Status needs EditorApplication.isPlaying which should be safe, but let's be careful
                "/" or "/status" => false,
                "/logs" => false,
                "/compilation/errors" => false,
                "/compilation/warnings" => false,
                "/compilation/status" => false,
                _ => true
            };
        }

        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Represents a pending command waiting to be processed on main thread.
        /// </summary>
        private class PendingCommand
        {
            public string Method;
            public string Path;
            public string Body;
            public ManualResetEvent WaitHandle;
            public int StatusCode;
            public string ContentType;
            public string ResponseBody;
            public volatile bool TimedOut;
        }
    }
}
