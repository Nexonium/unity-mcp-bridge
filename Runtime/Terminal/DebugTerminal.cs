using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityMCPBridge.Terminal
{
    public sealed class DebugTerminal : MonoBehaviour
    {
        private static DebugTerminal _instance;
        public static DebugTerminal Instance => _instance;

        // State
        private bool _isVisible;
        private string _inputText = "";
        private Vector2 _scrollPosition;
        private readonly List<TerminalLog> _logs = new();
        private readonly List<string> _history = new();
        private int _historyIndex = -1;
        private string _savedInput = "";
        private bool _captureUnityLogs = true;
        private readonly Dictionary<string, string> _aliases = new();
        private static bool _applicationQuitting;

        // GUI
        private GUIStyle _logStyle;
        private GUIStyle _inputStyle;
        private GUIStyle _boxStyle;
        private Texture2D _backgroundTexture;
        private bool _stylesInitialized;
        private bool _focusInput;
        private bool _scrollToBottom;
        private bool _consumeNextChar;
        private bool _moveCursorToEnd;
        private bool _deleteWordPending;
        private bool _pastePending;
        private bool _copyPending;
        private float _logAreaHeight;

        // Rendering Debugger suppression (via reflection to avoid URP dependency)
        private static bool _debugManagerResolved;
        private static object _debugManagerInstance;
        private static PropertyInfo _enableRuntimeUIProp;

        // Layout
        private const float TerminalHeightRatio = 0.4f;
        private const int Padding = 8;
        private const string InputControlName = "TerminalInput";
        private const string HistoryFileName = "terminal_history.txt";
        private const string AliasFileName = "terminal_aliases.txt";
        private static string HistoryFilePath => Path.Combine(Application.persistentDataPath, HistoryFileName);
        private static string AliasFilePath => Path.Combine(Application.persistentDataPath, AliasFileName);

        public bool IsVisible => _isVisible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
#if !UNITY_EDITOR
            if (!TerminalSettings.EnabledInBuilds && !Debug.isDebugBuild) return;
#endif
            // Check for surviving instance from domain reload
            var existing = FindAnyObjectByType<DebugTerminal>();
            if (existing != null)
            {
                _instance = existing;
                if (!TerminalCommandRegistry.IsInitialized)
                    TerminalCommandRegistry.Initialize();
                return;
            }

            var go = new GameObject("[MCP Debug Terminal]");
            go.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<DebugTerminal>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (!TerminalCommandRegistry.IsInitialized)
                TerminalCommandRegistry.Initialize();

            Application.logMessageReceived += OnUnityLogMessage;
            LoadHistory();
            LoadAliases();
            AddLog("Debug Terminal initialized. Press ~ to toggle. Type 'help' for commands.", TerminalLogType.System);
        }

        private void OnApplicationQuit()
        {
            _applicationQuitting = true;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnUnityLogMessage;
            SaveHistory();
            SaveAliases();

            // Only re-enable Rendering Debugger if not quitting
            // (spawning GameObjects from OnDestroy during shutdown causes errors)
            if (!_applicationQuitting)
                SetRenderingDebuggerEnabled(true);

            if (_instance == this)
                _instance = null;

            if (_backgroundTexture != null)
            {
                Destroy(_backgroundTexture);
                _backgroundTexture = null;
            }
        }

        private void Update()
        {
            WatchManager.UpdateAll();
        }

        private void OnGUI()
        {
            // Toggle key: handle ALWAYS, before visibility check.
            // Using OnGUI events instead of Input.GetKeyDown for InputSystem compatibility.
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == TerminalSettings.ToggleKey)
            {
                SetVisible(!_isVisible);
                // IMGUI sends a separate character event (e.g. '`') after the KeyDown.
                // Flag it for consumption so it doesn't get typed into the TextField.
                _consumeNextChar = true;
                Event.current.Use();
                return;
            }
            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == TerminalSettings.ToggleKey)
            {
                Event.current.Use();
                return;
            }

            // Consume the character event that follows the toggle KeyDown.
            // Only check on KeyDown events — Layout/Repaint must not reset the flag.
            if (_consumeNextChar && Event.current.type == EventType.KeyDown)
            {
                _consumeNextChar = false;
                if (Event.current.character != 0)
                {
                    Event.current.Use();
                    return;
                }
            }

            if (!_isVisible) return;

            // Handle special keys BEFORE TextField so it doesn't consume them
            if (Event.current.type == EventType.KeyDown)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        if (!string.IsNullOrWhiteSpace(_inputText))
                        {
                            var input = _inputText.Trim();
                            ExecuteInput(input);
                            _history.Add(input);
                            _historyIndex = _history.Count;
                            SaveHistory();
                            _inputText = "";
                            _savedInput = "";
                            }
                        Event.current.Use();
                        return;

                    case KeyCode.UpArrow:
                        NavigateHistory(-1);
                        _moveCursorToEnd = true;
                        Event.current.Use();
                        return;

                    case KeyCode.DownArrow:
                        NavigateHistory(1);
                        _moveCursorToEnd = true;
                        Event.current.Use();
                        return;

                    case KeyCode.Tab:
                        HandleAutocomplete();
                        _moveCursorToEnd = true;
                        Event.current.Use();
                        return;

                    case KeyCode.Escape:
                        SetVisible(false);
                        Event.current.Use();
                        return;

                    case KeyCode.PageUp:
                        _scrollPosition.y = Mathf.Max(0, _scrollPosition.y - _logAreaHeight);
                        Event.current.Use();
                        return;

                    case KeyCode.PageDown:
                        _scrollPosition.y += _logAreaHeight;
                        Event.current.Use();
                        return;

                    case KeyCode.Backspace when Event.current.control:
                        _deleteWordPending = true;
                        Event.current.Use();
                        break;

                    case KeyCode.V when Event.current.control:
                        _pastePending = true;
                        Event.current.Use();
                        break;

                    case KeyCode.C when Event.current.control:
                        _copyPending = true;
                        Event.current.Use();
                        break;
                }
            }

            InitializeStyles();

            var screenHeight = Screen.height * TerminalHeightRatio;
            var rect = new Rect(0, 0, Screen.width, screenHeight);

            // Background
            GUI.DrawTexture(rect, _backgroundTexture);

            GUILayout.BeginArea(new Rect(Padding, Padding, rect.width - Padding * 2, rect.height - Padding * 2));

            // Log area
            _logAreaHeight = rect.height - Padding * 2 - 28;
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true, GUILayout.Height(_logAreaHeight));

            for (var i = 0; i < _logs.Count; i++)
            {
                var log = _logs[i];
                var prevColor = GUI.contentColor;
                GUI.contentColor = GetLogColor(log.Type);
                GUILayout.Label(log.Text, _logStyle);
                GUI.contentColor = prevColor;
            }

            GUILayout.EndScrollView();

            // Auto-scroll
            if (_scrollToBottom)
            {
                _scrollPosition.y = float.MaxValue;
                _scrollToBottom = false;
            }

            // Input line
            GUILayout.BeginHorizontal();

            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = Color.clear;

            var promptStyle = new GUIStyle(_logStyle) { fixedWidth = 15 };
            GUILayout.Label(">", promptStyle);

            GUI.SetNextControlName(InputControlName);
            _inputText = GUILayout.TextField(_inputText, _inputStyle);

            // Apply pending word deletion through TextEditor to stay in sync
            if (_deleteWordPending)
            {
                _deleteWordPending = false;
                var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                if (editor != null)
                {
                    var text = editor.text;
                    var cursor = editor.cursorIndex;
                    var newCursor = FindWordBoundaryBackward(text, cursor);
                    editor.text = text[..newCursor] + text[cursor..];
                    editor.cursorIndex = newCursor;
                    editor.selectIndex = newCursor;
                    _inputText = editor.text;
                }
            }

            // Apply pending paste through TextEditor to insert at cursor position
            if (_pastePending)
            {
                _pastePending = false;
                var clipboard = GUIUtility.systemCopyBuffer;
                if (!string.IsNullOrEmpty(clipboard))
                {
                    // Strip newlines — terminal is single-line input
                    clipboard = clipboard.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
                    var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    if (editor != null)
                    {
                        var text = editor.text;
                        var cursor = editor.cursorIndex;
                        var select = editor.selectIndex;
                        var start = Mathf.Min(cursor, select);
                        var end = Mathf.Max(cursor, select);
                        editor.text = text[..start] + clipboard + text[end..];
                        var newPos = start + clipboard.Length;
                        editor.cursorIndex = newPos;
                        editor.selectIndex = newPos;
                        _inputText = editor.text;
                    }
                }
            }

            // Apply pending copy — copy selected text or full input line
            if (_copyPending)
            {
                _copyPending = false;
                var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                if (editor != null)
                {
                    var cursor = editor.cursorIndex;
                    var select = editor.selectIndex;
                    if (cursor != select)
                    {
                        var start = Mathf.Min(cursor, select);
                        var end = Mathf.Max(cursor, select);
                        GUIUtility.systemCopyBuffer = editor.text[start..end];
                    }
                    else
                    {
                        // No selection — copy entire input
                        GUIUtility.systemCopyBuffer = editor.text;
                    }
                }
            }

            // Move cursor to end after history/autocomplete changed the text
            if (_moveCursorToEnd)
            {
                var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                if (editor != null)
                {
                    editor.MoveTextEnd();
                }
                _moveCursorToEnd = false;
            }

            GUI.backgroundColor = prevBg;
            GUILayout.EndHorizontal();

            GUILayout.EndArea();

            // Watch panel (drawn below terminal)
            if (WatchManager.HasWatches)
                DrawWatchPanel(rect);

            // Focus management
            if (_focusInput)
            {
                GUI.FocusControl(InputControlName);
                _focusInput = false;
            }

            // Consume ALL remaining keyboard events while terminal is open
            // to prevent game input from receiving them
            if (Event.current.isKey)
            {
                Event.current.Use();
            }
        }

        private void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (visible)
                _focusInput = true;

            // Suppress URP Rendering Debugger while terminal is open
            // (it intercepts Ctrl+Backspace via Input.GetKey polling in Update).
            // Uses reflection to avoid hard dependency on com.unity.render-pipelines.core.
            SetRenderingDebuggerEnabled(!visible);
        }

        private static void SetRenderingDebuggerEnabled(bool enabled)
        {
            try
            {
                if (!_debugManagerResolved)
                {
                    _debugManagerResolved = true;
                    var type = Type.GetType("UnityEngine.Rendering.DebugManager, Unity.RenderPipelines.Core.Runtime");
                    if (type == null) return;

                    var instanceProp = type.GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
                    if (instanceProp == null) return;

                    _debugManagerInstance = instanceProp.GetValue(null);
                    _enableRuntimeUIProp = type.GetProperty("enableRuntimeUI", BindingFlags.Instance | BindingFlags.Public);
                }

                _enableRuntimeUIProp?.SetValue(_debugManagerInstance, enabled);
            }
            catch
            {
                // Silently ignore if DebugManager is unavailable
            }
        }

        private void ExecuteInput(string input)
        {
            AddLog($"> {input}", TerminalLogType.Input);

            // Resolve alias
            input = ResolveAlias(input);

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            var commandName = parts[0].ToLowerInvariant();
            var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

            var result = TerminalCommandRegistry.Execute(commandName, args);

            if (result.Success)
            {
                if (!string.IsNullOrEmpty(result.Output))
                    AddLog(result.Output, TerminalLogType.Info);
            }
            else
            {
                AddLog(result.Error, TerminalLogType.Error);
            }

            _scrollToBottom = true;
        }

        private void NavigateHistory(int direction)
        {
            if (_history.Count == 0) return;

            // Save current input when starting to navigate
            if (_historyIndex == _history.Count)
                _savedInput = _inputText;

            _historyIndex += direction;
            _historyIndex = Mathf.Clamp(_historyIndex, 0, _history.Count);

            _inputText = _historyIndex < _history.Count ? _history[_historyIndex] : _savedInput;
        }

        private void HandleAutocomplete()
        {
            if (string.IsNullOrEmpty(_inputText)) return;

            var spaceIndex = _inputText.IndexOf(' ');

            if (spaceIndex < 0)
            {
                // Command name completion
                var completions = new List<string>(TerminalCommandRegistry.GetCompletions(_inputText));
                if (completions.Count == 0) return;

                if (completions.Count == 1)
                {
                    _inputText = completions[0] + " ";
                    return;
                }

                var prefix = GetLongestCommonPrefix(completions);
                if (prefix.Length > _inputText.Length)
                {
                    _inputText = prefix;
                    return;
                }

                AddLog(string.Join("  ", completions), TerminalLogType.System);
                _scrollToBottom = true;
                return;
            }

            // Argument completion
            var commandName = _inputText[..spaceIndex].Trim();
            var argsPart = _inputText[(spaceIndex + 1)..];
            var currentArgs = argsPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var argCompletions = TerminalCommandRegistry.GetArgumentCompletions(commandName, currentArgs);
            if (argCompletions == null || argCompletions.Count == 0) return;

            if (argCompletions.Count == 1)
            {
                _inputText = $"{commandName} {argCompletions[0]}";
                return;
            }

            // Multiple matches — find common prefix among completions
            var completionsList = argCompletions.ToList();
            var commonPrefix = GetLongestCommonPrefix(completionsList);

            if (currentArgs.Length > 0)
            {
                var currentSuffix = currentArgs[^1];
                if (commonPrefix.Length > currentSuffix.Length)
                {
                    // Replace the last arg with the common prefix
                    var baseArgs = currentArgs.Length > 1
                        ? string.Join(" ", currentArgs[..^1]) + " "
                        : "";
                    _inputText = $"{commandName} {baseArgs}{commonPrefix}";
                    return;
                }
            }
            else if (!string.IsNullOrEmpty(commonPrefix))
            {
                _inputText = $"{commandName} {commonPrefix}";
                return;
            }

            // Show all matches
            AddLog(string.Join("  ", argCompletions), TerminalLogType.System);
            _scrollToBottom = true;
        }

        private static int FindWordBoundaryBackward(string text, int cursor)
        {
            if (string.IsNullOrEmpty(text) || cursor <= 0) return 0;

            var pos = cursor;

            // Skip trailing spaces
            while (pos > 0 && text[pos - 1] == ' ')
                pos--;

            // Skip word characters (non-space, treating dots as word boundaries too)
            while (pos > 0 && text[pos - 1] != ' ' && text[pos - 1] != '.')
                pos--;

            return pos;
        }

        private static string GetLongestCommonPrefix(List<string> strings)
        {
            if (strings.Count == 0) return "";
            var prefix = strings[0];
            for (var i = 1; i < strings.Count; i++)
            {
                while (prefix.Length > 0 && !strings[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    prefix = prefix[..^1];
                }
                if (prefix.Length == 0) return "";
            }
            return prefix;
        }

        // Public API

        public void AddLog(string text, TerminalLogType type)
        {
            _logs.Add(new TerminalLog(text, type));
            while (_logs.Count > TerminalSettings.MaxLogEntries)
                _logs.RemoveAt(0);
            _scrollToBottom = true;
        }

        public void ClearLog()
        {
            _logs.Clear();
            AddLog("Terminal cleared.", TerminalLogType.System);
        }

        public bool CaptureUnityLogs
        {
            get => _captureUnityLogs;
            set => _captureUnityLogs = value;
        }

        private void OnUnityLogMessage(string message, string stackTrace, LogType type)
        {
            if (!_captureUnityLogs) return;

            // Prevent recursion from terminal's own logs
            if (message.StartsWith("[Debug Terminal]") || message.StartsWith("[Terminal]"))
                return;

            var terminalType = type switch
            {
                LogType.Error or LogType.Exception or LogType.Assert => TerminalLogType.Error,
                LogType.Warning => TerminalLogType.Warning,
                _ => TerminalLogType.Info
            };

            var prefix = type switch
            {
                LogType.Error => "[ERROR] ",
                LogType.Exception => "[EXCEPTION] ",
                LogType.Assert => "[ASSERT] ",
                LogType.Warning => "[WARN] ",
                _ => "[LOG] "
            };

            AddLog($"{prefix}{message}", terminalType);
        }

        public TerminalCommandRegistry.CommandResult ExecuteCommand(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                return new TerminalCommandRegistry.CommandResult { Success = false, Error = "Empty command" };

            commandLine = ResolveAlias(commandLine);

            var parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return new TerminalCommandRegistry.CommandResult { Success = false, Error = "Empty command" };

            var commandName = parts[0].ToLowerInvariant();
            var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

            AddLog($"> {commandLine}", TerminalLogType.Input);
            _history.Add(commandLine);
            _historyIndex = _history.Count;
            SaveHistory();

            var result = TerminalCommandRegistry.Execute(commandName, args);

            if (result.Success && !string.IsNullOrEmpty(result.Output))
                AddLog(result.Output, TerminalLogType.Info);
            else if (!result.Success)
                AddLog(result.Error, TerminalLogType.Error);

            return result;
        }

        public IReadOnlyList<TerminalLog> GetLogs() => _logs.AsReadOnly();

        public IReadOnlyList<string> GetHistory() => _history.AsReadOnly();

        // Alias API

        public IReadOnlyDictionary<string, string> GetAliases() => _aliases;

        public void SetAlias(string name, string command)
        {
            _aliases[name.ToLowerInvariant()] = command;
            SaveAliases();
        }

        public bool RemoveAlias(string name)
        {
            var removed = _aliases.Remove(name.ToLowerInvariant());
            if (removed) SaveAliases();
            return removed;
        }

        private string ResolveAlias(string input)
        {
            var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return input;

            var key = parts[0].ToLowerInvariant();
            if (!_aliases.TryGetValue(key, out var expanded)) return input;

            return parts.Length > 1 ? $"{expanded} {parts[1]}" : expanded;
        }

        // Alias persistence

        private void LoadAliases()
        {
            try
            {
                var path = AliasFilePath;
                if (!File.Exists(path)) return;

                foreach (var line in File.ReadAllLines(path))
                {
                    var sep = line.IndexOf('=');
                    if (sep <= 0) continue;
                    var name = line[..sep].Trim();
                    var command = line[(sep + 1)..].Trim();
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(command))
                        _aliases[name] = command;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Debug Terminal] Failed to load aliases: {ex.Message}");
            }
        }

        private void SaveAliases()
        {
            try
            {
                var lines = new List<string>(_aliases.Count);
                foreach (var kvp in _aliases)
                    lines.Add($"{kvp.Key}={kvp.Value}");
                File.WriteAllLines(AliasFilePath, lines);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Debug Terminal] Failed to save aliases: {ex.Message}");
            }
        }

        // History persistence

        private void LoadHistory()
        {
            if (!TerminalSettings.PersistHistory) return;

            try
            {
                var path = HistoryFilePath;
                if (!File.Exists(path)) return;

                var lines = File.ReadAllLines(path);
                _history.Clear();
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        _history.Add(line);
                }

                while (_history.Count > TerminalSettings.MaxHistoryEntries)
                    _history.RemoveAt(0);

                _historyIndex = _history.Count;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Debug Terminal] Failed to load history: {ex.Message}");
            }
        }

        private void SaveHistory()
        {
            if (!TerminalSettings.PersistHistory) return;

            try
            {
                var toSave = _history;
                if (toSave.Count > TerminalSettings.MaxHistoryEntries)
                {
                    toSave = _history.GetRange(
                        _history.Count - TerminalSettings.MaxHistoryEntries,
                        TerminalSettings.MaxHistoryEntries);
                }

                File.WriteAllLines(HistoryFilePath, toSave);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Debug Terminal] Failed to save history: {ex.Message}");
            }
        }

        // Styles

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            var fontSize = TerminalSettings.FontSize;

            _backgroundTexture = new Texture2D(1, 1);
            _backgroundTexture.SetPixel(0, 0, new Color(0.05f, 0.05f, 0.1f, 0.92f));
            _backgroundTexture.Apply();

            _logStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                wordWrap = true,
                richText = true,
                normal = { textColor = Color.white },
                padding = new RectOffset(0, 0, 1, 1),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _inputStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = fontSize,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) },
                focused = { textColor = new Color(0.9f, 0.95f, 1f) }
            };

            _stylesInitialized = true;
        }

        private static Color GetLogColor(TerminalLogType type) => type switch
        {
            TerminalLogType.Error => new Color(1f, 0.3f, 0.3f),
            TerminalLogType.Warning => new Color(1f, 0.9f, 0.3f),
            TerminalLogType.Input => new Color(0.5f, 0.8f, 1f),
            TerminalLogType.System => new Color(0.3f, 1f, 0.3f),
            _ => Color.white
        };

        private void DrawWatchPanel(Rect terminalRect)
        {
            var watches = WatchManager.Watches;
            if (watches.Count == 0) return;

            var lineHeight = TerminalSettings.FontSize + 4;
            var panelHeight = (watches.Count + 1) * lineHeight + Padding * 2;
            var panelY = terminalRect.yMax + 2;
            var panelRect = new Rect(0, panelY, Screen.width, panelHeight);

            GUI.DrawTexture(panelRect, _backgroundTexture);

            GUILayout.BeginArea(new Rect(Padding, panelY + Padding, panelRect.width - Padding * 2, panelHeight - Padding * 2));

            var headerStyle = new GUIStyle(_logStyle) { normal = { textColor = new Color(0.6f, 0.8f, 1f) } };
            GUILayout.Label("-- Watches --", headerStyle);

            foreach (var w in watches)
            {
                var prevColor = GUI.contentColor;
                GUI.contentColor = w.HasError ? new Color(1f, 0.3f, 0.3f) : new Color(0.9f, 0.95f, 0.8f);
                GUILayout.Label($"  #{w.Id} {w.ObjectName} {w.MemberPath} = {w.LastValue}", _logStyle);
                GUI.contentColor = prevColor;
            }

            GUILayout.EndArea();
        }
    }
}
