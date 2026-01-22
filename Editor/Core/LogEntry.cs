using System;
using UnityEngine;

namespace UnityMCPBridge.Core
{
    /// <summary>
    /// Represents a single log entry from Unity console.
    /// </summary>
    [Serializable]
    public sealed class LogEntry
    {
        public string Message { get; }
        public string StackTrace { get; }
        public LogType Type { get; }
        public DateTime Timestamp { get; }

        public LogEntry(string message, string stackTrace, LogType type)
        {
            Message = message ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
            Type = type;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Converts log type to a simple string representation for JSON serialization.
        /// </summary>
        public string TypeString => Type switch
        {
            LogType.Error => "error",
            LogType.Assert => "assert",
            LogType.Warning => "warning",
            LogType.Log => "log",
            LogType.Exception => "exception",
            _ => "unknown"
        };
    }
}
