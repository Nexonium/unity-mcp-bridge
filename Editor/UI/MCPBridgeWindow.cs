using UnityEditor;
using UnityEngine;
using UnityMCPBridge.Core;
using UnityMCPBridge.Settings;

namespace UnityMCPBridge.UI
{
    /// <summary>
    /// Editor window for managing MCP Bridge settings and status.
    /// </summary>
    public sealed class MCPBridgeWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _showAdvancedSettings;

        // Cached styles
        private GUIStyle _headerStyle;
        private GUIStyle _statusBoxStyle;
        private GUIStyle _statusTextStyle;
        private bool _stylesInitialized;

        [MenuItem("Window/Unity MCP Bridge")]
        public static void ShowWindow()
        {
            var window = GetWindow<MCPBridgeWindow>();
            window.titleContent = new GUIContent("MCP Bridge");
            window.minSize = new Vector2(300, 400);
            window.Show();
        }

        private void OnEnable()
        {
            MCPBridge.OnBridgeStarted += Repaint;
            MCPBridge.OnBridgeStopped += Repaint;
        }

        private void OnDisable()
        {
            MCPBridge.OnBridgeStarted -= Repaint;
            MCPBridge.OnBridgeStopped -= Repaint;
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 5)
            };

            _statusBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            _statusTextStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitializeStyles();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawStatusSection();
            EditorGUILayout.Space(10);
            DrawControlsSection();
            EditorGUILayout.Space(10);
            DrawSettingsSection();
            EditorGUILayout.Space(10);
            DrawStatsSection();
            EditorGUILayout.Space(10);
            DrawAdvancedSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawStatusSection()
        {
            EditorGUILayout.LabelField("Status", _headerStyle);

            EditorGUILayout.BeginVertical(_statusBoxStyle);

            var isRunning = MCPBridge.IsRunning;
            var statusColor = isRunning ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.3f, 0.3f);
            var statusText = isRunning ? "RUNNING" : "STOPPED";

            var prevColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(statusText, _statusTextStyle);
            GUI.color = prevColor;

            if (isRunning)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Port: {MCPBridge.HttpServer.Port}");
                EditorGUILayout.LabelField($"URL: http://127.0.0.1:{MCPBridge.HttpServer.Port}/");
                
                EditorGUILayout.Space(5);
                
                // Copy URL button
                if (GUILayout.Button("Copy URL to Clipboard"))
                {
                    EditorGUIUtility.systemCopyBuffer = $"http://127.0.0.1:{MCPBridge.HttpServer.Port}/";
                    Debug.Log("[MCP Bridge] URL copied to clipboard");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawControlsSection()
        {
            EditorGUILayout.LabelField("Controls", _headerStyle);

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !MCPBridge.IsRunning;
            if (GUILayout.Button("Start Server", GUILayout.Height(30)))
            {
                MCPBridge.Start();
            }

            GUI.enabled = MCPBridge.IsRunning;
            if (GUILayout.Button("Stop Server", GUILayout.Height(30)))
            {
                MCPBridge.Stop();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("Settings", _headerStyle);

            EditorGUILayout.BeginVertical(_statusBoxStyle);

            // Auto-start
            var autoStart = EditorGUILayout.Toggle(
                new GUIContent("Auto-start on Unity Open", "Automatically start the server when Unity opens"),
                MCPBridgeSettings.AutoStart
            );
            if (autoStart != MCPBridgeSettings.AutoStart)
            {
                MCPBridgeSettings.AutoStart = autoStart;
            }

            EditorGUILayout.Space(5);

            // Port
            EditorGUI.BeginDisabledGroup(MCPBridge.IsRunning);
            var port = EditorGUILayout.IntField(
                new GUIContent("Port", "HTTP server port (requires restart)"),
                MCPBridgeSettings.Port
            );
            if (port != MCPBridgeSettings.Port)
            {
                MCPBridgeSettings.Port = port;
            }
            EditorGUI.EndDisabledGroup();

            if (MCPBridge.IsRunning)
            {
                EditorGUILayout.HelpBox("Stop the server to change the port.", MessageType.Info);
            }

            EditorGUILayout.Space(5);

            // Max log entries
            var maxLogs = EditorGUILayout.IntField(
                new GUIContent("Max Log Entries", "Maximum number of log entries to keep in memory"),
                MCPBridgeSettings.MaxLogEntries
            );
            if (maxLogs != MCPBridgeSettings.MaxLogEntries)
            {
                MCPBridgeSettings.MaxLogEntries = maxLogs;
                if (MCPBridge.LogService != null)
                {
                    MCPBridge.LogService.MaxEntries = maxLogs;
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStatsSection()
        {
            EditorGUILayout.LabelField("Statistics", _headerStyle);

            EditorGUILayout.BeginVertical(_statusBoxStyle);

            if (MCPBridge.IsRunning)
            {
                var logCount = MCPBridge.LogService?.GetLogs().Count ?? 0;
                var errorCount = MCPBridge.CompilationService?.GetErrors().Count ?? 0;
                var warningCount = MCPBridge.CompilationService?.GetWarnings().Count ?? 0;

                EditorGUILayout.LabelField($"Log Entries: {logCount}");
                EditorGUILayout.LabelField($"Compilation Errors: {errorCount}");
                EditorGUILayout.LabelField($"Compilation Warnings: {warningCount}");

                EditorGUILayout.Space(5);

                if (GUILayout.Button("Clear Logs"))
                {
                    MCPBridge.LogService?.Clear();
                    Debug.Log("[MCP Bridge] Logs cleared");
                }
            }
            else
            {
                EditorGUILayout.LabelField("Start the server to see statistics.");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAdvancedSection()
        {
            _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "Advanced Settings", true);

            if (!_showAdvancedSettings) return;

            EditorGUILayout.BeginVertical(_statusBoxStyle);

            // Include stack traces
            var includeStackTrace = EditorGUILayout.Toggle(
                new GUIContent("Include Stack Traces", "Include stack traces in log entries"),
                MCPBridgeSettings.IncludeStackTrace
            );
            if (includeStackTrace != MCPBridgeSettings.IncludeStackTrace)
            {
                MCPBridgeSettings.IncludeStackTrace = includeStackTrace;
            }

            // Include warnings
            var includeWarnings = EditorGUILayout.Toggle(
                new GUIContent("Include Warnings", "Include compilation warnings (not just errors)"),
                MCPBridgeSettings.IncludeWarnings
            );
            if (includeWarnings != MCPBridgeSettings.IncludeWarnings)
            {
                MCPBridgeSettings.IncludeWarnings = includeWarnings;
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog(
                    "Reset Settings",
                    "Are you sure you want to reset all settings to their default values?",
                    "Reset",
                    "Cancel"))
                {
                    MCPBridgeSettings.ResetToDefaults();
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
