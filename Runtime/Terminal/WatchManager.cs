using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityMCPBridge.Terminal
{
    /// <summary>
    /// Manages watch expressions that are evaluated each frame.
    /// Expressions use the format: GameObjectName ComponentType.property.subfield
    /// </summary>
    public static class WatchManager
    {
        private static readonly List<WatchEntry> _watches = new();
        private static float _updateInterval = 0.1f;
        private static float _lastUpdate;

        public static IReadOnlyList<WatchEntry> Watches => _watches;
        public static bool HasWatches => _watches.Count > 0;

        public static float UpdateInterval
        {
            get => _updateInterval;
            set => _updateInterval = Mathf.Max(0.016f, value);
        }

        /// <summary>
        /// Add a watch expression. Returns the watch ID.
        /// Format: "GameObjectName ComponentType.property.subfield"
        /// </summary>
        public static int Add(string objectName, string memberPath)
        {
            if (string.IsNullOrEmpty(objectName) || string.IsNullOrEmpty(memberPath))
                return -1;

            var entry = new WatchEntry
            {
                Id = _watches.Count > 0 ? _watches.Max(w => w.Id) + 1 : 0,
                ObjectName = objectName,
                MemberPath = memberPath,
                LastValue = "...",
                HasError = false
            };

            _watches.Add(entry);
            Evaluate(entry);
            return entry.Id;
        }

        /// <summary>
        /// Remove a watch by ID. Returns true if removed.
        /// </summary>
        public static bool Remove(int id)
        {
            var index = _watches.FindIndex(w => w.Id == id);
            if (index < 0) return false;
            _watches.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Remove all watches.
        /// </summary>
        public static void Clear()
        {
            _watches.Clear();
        }

        /// <summary>
        /// Called from DebugTerminal.Update() to refresh watch values.
        /// </summary>
        public static void UpdateAll()
        {
            if (_watches.Count == 0) return;
            if (Time.unscaledTime - _lastUpdate < _updateInterval) return;

            _lastUpdate = Time.unscaledTime;

            for (var i = 0; i < _watches.Count; i++)
                Evaluate(_watches[i]);
        }

        /// <summary>
        /// Get a formatted string of all watch values for display.
        /// </summary>
        public static string FormatAll()
        {
            if (_watches.Count == 0)
                return "No active watches.";

            var sb = new StringBuilder();
            sb.AppendLine($"Watches ({_watches.Count}):");

            foreach (var w in _watches)
            {
                var status = w.HasError ? "[ERR]" : "     ";
                sb.AppendLine($"  #{w.Id} {status} {w.ObjectName} {w.MemberPath} = {w.LastValue}");
            }

            return sb.ToString().TrimEnd();
        }

        private static void Evaluate(WatchEntry entry)
        {
            try
            {
                var obj = GameObject.Find(entry.ObjectName);
                if (obj == null)
                {
                    // Try inactive objects
                    var all = Resources.FindObjectsOfTypeAll<GameObject>();
                    obj = all.FirstOrDefault(o => o.name == entry.ObjectName && o.scene.isLoaded);
                }

                if (obj == null)
                {
                    entry.LastValue = "<object not found>";
                    entry.HasError = true;
                    return;
                }

                var result = ReflectionResolver.GetValue(obj, entry.MemberPath);
                if (result.Success)
                {
                    entry.LastValue = result.Display;
                    entry.HasError = false;
                }
                else
                {
                    entry.LastValue = result.Error;
                    entry.HasError = true;
                }
            }
            catch (Exception ex)
            {
                entry.LastValue = $"<{ex.Message}>";
                entry.HasError = true;
            }
        }

        public class WatchEntry
        {
            public int Id;
            public string ObjectName;
            public string MemberPath;
            public string LastValue;
            public bool HasError;
        }
    }
}
