using System;

namespace UnityMCPBridge.Core
{
    /// <summary>
    /// Quality presets for screenshot capture.
    /// </summary>
    public enum ScreenshotQuality
    {
        /// <summary>Low quality (640x480) - ~400-600 tokens</summary>
        Low,
        /// <summary>Medium quality (1280x720) - ~1200 tokens</summary>
        Medium,
        /// <summary>High quality (full resolution) - ~2700+ tokens</summary>
        High
    }

    /// <summary>
    /// View type to capture.
    /// </summary>
    public enum ScreenshotView
    {
        /// <summary>Capture the Game View</summary>
        Game,
        /// <summary>Capture the Scene View</summary>
        Scene
    }

    /// <summary>
    /// Result of a screenshot capture operation.
    /// </summary>
    public class ScreenshotResult
    {
        /// <summary>Whether the capture was successful.</summary>
        public bool Success { get; set; }

        /// <summary>Absolute path to the saved screenshot file.</summary>
        public string FilePath { get; set; }

        /// <summary>Width of the captured image in pixels.</summary>
        public int Width { get; set; }

        /// <summary>Height of the captured image in pixels.</summary>
        public int Height { get; set; }

        /// <summary>File size in bytes.</summary>
        public long FileSize { get; set; }

        /// <summary>Estimated token cost for AI processing.</summary>
        public int EstimatedTokens { get; set; }

        /// <summary>Error message if capture failed.</summary>
        public string ErrorMessage { get; set; }

        /// <summary>Timestamp when the screenshot was taken.</summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Service for capturing screenshots from Unity Editor views.
    /// </summary>
    public interface IScreenshotService : IService
    {
        /// <summary>
        /// Captures a screenshot from the specified view.
        /// </summary>
        /// <param name="view">Which view to capture (Game or Scene).</param>
        /// <param name="quality">Quality preset for the screenshot.</param>
        /// <returns>Result containing file path and metadata.</returns>
        ScreenshotResult CaptureScreenshot(ScreenshotView view, ScreenshotQuality quality);

        /// <summary>
        /// Gets the directory where screenshots are saved.
        /// </summary>
        string ScreenshotDirectory { get; }

        /// <summary>
        /// Cleans up old screenshot files.
        /// </summary>
        /// <param name="maxAgeMinutes">Delete files older than this many minutes.</param>
        /// <returns>Number of files deleted.</returns>
        int CleanupOldScreenshots(int maxAgeMinutes = 30);
    }
}
