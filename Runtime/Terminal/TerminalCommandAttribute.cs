using System;

namespace UnityMCPBridge.Terminal
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class TerminalCommandAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public string Usage { get; }

        /// <summary>
        /// Name of a static method that provides argument completions.
        /// Must have signature: static IReadOnlyList&lt;string&gt; Method(string[] currentArgs)
        /// </summary>
        public string CompleterMethod { get; set; }

        /// <summary>
        /// Command category for grouping and filtering (e.g., "builtin", "game", "debug").
        /// When not specified, defaults to "builtin".
        /// </summary>
        public string Category { get; set; }

        public TerminalCommandAttribute(string name, string description, string usage = null)
        {
            Name = name;
            Description = description;
            Usage = usage;
        }
    }
}
