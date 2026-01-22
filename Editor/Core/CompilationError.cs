using System;
using UnityEditor.Compilation;

namespace UnityMCPBridge.Core
{
    /// <summary>
    /// Represents a compilation error or warning from Unity's compilation pipeline.
    /// </summary>
    [Serializable]
    public sealed class CompilationError
    {
        public string Message { get; }
        public string File { get; }
        public int Line { get; }
        public int Column { get; }
        public bool IsWarning { get; }
        public string AssemblyName { get; }
        public DateTime Timestamp { get; }

        public CompilationError(CompilerMessage message, string assemblyName)
        {
            Message = message.message ?? string.Empty;
            File = message.file ?? string.Empty;
            Line = message.line;
            Column = message.column;
            IsWarning = message.type == CompilerMessageType.Warning;
            AssemblyName = assemblyName ?? string.Empty;
            Timestamp = DateTime.UtcNow;
        }

        public string Severity => IsWarning ? "warning" : "error";
    }
}
