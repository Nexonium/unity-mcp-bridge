using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityMCPBridge.Terminal
{
    /// <summary>
    /// Named boolean toggle registry for debug visualizations.
    /// Game code can query toggles to show/hide debug overlays.
    /// </summary>
    public static class DebugToggles
    {
        private static readonly Dictionary<string, bool> _toggles = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _descriptions = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Fired when any toggle changes. Args: (name, newValue).
        /// </summary>
        public static event Action<string, bool> OnToggleChanged;

        /// <summary>
        /// Register a toggle with a description. Does not overwrite existing value.
        /// </summary>
        public static void Register(string name, string description, bool defaultValue = false)
        {
            if (string.IsNullOrEmpty(name)) return;

            if (!_toggles.ContainsKey(name))
                _toggles[name] = defaultValue;

            _descriptions[name] = description ?? "";
        }

        /// <summary>
        /// Get a toggle value. Returns false if not registered.
        /// </summary>
        public static bool Get(string name)
        {
            return !string.IsNullOrEmpty(name) && _toggles.TryGetValue(name, out var val) && val;
        }

        /// <summary>
        /// Set a toggle value. Auto-registers if not yet registered.
        /// </summary>
        public static void Set(string name, bool value)
        {
            if (string.IsNullOrEmpty(name)) return;

            var changed = !_toggles.TryGetValue(name, out var prev) || prev != value;
            _toggles[name] = value;

            if (changed)
                OnToggleChanged?.Invoke(name, value);
        }

        /// <summary>
        /// Toggle a value. Auto-registers if not yet registered. Returns new value.
        /// </summary>
        public static bool Toggle(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            var current = Get(name);
            Set(name, !current);
            return !current;
        }

        /// <summary>
        /// List all registered toggles.
        /// </summary>
        public static string ListAll()
        {
            if (_toggles.Count == 0)
                return "No debug toggles registered.";

            var sb = new StringBuilder();
            sb.AppendLine($"Debug Toggles ({_toggles.Count}):");

            foreach (var kvp in _toggles.OrderBy(k => k.Key))
            {
                var state = kvp.Value ? "ON" : "OFF";
                var desc = _descriptions.TryGetValue(kvp.Key, out var d) && !string.IsNullOrEmpty(d) ? $" - {d}" : "";
                sb.AppendLine($"  [{state}] {kvp.Key}{desc}");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Get all toggle names for autocomplete.
        /// </summary>
        public static IReadOnlyList<string> GetNames()
        {
            return _toggles.Keys.OrderBy(k => k).ToList();
        }

        /// <summary>
        /// Clear all toggles. Useful on domain reload.
        /// </summary>
        public static void Clear()
        {
            _toggles.Clear();
            _descriptions.Clear();
        }
    }
}
