using System.Collections.Generic;
using UnityEditor;
using UnityMCPBridge.Settings;
using UnityMCPBridge.Terminal;

namespace UnityMCPBridge.Core
{
    /// <summary>
    /// Editor-side bridge for the Runtime Debug Terminal.
    /// Pushes settings to Runtime on play mode entry.
    /// Routes MCP HTTP requests to the Runtime terminal.
    /// </summary>
    [InitializeOnLoad]
    public static class TerminalBridge
    {
        static TerminalBridge()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                PushSettingsToRuntime();
            }
        }

        private static void PushSettingsToRuntime()
        {
            TerminalSettings.ToggleKey = MCPBridgeSettings.TerminalToggleKey;
            TerminalSettings.EnabledInBuilds = MCPBridgeSettings.TerminalEnabledInBuilds;
            TerminalSettings.MaxLogEntries = MCPBridgeSettings.TerminalMaxLogEntries;
            TerminalSettings.FontSize = MCPBridgeSettings.TerminalFontSize;
        }

        /// <summary>
        /// Executes a terminal command. Called from RequestHandler on main thread.
        /// </summary>
        public static (bool success, string output, string error) ExecuteCommand(string commandLine)
        {
            var terminal = DebugTerminal.Instance;
            if (terminal == null)
            {
                return (false, null, "Debug Terminal is not active. Enter Play Mode first.");
            }

            var result = terminal.ExecuteCommand(commandLine);
            return (result.Success, result.Output, result.Error);
        }

        /// <summary>
        /// Gets terminal status. Called from RequestHandler on main thread.
        /// </summary>
        public static (bool active, bool visible, int logCount, int commandCount) GetStatus()
        {
            var terminal = DebugTerminal.Instance;
            var commandCount = TerminalCommandRegistry.IsInitialized
                ? TerminalCommandRegistry.GetCommandNames().Count
                : 0;

            if (terminal == null)
            {
                return (false, false, 0, commandCount);
            }

            return (true, terminal.IsVisible, terminal.GetLogs().Count, commandCount);
        }

        /// <summary>
        /// Gets terminal logs with optional filtering.
        /// </summary>
        public static (bool active, List<Dictionary<string, object>> logs) GetTerminalLogs(int limit, string typeFilter)
        {
            var terminal = DebugTerminal.Instance;
            if (terminal == null)
                return (false, new List<Dictionary<string, object>>());

            var allLogs = terminal.GetLogs();
            var result = new List<Dictionary<string, object>>();

            foreach (var log in allLogs)
            {
                if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "all")
                {
                    var logTypeName = log.Type.ToString().ToLowerInvariant();
                    if (logTypeName != typeFilter)
                        continue;
                }

                result.Add(new Dictionary<string, object>
                {
                    ["text"] = log.Text,
                    ["type"] = log.Type.ToString().ToLowerInvariant(),
                    ["timestamp"] = log.Timestamp.ToString("o")
                });
            }

            if (limit > 0 && result.Count > limit)
                result = result.GetRange(result.Count - limit, limit);

            return (true, result);
        }

        /// <summary>
        /// Gets terminal command history.
        /// </summary>
        public static (bool active, List<string> history) GetTerminalHistory(int limit)
        {
            var terminal = DebugTerminal.Instance;
            if (terminal == null)
                return (false, new List<string>());

            var allHistory = terminal.GetHistory();
            var result = new List<string>(allHistory);

            if (limit > 0 && result.Count > limit)
                result = result.GetRange(result.Count - limit, limit);

            return (true, result);
        }
    }
}
