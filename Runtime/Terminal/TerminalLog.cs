using System;

namespace UnityMCPBridge.Terminal
{
    public enum TerminalLogType
    {
        Info,
        Warning,
        Error,
        Input,
        System
    }

    public sealed class TerminalLog
    {
        public string Text { get; }
        public TerminalLogType Type { get; }
        public DateTime Timestamp { get; }

        public TerminalLog(string text, TerminalLogType type)
        {
            Text = text;
            Type = type;
            Timestamp = DateTime.Now;
        }
    }
}
