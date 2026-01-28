using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityMCPBridge.Core;

namespace UnityMCPBridge.Services
{
    /// <summary>
    /// Service for capturing screenshots from Unity Editor views.
    /// Implements screenshot capture for Game View and Scene View with quality presets.
    /// </summary>
    public sealed class ScreenshotService : IScreenshotService
    {
        private const string ScreenshotFolderName = "MCPBridge_Screenshots";
        private const int TokensPerPixelDivisor = 750;

        private static readonly (int width, int height)[] QualityResolutions =
        {
            (640, 480),   // Low
            (1280, 720),  // Medium
            (0, 0)        // High - use native resolution
        };

        private string _screenshotDirectory;
        private bool _isRunning;

        /// <inheritdoc/>
        public bool IsRunning => _isRunning;

        /// <inheritdoc/>
        public string ScreenshotDirectory => _screenshotDirectory;

        /// <inheritdoc/>
        public void Start()
        {
            if (_isRunning) return;

            // Use temp directory for screenshots
            _screenshotDirectory = Path.Combine(Path.GetTempPath(), ScreenshotFolderName);
            
            if (!Directory.Exists(_screenshotDirectory))
            {
                Directory.CreateDirectory(_screenshotDirectory);
            }

            _isRunning = true;
            Debug.Log($"[MCP Bridge] Screenshot service started. Directory: {_screenshotDirectory}");
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (!_isRunning) return;

            // Clean up old screenshots on stop
            CleanupOldScreenshots(0);
            _isRunning = false;
        }

        /// <inheritdoc/>
        public ScreenshotResult CaptureScreenshot(ScreenshotView view, ScreenshotQuality quality)
        {
            if (!_isRunning)
            {
                return new ScreenshotResult
                {
                    Success = false,
                    ErrorMessage = "Screenshot service is not running",
                    Timestamp = DateTime.UtcNow
                };
            }

            try
            {
                return view switch
                {
                    ScreenshotView.Game => CaptureGameView(quality),
                    ScreenshotView.Scene => CaptureSceneView(quality),
                    _ => new ScreenshotResult
                    {
                        Success = false,
                        ErrorMessage = $"Unknown view type: {view}",
                        Timestamp = DateTime.UtcNow
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Bridge] Screenshot capture failed: {ex}");
                return new ScreenshotResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        /// <inheritdoc/>
        public int CleanupOldScreenshots(int maxAgeMinutes = 30)
        {
            if (string.IsNullOrEmpty(_screenshotDirectory) || !Directory.Exists(_screenshotDirectory))
            {
                return 0;
            }

            var cutoffTime = DateTime.UtcNow.AddMinutes(-maxAgeMinutes);
            var files = Directory.GetFiles(_screenshotDirectory, "*.png");
            var deletedCount = 0;

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTimeUtc < cutoffTime)
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MCP Bridge] Failed to delete old screenshot: {ex.Message}");
                }
            }

            return deletedCount;
        }

