using UnityEditor;
using UnityEngine;

namespace UnityMCPBridge.Settings
{
    /// <summary>
    /// Persistent settings for Unity MCP Bridge.
    /// Uses EditorPrefs for storage to persist across sessions.
    /// </summary>
    public static class MCPBridgeSettings
    {
        private const string KeyPrefix = "UnityMCPBridge_";
        private const string PortKey = KeyPrefix + "Port";
        private const string MaxLogEntriesKey = KeyPrefix + "MaxLogEntries";
        private const string AutoStartKey = KeyPrefix + "AutoStart";
        private const string IncludeWarningsKey = KeyPrefix + "IncludeWarnings";
        private const string IncludeStackTraceKey = KeyPrefix + "IncludeStackTrace";
        private const string ScreenshotQualityKey = KeyPrefix + "ScreenshotQuality";
        private const string ScreenshotCleanupMinutesKey = KeyPrefix + "ScreenshotCleanupMinutes";
        private const string TerminalToggleKeyKey = KeyPrefix + "TerminalToggleKey";
        private const string TerminalEnabledInBuildsKey = KeyPrefix + "TerminalEnabledInBuilds";
        private const string TerminalMaxLogEntriesKey = KeyPrefix + "TerminalMaxLogEntries";
        private const string TerminalFontSizeKey = KeyPrefix + "TerminalFontSize";

        // Default values
        private const int DefaultPort = 7890;
        private const int DefaultMaxLogEntries = 500;
        private const bool DefaultAutoStart = false;
        private const bool DefaultIncludeWarnings = true;
        private const bool DefaultIncludeStackTrace = true;
        private const int DefaultScreenshotQuality = 0; // Low
        private const int DefaultScreenshotCleanupMinutes = 30;
        private const int DefaultTerminalToggleKey = (int)KeyCode.BackQuote;
        private const bool DefaultTerminalEnabledInBuilds = false;
        private const int DefaultTerminalMaxLogEntries = 200;
        private const int DefaultTerminalFontSize = 14;

        /// <summary>
        /// HTTP server port. Default: 7890
        /// </summary>
        public static int Port
        {
            get => EditorPrefs.GetInt(PortKey, DefaultPort);
            set => EditorPrefs.SetInt(PortKey, Mathf.Clamp(value, 1024, 65535));
        }

        /// <summary>
        /// Maximum number of log entries to keep in memory. Default: 500
        /// </summary>
        public static int MaxLogEntries
        {
            get => EditorPrefs.GetInt(MaxLogEntriesKey, DefaultMaxLogEntries);
            set => EditorPrefs.SetInt(MaxLogEntriesKey, Mathf.Max(10, value));
        }

        /// <summary>
        /// Whether to auto-start the server when Unity opens. Default: false
        /// </summary>
        public static bool AutoStart
        {
            get => EditorPrefs.GetBool(AutoStartKey, DefaultAutoStart);
            set => EditorPrefs.SetBool(AutoStartKey, value);
        }

        /// <summary>
        /// Whether to include compilation warnings (not just errors). Default: true
        /// </summary>
        public static bool IncludeWarnings
        {
            get => EditorPrefs.GetBool(IncludeWarningsKey, DefaultIncludeWarnings);
            set => EditorPrefs.SetBool(IncludeWarningsKey, value);
        }

        /// <summary>
        /// Whether to include stack traces in log entries. Default: true
        /// </summary>
        public static bool IncludeStackTrace
        {
            get => EditorPrefs.GetBool(IncludeStackTraceKey, DefaultIncludeStackTrace);
            set => EditorPrefs.SetBool(IncludeStackTraceKey, value);
        }

        /// <summary>
        /// Default screenshot quality preset (0=Low, 1=Medium, 2=High). Default: 0 (Low)
        /// </summary>
        public static int ScreenshotQuality
        {
            get => EditorPrefs.GetInt(ScreenshotQualityKey, DefaultScreenshotQuality);
            set => EditorPrefs.SetInt(ScreenshotQualityKey, Mathf.Clamp(value, 0, 2));
        }

        /// <summary>
        /// Auto-cleanup screenshots older than this many minutes. 0 = disabled. Default: 30
        /// </summary>
        public static int ScreenshotCleanupMinutes
        {
            get => EditorPrefs.GetInt(ScreenshotCleanupMinutesKey, DefaultScreenshotCleanupMinutes);
            set => EditorPrefs.SetInt(ScreenshotCleanupMinutesKey, Mathf.Max(0, value));
        }

        /// <summary>
        /// Terminal toggle key. Default: BackQuote (~)
        /// </summary>
        public static KeyCode TerminalToggleKey
        {
            get => (KeyCode)EditorPrefs.GetInt(TerminalToggleKeyKey, DefaultTerminalToggleKey);
            set => EditorPrefs.SetInt(TerminalToggleKeyKey, (int)value);
        }

        /// <summary>
        /// Whether the terminal is available in development builds. Default: false
        /// </summary>
        public static bool TerminalEnabledInBuilds
        {
            get => EditorPrefs.GetBool(TerminalEnabledInBuildsKey, DefaultTerminalEnabledInBuilds);
            set => EditorPrefs.SetBool(TerminalEnabledInBuildsKey, value);
        }

        /// <summary>
        /// Maximum terminal output lines. Default: 200
        /// </summary>
        public static int TerminalMaxLogEntries
        {
            get => EditorPrefs.GetInt(TerminalMaxLogEntriesKey, DefaultTerminalMaxLogEntries);
            set => EditorPrefs.SetInt(TerminalMaxLogEntriesKey, Mathf.Max(50, value));
        }

        /// <summary>
        /// Terminal font size. Default: 14
        /// </summary>
        public static int TerminalFontSize
        {
            get => EditorPrefs.GetInt(TerminalFontSizeKey, DefaultTerminalFontSize);
            set => EditorPrefs.SetInt(TerminalFontSizeKey, Mathf.Clamp(value, 10, 24));
        }

        /// <summary>
        /// Resets all settings to their default values.
        /// </summary>
        public static void ResetToDefaults()
        {
            Port = DefaultPort;
            MaxLogEntries = DefaultMaxLogEntries;
            AutoStart = DefaultAutoStart;
            IncludeWarnings = DefaultIncludeWarnings;
            IncludeStackTrace = DefaultIncludeStackTrace;
            ScreenshotQuality = DefaultScreenshotQuality;
            ScreenshotCleanupMinutes = DefaultScreenshotCleanupMinutes;
            TerminalToggleKey = (KeyCode)DefaultTerminalToggleKey;
            TerminalEnabledInBuilds = DefaultTerminalEnabledInBuilds;
            TerminalMaxLogEntries = DefaultTerminalMaxLogEntries;
            TerminalFontSize = DefaultTerminalFontSize;
        }
    }
}
