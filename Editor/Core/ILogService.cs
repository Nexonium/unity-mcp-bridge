using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityMCPBridge.Core
{
    /// <summary>
    /// Service for collecting and providing access to Unity console logs.
    /// </summary>
    public interface ILogService : IService
    {
        /// <summary>
        /// Event raised when a new log entry is received.
        /// </summary>
        event Action<LogEntry> OnLogReceived;

        /// <summary>
        /// Gets all stored log entries.
        /// </summary>
        IReadOnlyList<LogEntry> GetLogs();

        /// <summary>
        /// Gets log entries filtered by type.
        /// </summary>
        /// <param name="type">The log type to filter by.</param>
        IReadOnlyList<LogEntry> GetLogs(LogType type);

        /// <summary>
        /// Gets log entries since a specific timestamp.
        /// </summary>
        /// <param name="since">UTC timestamp to filter from.</param>
        IReadOnlyList<LogEntry> GetLogsSince(DateTime since);

        /// <summary>
        /// Clears all stored log entries.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets or sets the maximum number of log entries to store.
        /// </summary>
        int MaxEntries { get; set; }
    }
}
