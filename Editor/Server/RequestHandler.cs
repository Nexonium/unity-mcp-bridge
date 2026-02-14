using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
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
                    "/editor/open-asset" when method == "POST" => HandleOpenAsset(body),
                    "/editor/select-object" when method == "POST" => HandleSelectObject(body),
                    "/editor/frame-selected" when method == "POST" => HandleFrameSelected(),
                    "/editor/hierarchy" => HandleGetHierarchy(body),
                    "/terminal/execute" when method == "POST" => HandleTerminalExecute(body),
                    "/terminal/execute-batch" when method == "POST" => HandleTerminalExecuteBatch(body),
                    "/terminal/status" => HandleTerminalStatus(),
                    "/terminal/commands" => HandleTerminalCommands(body),
                    "/terminal/logs" => HandleTerminalLogs(body),
                    "/terminal/history" => HandleTerminalHistory(body),
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

        private (int, string, string) HandleOpenAsset(string body)
        {
            if (string.IsNullOrEmpty(body) || !body.Contains("\"path\""))
            {
                return (400, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "Missing 'path' parameter"
                }));
            }

            // Extract path from JSON body
            var pathMatch = System.Text.RegularExpressions.Regex.Match(body, "\"path\"\\s*:\\s*\"([^\"]+)\"");
            if (!pathMatch.Success)
            {
                return (400, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "Invalid 'path' parameter"
                }));
            }

            var assetPath = pathMatch.Groups[1].Value;
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);

            if (asset == null)
            {
                return (404, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = $"Asset not found: {assetPath}"
                }));
            }

            // Open the asset (prefab opens in Prefab Mode, scene opens as scene, etc.)
            AssetDatabase.OpenAsset(asset);

            return (200, "application/json", ToJson(new Dictionary<string, object>
            {
                ["success"] = true,
                ["message"] = $"Opened asset: {assetPath}",
                ["assetType"] = asset.GetType().Name
            }));
        }

        private (int, string, string) HandleSelectObject(string body)
        {
            if (string.IsNullOrEmpty(body))
            {
                return (400, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "Missing request body"
                }));
            }

            // Check if we're in Prefab Mode
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            var inPrefabMode = prefabStage != null;

            // Try to find by path first
            var pathMatch = System.Text.RegularExpressions.Regex.Match(body, "\"path\"\\s*:\\s*\"([^\"]+)\"");
            if (pathMatch.Success)
            {
                var objectPath = pathMatch.Groups[1].Value;
                GameObject foundObject = null;

                if (inPrefabMode)
                {
                    // In Prefab Mode, search from prefab root
                    var root = prefabStage.prefabContentsRoot;
                    if (root.name == objectPath || objectPath == "/")
                    {
                        foundObject = root;
                    }
                    else
                    {
                        // Remove leading slash and root name if present
                        var searchPath = objectPath.TrimStart('/');
                        if (searchPath.StartsWith(root.name + "/"))
                        {
                            searchPath = searchPath.Substring(root.name.Length + 1);
                        }
                        var childTransform = root.transform.Find(searchPath);
                        foundObject = childTransform?.gameObject;
                    }
                }
                else
                {
                    foundObject = GameObject.Find(objectPath);
                }

                if (foundObject != null)
                {
                    Selection.activeGameObject = foundObject;
                    return (200, "application/json", ToJson(new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["message"] = $"Selected object: {foundObject.name}",
                        ["objectName"] = foundObject.name,
                        ["inPrefabMode"] = inPrefabMode
                    }));
                }

                return (404, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = $"Object not found: {objectPath}",
                    ["inPrefabMode"] = inPrefabMode
                }));
            }

            // Try to find by name
            var nameMatch = System.Text.RegularExpressions.Regex.Match(body, "\"name\"\\s*:\\s*\"([^\"]+)\"");
            if (nameMatch.Success)
            {
                var objectName = nameMatch.Groups[1].Value;
                GameObject foundObject = null;

                if (inPrefabMode)
                {
                    // In Prefab Mode, search recursively in prefab hierarchy
                    var root = prefabStage.prefabContentsRoot;
                    foundObject = FindChildByNameRecursive(root.transform, objectName);
                }
                else
                {
                    var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                    foundObject = allObjects.FirstOrDefault(o => o.name == objectName);
                }

                if (foundObject != null)
                {
                    Selection.activeGameObject = foundObject;
                    return (200, "application/json", ToJson(new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["message"] = $"Selected object: {foundObject.name}",
                        ["objectName"] = foundObject.name,
                        ["inPrefabMode"] = inPrefabMode
                    }));
                }

                return (404, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = $"Object not found by name: {objectName}",
                    ["inPrefabMode"] = inPrefabMode
                }));
            }

            return (400, "application/json", ToJson(new Dictionary<string, object>
            {
                ["success"] = false,
                ["error"] = "Provide 'path' or 'name' parameter"
            }));
        }

        private static GameObject FindChildByNameRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent.gameObject;
            }

            foreach (Transform child in parent)
            {
                var found = FindChildByNameRecursive(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private (int, string, string) HandleFrameSelected()
        {
            if (Selection.activeGameObject == null && Selection.activeObject == null)
            {
                return (400, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "No object selected"
                }));
            }

            // Frame the selected object in Scene View
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.FrameSelected();
                return (200, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["message"] = $"Framed selected object: {Selection.activeObject?.name ?? "unknown"}",
                    ["objectName"] = Selection.activeObject?.name
                }));
            }

            return (500, "application/json", ToJson(new Dictionary<string, object>
            {
                ["success"] = false,
                ["error"] = "No active Scene View"
            }));
        }

        private (int, string, string) HandleGetHierarchy(string body)
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            var inPrefabMode = prefabStage != null;

            // Parse max depth from body (default: 10)
            var maxDepth = 10;
            if (!string.IsNullOrEmpty(body))
            {
                var depthMatch = System.Text.RegularExpressions.Regex.Match(body, "\"maxDepth\"\\s*:\\s*(\\d+)");
                if (depthMatch.Success)
                {
                    maxDepth = int.Parse(depthMatch.Groups[1].Value);
                }
            }

            var hierarchyItems = new List<Dictionary<string, object>>();

            if (inPrefabMode)
            {
                // Get hierarchy from prefab root
                var root = prefabStage.prefabContentsRoot;
                BuildHierarchy(root.transform, hierarchyItems, 0, maxDepth, "");
            }
            else
            {
                // Get hierarchy from scene root objects
                var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    BuildHierarchy(root.transform, hierarchyItems, 0, maxDepth, "");
                }
            }

            return (200, "application/json", ToJson(new Dictionary<string, object>
            {
                ["success"] = true,
                ["inPrefabMode"] = inPrefabMode,
                ["prefabName"] = inPrefabMode ? prefabStage.prefabContentsRoot.name : null,
                ["objectCount"] = hierarchyItems.Count,
                ["hierarchy"] = hierarchyItems
            }));
        }

        private static void BuildHierarchy(Transform transform, List<Dictionary<string, object>> items, int depth, int maxDepth, string parentPath)
        {
            var path = string.IsNullOrEmpty(parentPath) ? transform.name : $"{parentPath}/{transform.name}";
            
            items.Add(new Dictionary<string, object>
            {
                ["name"] = transform.name,
                ["path"] = path,
                ["depth"] = depth,
                ["childCount"] = transform.childCount,
                ["active"] = transform.gameObject.activeSelf
            });

            if (depth < maxDepth)
            {
                foreach (Transform child in transform)
                {
                    BuildHierarchy(child, items, depth + 1, maxDepth, path);
                }
            }
        }

        private (int, string, string) HandleTerminalExecute(string body)
        {
            if (string.IsNullOrEmpty(body) || !body.Contains("\"command\""))
            {
                return (400, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "Missing 'command' parameter"
                }));
            }

            var commandMatch = System.Text.RegularExpressions.Regex.Match(body, "\"command\"\\s*:\\s*\"([^\"]+)\"");
            if (!commandMatch.Success)
            {
                return (400, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "Invalid 'command' parameter"
                }));
            }

            var commandLine = commandMatch.Groups[1].Value;
            var (success, output, error) = TerminalBridge.ExecuteCommand(commandLine);

            var response = new Dictionary<string, object>
            {
                ["success"] = success,
                ["command"] = commandLine
            };

            if (output != null) response["output"] = output;
            if (error != null) response["error"] = error;

            return (200, "application/json", ToJson(response));
        }

        private (int, string, string) HandleTerminalExecuteBatch(string body)
        {
            if (string.IsNullOrEmpty(body) || !body.Contains("\"commands\""))
            {
                return (400, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "Missing 'commands' parameter (expected array)"
                }));
            }

            // Extract commands array using regex
            var arrayMatch = System.Text.RegularExpressions.Regex.Match(body, "\"commands\"\\s*:\\s*\\[([^\\]]*)]");
            if (!arrayMatch.Success)
            {
                return (400, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "Invalid 'commands' parameter (expected array)"
                }));
            }

            var arrayContent = arrayMatch.Groups[1].Value;
            var commandMatches = System.Text.RegularExpressions.Regex.Matches(arrayContent, "\"([^\"]+)\"");

            if (commandMatches.Count == 0)
            {
                return (400, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "Empty commands array"
                }));
            }

            var results = new List<Dictionary<string, object>>();
            var allSuccess = true;

            foreach (System.Text.RegularExpressions.Match match in commandMatches)
            {
                var commandLine = match.Groups[1].Value;
                var (success, output, error) = TerminalBridge.ExecuteCommand(commandLine);

                var entry = new Dictionary<string, object>
                {
                    ["success"] = success,
                    ["command"] = commandLine
                };
                if (output != null) entry["output"] = output;
                if (error != null) entry["error"] = error;
                results.Add(entry);

                if (!success) allSuccess = false;
            }

            return (200, "application/json", ToJson(new Dictionary<string, object>
            {
                ["success"] = allSuccess,
                ["count"] = results.Count,
                ["results"] = results
            }));
        }

        private (int, string, string) HandleTerminalStatus()
        {
            var (active, visible, logCount, commandCount) = TerminalBridge.GetStatus();

            return (200, "application/json", ToJson(new Dictionary<string, object>
            {
                ["active"] = active,
                ["visible"] = visible,
                ["logCount"] = logCount,
                ["commandCount"] = commandCount,
                ["isPlaying"] = EditorStateCache.IsPlaying
            }));
        }

        private (int, string, string) HandleTerminalCommands(string body)
        {
            var categoryFilter = "all";

            if (!string.IsNullOrEmpty(body))
            {
                var categoryMatch = System.Text.RegularExpressions.Regex.Match(body, "\"category\"\\s*:\\s*\"([^\"]+)\"");
                if (categoryMatch.Success)
                    categoryFilter = categoryMatch.Groups[1].Value.ToLowerInvariant();
            }

            var commands = TerminalBridge.GetCommands(categoryFilter);

            return (200, "application/json", ToJson(new Dictionary<string, object>
            {
                ["count"] = commands.Count,
                ["category"] = categoryFilter,
                ["commands"] = commands
            }));
        }

        private (int, string, string) HandleTerminalLogs(string body)
        {
            var limit = 0;
            var typeFilter = "all";

            if (!string.IsNullOrEmpty(body))
            {
                var limitMatch = System.Text.RegularExpressions.Regex.Match(body, "\"limit\"\\s*:\\s*(\\d+)");
                if (limitMatch.Success)
                    limit = int.Parse(limitMatch.Groups[1].Value);

                var typeMatch = System.Text.RegularExpressions.Regex.Match(body, "\"type\"\\s*:\\s*\"([^\"]+)\"");
                if (typeMatch.Success)
                    typeFilter = typeMatch.Groups[1].Value.ToLowerInvariant();
            }

            var (active, logs) = TerminalBridge.GetTerminalLogs(limit, typeFilter);

            if (!active)
            {
                return (200, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["active"] = false,
                    ["error"] = "Debug Terminal is not active. Enter Play Mode first.",
                    ["count"] = 0,
                    ["logs"] = Array.Empty<object>()
                }));
            }

            return (200, "application/json", ToJson(new Dictionary<string, object>
            {
                ["active"] = true,
                ["count"] = logs.Count,
                ["logs"] = logs
            }));
        }

        private (int, string, string) HandleTerminalHistory(string body)
        {
            var limit = 0;

            if (!string.IsNullOrEmpty(body))
            {
                var limitMatch = System.Text.RegularExpressions.Regex.Match(body, "\"limit\"\\s*:\\s*(\\d+)");
                if (limitMatch.Success)
                    limit = int.Parse(limitMatch.Groups[1].Value);
            }

            var (active, history) = TerminalBridge.GetTerminalHistory(limit);

            if (!active)
            {
                return (200, "application/json", ToJson(new Dictionary<string, object>
                {
                    ["active"] = false,
                    ["error"] = "Debug Terminal is not active. Enter Play Mode first.",
                    ["count"] = 0,
                    ["history"] = Array.Empty<string>()
                }));
            }

            return (200, "application/json", ToJson(new Dictionary<string, object>
            {
                ["active"] = true,
                ["count"] = history.Count,
                ["history"] = history
            }));
        }

        private static string ToJson(object obj) => JsonSerializer.Serialize(obj);
    }
}
