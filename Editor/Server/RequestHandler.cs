using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityMCPBridge.Core;

namespace UnityMCPBridge.Server
{
    /// <summary>
    /// Handles HTTP requests and generates appropriate responses.
    /// Single Responsibility: Route requests and format responses.
    /// </summary>
    public sealed class RequestHandler
    {
        private readonly ILogService _logService;
        private readonly ICompilationService _compilationService;
        private readonly IScreenshotService _screenshotService;

        public RequestHandler(ILogService logService, ICompilationService compilationService, IScreenshotService screenshotService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _compilationService = compilationService ?? throw new ArgumentNullException(nameof(compilationService));
            _screenshotService = screenshotService ?? throw new ArgumentNullException(nameof(screenshotService));
        }

        /// <summary>
        /// Handles an HTTP request and returns a response.
        /// </summary>
        /// <param name="method">HTTP method (GET, POST, etc.)</param>
        /// <param name="path">Request path</param>
        /// <param name="body">Request body (for POST requests)</param>
        /// <returns>Tuple of (status code, content type, response body)</returns>
        public (int statusCode, string contentType, string body) HandleRequest(string method, string path, string body)
        {
            try
            {
                return path.ToLowerInvariant() switch
                {
                    "/" or "/status" => HandleStatus(),
                    "/logs" => HandleGetLogs(),
                    "/logs/clear" when method == "POST" => HandleClearLogs(),
                    "/compilation/errors" => HandleGetCompilationErrors(),
                    "/compilation/warnings" => HandleGetCompilationWarnings(),
                    "/compilation/status" => HandleCompilationStatus(),
                    "/editor/play" when method == "POST" => HandlePlay(),
                    "/editor/stop" when method == "POST" => HandleStop(),
                    "/editor/pause" when method == "POST" => HandlePause(),
                    "/editor/refresh" when method == "POST" => HandleRefresh(),
                    "/editor/screenshot" when method == "POST" => HandleScreenshot(body),
                    _ => (404, "application/json", ToJson(new Dictionary<string, object>
                    {
                        ["error"] = "Not found",
                        ["path"] = path
                    }))
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Bridge] Request handler error: {ex}");
                return (500, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                }));
            }
        }

        private (int, string, string) HandleStatus()
        {
            // Use cached state for thread-safe access
            var response = new Dictionary<string, object>
            {
                ["status"] = "running",
                ["unityVersion"] = EditorStateCache.UnityVersion,
                ["projectName"] = EditorStateCache.ProductName,
                ["isPlaying"] = EditorStateCache.IsPlaying,
                ["isPaused"] = EditorStateCache.IsPaused,
                ["isCompiling"] = EditorStateCache.IsCompiling || _compilationService.IsCompiling,
                ["hasCompilationErrors"] = _compilationService.HasErrors,
                ["logCount"] = _logService.GetLogs().Count,
                ["timestamp"] = DateTime.UtcNow.ToString("o")
            };
            return (200, "application/json", ToJson(response));
        }

        private (int, string, string) HandleGetLogs()
        {
            var logs = _logService.GetLogs();
            var response = new Dictionary<string, object>
            {
                ["count"] = logs.Count,
                ["logs"] = logs.Select(l => new Dictionary<string, object>
                {
                    ["message"] = l.Message,
                    ["stackTrace"] = l.StackTrace,
                    ["type"] = l.TypeString,
                    ["timestamp"] = l.Timestamp.ToString("o")
                }).ToArray()
            };
            return (200, "application/json", ToJson(response));
        }

        private (int, string, string) HandleClearLogs()
        {
            _logService.Clear();
            return (200, "application/json", ToJson(new Dictionary<string, object>
            {
                ["success"] = true,
                ["message"] = "Logs cleared"
            }));
        }

        private (int, string, string) HandleGetCompilationErrors()
        {
            var errors = _compilationService.GetErrors();
            var response = new Dictionary<string, object>
            {
                ["count"] = errors.Count,
                ["errors"] = errors.Select(e => new Dictionary<string, object>
                {
                    ["message"] = e.Message,
                    ["file"] = e.File,
                    ["line"] = e.Line,
                    ["column"] = e.Column,
                    ["assembly"] = e.AssemblyName,
                    ["severity"] = e.Severity,
                    ["timestamp"] = e.Timestamp.ToString("o")
                }).ToArray()
            };
            return (200, "application/json", ToJson(response));
        }

        private (int, string, string) HandleGetCompilationWarnings()
        {
            var warnings = _compilationService.GetWarnings();
            var response = new Dictionary<string, object>
            {
                ["count"] = warnings.Count,
                ["warnings"] = warnings.Select(w => new Dictionary<string, object>
                {
                    ["message"] = w.Message,
                    ["file"] = w.File,
                    ["line"] = w.Line,
                    ["column"] = w.Column,
                    ["assembly"] = w.AssemblyName,
                    ["severity"] = w.Severity,
                    ["timestamp"] = w.Timestamp.ToString("o")
                }).ToArray()
            };
            return (200, "application/json", ToJson(response));
        }

        private (int, string, string) HandleCompilationStatus()
        {
            var response = new Dictionary<string, object>
            {
                ["isCompiling"] = _compilationService.IsCompiling,
                ["hasErrors"] = _compilationService.HasErrors,
                ["hasWarnings"] = _compilationService.HasWarnings,
                ["errorCount"] = _compilationService.GetErrors().Count,
                ["warningCount"] = _compilationService.GetWarnings().Count
            };
            return (200, "application/json", ToJson(response));
        }

        private (int, string, string) HandlePlay()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.EnterPlaymode();
                return (200, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["message"] = "Entering play mode"
                }));
            }
            return (200, "application/json", ToJson(new Dictionary<string, object>
            {
                ["success"] = false,
                ["message"] = "Already in play mode"
            }));
        }

        private (int, string, string) HandleStop()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return (200, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["message"] = "Exiting play mode"
                }));
            }
            return (200, "application/json", ToJson(new Dictionary<string, object>
            {
                ["success"] = false,
                ["message"] = "Not in play mode"
            }));
        }

        private (int, string, string) HandlePause()
        {
            EditorApplication.isPaused = !EditorApplication.isPaused;
            return (200, "application/json", ToJson(new Dictionary<string, object>
            {
                ["success"] = true,
                ["isPaused"] = EditorApplication.isPaused,
                ["message"] = EditorApplication.isPaused ? "Paused" : "Resumed"
            }));
        }

        private (int, string, string) HandleRefresh()
        {
            AssetDatabase.Refresh();
            return (200, "application/json", ToJson(new Dictionary<string, object>
            {
                ["success"] = true,
                ["message"] = "Asset database refreshed"
            }));
        }

        private (int, string, string) HandleScreenshot(string body)
        {
            // Parse request body for parameters
            var view = ScreenshotView.Game;
            var quality = ScreenshotQuality.Low;

            if (!string.IsNullOrEmpty(body))
            {
                try
                {
                    // Simple JSON parsing for view and quality parameters
                    if (body.Contains("\"view\""))
                    {
                        if (body.Contains("\"scene\"", StringComparison.OrdinalIgnoreCase))
                        {
                            view = ScreenshotView.Scene;
                        }
                    }
                    if (body.Contains("\"quality\""))
                    {
                        if (body.Contains("\"medium\"", StringComparison.OrdinalIgnoreCase))
                        {
                            quality = ScreenshotQuality.Medium;
                        }
                        else if (body.Contains("\"high\"", StringComparison.OrdinalIgnoreCase))
                        {
                            quality = ScreenshotQuality.High;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MCP Bridge] Failed to parse screenshot parameters: {ex.Message}");
                }
            }

            var result = _screenshotService.CaptureScreenshot(view, quality);

            if (!result.Success)
            {
                return (500, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = result.ErrorMessage
                }));
            }

            return (200, "application/json", ToJson(new Dictionary<string, object>
            {
                ["success"] = true,
                ["filePath"] = result.FilePath,
                ["width"] = result.Width,
                ["height"] = result.Height,
                ["fileSize"] = result.FileSize,
                ["estimatedTokens"] = result.EstimatedTokens,
                ["view"] = view.ToString().ToLowerInvariant(),
                ["quality"] = quality.ToString().ToLowerInvariant(),
                ["timestamp"] = result.Timestamp.ToString("o")
            }));
        }

        private static string ToJson(object obj) => JsonSerializer.Serialize(obj);
    }
}
