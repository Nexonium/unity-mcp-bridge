using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityMCPBridge.Core;
using UnityMCPBridge.Settings;

namespace UnityMCPBridge.Services
{
    /// <summary>
    /// Service that tracks Unity compilation errors and warnings.
    /// </summary>
    public sealed class CompilationService : ICompilationService
    {
        private readonly object _lock = new();
        private readonly List<CompilationError> _errors = new();
        private readonly List<CompilationError> _warnings = new();
        private bool _isCompiling;

        public bool IsRunning { get; private set; }
        
        public bool IsCompiling
        {
            get
            {
                lock (_lock) { return _isCompiling; }
            }
            private set
            {
                lock (_lock) { _isCompiling = value; }
            }
        }

        public bool HasErrors
        {
            get
            {
                lock (_lock)
                {
                    return _errors.Count > 0;
                }
            }
        }

        public bool HasWarnings
        {
            get
            {
                lock (_lock)
                {
                    return _warnings.Count > 0;
                }
            }
        }

        public event Action OnCompilationStarted;
        public event Action<bool> OnCompilationFinished;
        public event Action<CompilationError> OnErrorReceived;

        public void Start()
        {
            if (IsRunning) return;

            CompilationPipeline.compilationStarted += HandleCompilationStarted;
            CompilationPipeline.compilationFinished += HandleCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += HandleAssemblyCompilationFinished;

            IsRunning = true;
            Debug.Log("[MCP Bridge] Compilation service started");
        }

        public void Stop()
        {
            if (!IsRunning) return;

            CompilationPipeline.compilationStarted -= HandleCompilationStarted;
            CompilationPipeline.compilationFinished -= HandleCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished -= HandleAssemblyCompilationFinished;

            IsRunning = false;
            Debug.Log("[MCP Bridge] Compilation service stopped");
        }

        public IReadOnlyList<CompilationError> GetErrors()
        {
            lock (_lock)
            {
                return _errors.ToList();
            }
        }

        public IReadOnlyList<CompilationError> GetWarnings()
        {
            lock (_lock)
            {
                return _warnings.ToList();
            }
        }

        private void HandleCompilationStarted(object context)
        {
            IsCompiling = true;

            lock (_lock)
            {
                _errors.Clear();
                _warnings.Clear();
            }

            OnCompilationStarted?.Invoke();
        }

        private void HandleCompilationFinished(object context)
        {
            IsCompiling = false;
            OnCompilationFinished?.Invoke(!HasErrors);
        }

        private void HandleAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            if (messages == null || messages.Length == 0) return;

            var assemblyName = System.IO.Path.GetFileNameWithoutExtension(assemblyPath);
            var includeWarnings = MCPBridgeSettings.IncludeWarnings;
            
            // Collect errors to notify outside of lock to prevent potential deadlocks
            var errorsToNotify = new List<CompilationError>();

            lock (_lock)
            {
                foreach (var message in messages)
                {
                    var error = new CompilationError(message, assemblyName);

                    if (error.IsWarning)
                    {
                        if (includeWarnings)
                        {
                            _warnings.Add(error);
                            errorsToNotify.Add(error);
                        }
                    }
                    else
                    {
                        _errors.Add(error);
                        errorsToNotify.Add(error);
                    }
                }
            }
            
            // Invoke events outside of lock to prevent deadlocks
            foreach (var error in errorsToNotify)
            {
                OnErrorReceived?.Invoke(error);
            }
        }
    }
}
