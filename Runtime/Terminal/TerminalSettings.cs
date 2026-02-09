using UnityEngine;

namespace UnityMCPBridge.Terminal
{
    public static class TerminalSettings
    {
        private static KeyCode _toggleKey = KeyCode.BackQuote;
        private static bool _enabledInBuilds;
        private static int _maxLogEntries = 200;
        private static int _fontSize = 14;
        private static int _maxHistoryEntries = 100;
        private static bool _persistHistory = true;

        public static KeyCode ToggleKey
        {
            get => _toggleKey;
            set => _toggleKey = value;
        }

        public static bool EnabledInBuilds
        {
            get => _enabledInBuilds;
            set => _enabledInBuilds = value;
        }

        public static int MaxLogEntries
        {
            get => _maxLogEntries;
            set => _maxLogEntries = Mathf.Max(50, value);
        }

        public static int FontSize
        {
            get => _fontSize;
            set => _fontSize = Mathf.Clamp(value, 10, 24);
        }

        public static int MaxHistoryEntries
        {
            get => _maxHistoryEntries;
            set => _maxHistoryEntries = Mathf.Clamp(value, 10, 1000);
        }

        public static bool PersistHistory
        {
            get => _persistHistory;
            set => _persistHistory = value;
        }

        public static void ResetToDefaults()
        {
            _toggleKey = KeyCode.BackQuote;
            _enabledInBuilds = false;
            _maxLogEntries = 200;
            _fontSize = 14;
            _maxHistoryEntries = 100;
            _persistHistory = true;
        }
    }
}
