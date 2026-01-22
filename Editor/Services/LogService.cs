using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityMCPBridge.Core;
using UnityMCPBridge.Settings;

namespace UnityMCPBridge.Services
{
    /// <summary>
    /// Service that collects Unity console logs and provides access to them.
    /// Implements a circular buffer to limit memory usage.
    /// </summary>
    public sealed class LogService : ILogService
    {
        private readonly object _lock = new();
        private readonly LinkedList<LogEntry> _logs = new();
        private int _maxEntries;

        public bool IsRunning { get; private set; }

        public event Action<LogEntry> OnLogReceived;

        public int MaxEntries
        {
            get => _maxEntries;
            set
            {
                _maxEntries = Math.Max(10, value);
                TrimExcessEntries();
            }
        }

        public LogService()
        {
            _maxEntries = MCPBridgeSettings.MaxLogEntries;
        }

        public void Start()
        {
            if (IsRunning) return;

            Application.logMessageReceived += HandleLogMessage;
            IsRunning = true;
            Debug.Log("[MCP Bridge] Log service started");
        }

        public void Stop()
        {
            if (!IsRunning) return;

            Application.logMessageReceived -= HandleLogMessage;
            IsRunning = false;
            Debug.Log("[MCP Bridge] Log service stopped");
        }

        public IReadOnlyList<LogEntry> GetLogs()
        {
            lock (_lock)
            {
                return _logs.ToList();
            }
        }

        public IReadOnlyList<LogEntry> GetLogs(LogType type)
        {
            lock (_lock)
            {
                return _logs.Where(e => e.Type == type).ToList();
            }
        }

        public IReadOnlyList<LogEntry> GetLogsSince(DateTime since)
        {
            lock (_lock)
            {
                return _logs.Where(e => e.Timestamp > since).ToList();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _logs.Clear();
            }
        }

        private void HandleLogMessage(string message, string stackTrace, LogType type)
        {
            // Skip our own log messages to avoid recursion
            if (message.StartsWith("[MCP Bridge]")) return;

            var entry = new LogEntry(
                message,
                MCPBridgeSettings.IncludeStackTrace ? stackTrace : null,
                type
            );

            lock (_lock)
            {
                _logs.AddLast(entry);
                TrimExcessEntries();
            }

            OnLogReceived?.Invoke(entry);
        }

        private void TrimExcessEntries()
        {
            lock (_lock)
            {
                while (_logs.Count > _maxEntries)
                {
                    _logs.RemoveFirst();
                }
            }
        }
    }
}