        private ScreenshotResult CaptureGameView(ScreenshotQuality quality)
        {
            // Find the main camera or any camera in the scene
            var camera = Camera.main;
            if (camera == null)
            {
                var cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (cameras.Length > 0)
                {
                    camera = cameras[0];
                }
            }

            if (camera == null)
            {
                return new ScreenshotResult
                {
                    Success = false,
                    ErrorMessage = "No camera found in scene. Add a camera or use Scene View instead.",
                    Timestamp = DateTime.UtcNow
                };
            }

            var (targetWidth, targetHeight) = GetTargetResolution(quality);

            // Get camera's aspect ratio from Game View or use default
            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            var gameView = EditorWindow.GetWindow(gameViewType, false, "Game", false);
            
            int viewWidth = 1920;
            int viewHeight = 1080;
            
            if (gameView != null)
            {
                var position = gameView.position;
                viewWidth = Mathf.Max((int)position.width, 1);
                viewHeight = Mathf.Max((int)position.height, 1);
            }

            int width, height;
            if (targetWidth > 0 && targetHeight > 0)
            {
                var aspect = (float)viewWidth / viewHeight;
                if (viewWidth > targetWidth || viewHeight > targetHeight)
                {
                    if (aspect > (float)targetWidth / targetHeight)
                    {
                        width = targetWidth;
                        height = Mathf.RoundToInt(targetWidth / aspect);
                    }
                    else
                    {
                        height = targetHeight;
                        width = Mathf.RoundToInt(targetHeight * aspect);
                    }
                }
                else
                {
                    width = viewWidth;
                    height = viewHeight;
                }
            }
            else
            {
                width = viewWidth;
                height = viewHeight;
            }

            // Ensure minimum dimensions
            width = Mathf.Max(width, 1);
            height = Mathf.Max(height, 1);

            // Create render texture and capture from camera
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTargetTexture = camera.targetTexture;
            var previousActiveTexture = RenderTexture.active;

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();

                // Save to file
                var filename = GenerateFilename("game");
                var filePath = Path.Combine(_screenshotDirectory, filename);
                var pngBytes = texture.EncodeToPNG();
                File.WriteAllBytes(filePath, pngBytes);

                UnityEngine.Object.DestroyImmediate(texture);

                var fileInfo = new FileInfo(filePath);

                return new ScreenshotResult
                {
                    Success = true,
                    FilePath = filePath,
                    Width = width,
                    Height = height,
                    FileSize = fileInfo.Length,
                    EstimatedTokens = CalculateTokens(width, height),
                    Timestamp = DateTime.UtcNow
                };
            }
            finally
            {
                camera.targetTexture = previousTargetTexture;
                RenderTexture.active = previousActiveTexture;
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private ScreenshotResult CaptureSceneView(ScreenshotQuality quality)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                return new ScreenshotResult
                {
                    Success = false,
                    ErrorMessage = "No active Scene View found",
                    Timestamp = DateTime.UtcNow
                };
            }

            var camera = sceneView.camera;
            if (camera == null)
            {
                return new ScreenshotResult
                {
                    Success = false,
                    ErrorMessage = "Scene View camera not available",
                    Timestamp = DateTime.UtcNow
                };
            }

            var (targetWidth, targetHeight) = GetTargetResolution(quality);
            
            // Use Scene View dimensions if no target specified
            var viewWidth = (int)sceneView.position.width;
            var viewHeight = (int)sceneView.position.height;

            int width, height;
            if (targetWidth > 0 && targetHeight > 0)
            {
                var aspect = (float)viewWidth / viewHeight;
                if (viewWidth > targetWidth || viewHeight > targetHeight)
                {
                    if (aspect > (float)targetWidth / targetHeight)
                    {
                        width = targetWidth;
                        height = Mathf.RoundToInt(targetWidth / aspect);
                    }
                    else
                    {
                        height = targetHeight;
                        width = Mathf.RoundToInt(targetHeight * aspect);
                    }
                }
                else
                {
                    width = viewWidth;
                    height = viewHeight;
                }
            }
            else
            {
                width = viewWidth;
                height = viewHeight;
            }

            // Ensure minimum dimensions
            width = Mathf.Max(width, 1);
            height = Mathf.Max(height, 1);

            // Create render texture and capture
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTargetTexture = camera.targetTexture;
            var previousActiveTexture = RenderTexture.active;

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();

                // Save to file
                var filename = GenerateFilename("scene");
                var filePath = Path.Combine(_screenshotDirectory, filename);
                var pngBytes = texture.EncodeToPNG();
                File.WriteAllBytes(filePath, pngBytes);

                UnityEngine.Object.DestroyImmediate(texture);

                var fileInfo = new FileInfo(filePath);

                return new ScreenshotResult
                {
                    Success = true,
                    FilePath = filePath,
                    Width = width,
                    Height = height,
                    FileSize = fileInfo.Length,
                    EstimatedTokens = CalculateTokens(width, height),
                    Timestamp = DateTime.UtcNow
                };
            }
            finally
            {
                camera.targetTexture = previousTargetTexture;
                RenderTexture.active = previousActiveTexture;
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static (int width, int height) GetTargetResolution(ScreenshotQuality quality)
        {
            var index = (int)quality;
            if (index >= 0 && index < QualityResolutions.Length)
            {
                return QualityResolutions[index];
            }
            return (0, 0);
        }

        private static string GenerateFilename(string viewType)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            return $"mcp_{viewType}_{timestamp}.png";
        }

        private static int CalculateTokens(int width, int height)
        {
            return (width * height) / TokensPerPixelDivisor;
        }
    }
}
