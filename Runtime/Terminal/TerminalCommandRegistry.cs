using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityMCPBridge.Terminal
{
    public static class TerminalCommandRegistry
    {
        private static readonly Dictionary<string, CommandInfo> _commands = new();
        private static readonly object _lock = new();
        private static bool _initialized;
        private static List<string> _sortedNames;

        public static bool IsInitialized => _initialized;

        public static void Initialize()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                _commands.Clear();
                DiscoverCommands();
                _sortedNames = _commands.Keys.OrderBy(k => k).ToList();
                _initialized = true;

                Debug.Log($"[Debug Terminal] Discovered {_commands.Count} commands");
            }
        }

        public static CommandResult Execute(string commandName, string[] args)
        {
            if (string.IsNullOrEmpty(commandName))
                return new CommandResult { Success = false, Error = "Empty command name" };

            CommandInfo info;
            lock (_lock)
            {
                if (!_commands.TryGetValue(commandName, out info))
                    return new CommandResult { Success = false, Error = $"Unknown command: '{commandName}'. Type 'help' for a list of commands." };
            }

            try
            {
                var output = info.Handler(args);
                return new CommandResult { Success = true, Output = output };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Error = $"Error executing '{commandName}': {ex.Message}" };
            }
        }

        public static IReadOnlyList<string> GetCommandNames()
        {
            lock (_lock)
            {
                return _sortedNames ?? new List<string>();
            }
        }

        public static IReadOnlyList<string> GetCompletions(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return GetCommandNames();

            lock (_lock)
            {
                return _sortedNames?.Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList()
                       ?? new List<string>();
            }
        }

        public static CommandInfo GetCommand(string name)
        {
            lock (_lock)
            {
                _commands.TryGetValue(name, out var info);
                return info;
            }
        }

        /// <summary>
        /// Returns structured info for all registered commands.
        /// </summary>
        public static IReadOnlyList<CommandInfo> GetAllCommands()
        {
            lock (_lock)
            {
                return _commands.Values.OrderBy(c => c.Name).ToList();
            }
        }

        public static void Register(string name, string description, Func<string[], string> handler, string usage = null, string category = null)
        {
            if (string.IsNullOrEmpty(name) || handler == null) return;

            lock (_lock)
            {
                _commands[name.ToLowerInvariant()] = new CommandInfo(name.ToLowerInvariant(), description, handler, usage, category);
                _sortedNames = _commands.Keys.OrderBy(k => k).ToList();
            }
        }

        private static void DiscoverCommands()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Skip system/Unity assemblies for performance
                var assemblyName = assembly.GetName().Name;
                if (assemblyName.StartsWith("System") || assemblyName.StartsWith("Unity") ||
                    assemblyName.StartsWith("mscorlib") || assemblyName.StartsWith("Mono") ||
                    assemblyName.StartsWith("netstandard"))
                    continue;

                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            var attr = method.GetCustomAttribute<TerminalCommandAttribute>();
                            if (attr == null) continue;

                            // Validate signature: static string Method(string[] args)
                            var parameters = method.GetParameters();
                            if (method.ReturnType != typeof(string) || parameters.Length != 1 || parameters[0].ParameterType != typeof(string[]))
                            {
                                Debug.LogWarning($"[Debug Terminal] Skipping '{attr.Name}': invalid signature on {type.Name}.{method.Name}. Expected: static string Method(string[] args)");
                                continue;
                            }

                            var handler = (Func<string[], string>)Delegate.CreateDelegate(typeof(Func<string[], string>), method);
                            var cmdInfo = new CommandInfo(attr.Name.ToLowerInvariant(), attr.Description, handler, attr.Usage, attr.Category);

                            // Resolve argument completer if specified
                            if (!string.IsNullOrEmpty(attr.CompleterMethod))
                            {
                                var completer = ResolveCompleter(type, attr.CompleterMethod);
                                if (completer != null)
                                    cmdInfo.ArgumentCompleter = completer;
                                else
                                    Debug.LogWarning($"[Debug Terminal] Completer '{attr.CompleterMethod}' not found for command '{attr.Name}'");
                            }

                            _commands[attr.Name.ToLowerInvariant()] = cmdInfo;
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Some assemblies may fail to load types
                }
            }
        }

        /// <summary>
        /// Get argument completions for a command.
        /// </summary>
        public static IReadOnlyList<string> GetArgumentCompletions(string commandName, string[] currentArgs)
        {
            if (string.IsNullOrEmpty(commandName)) return Array.Empty<string>();

            CommandInfo info;
            lock (_lock)
            {
                if (!_commands.TryGetValue(commandName.ToLowerInvariant(), out info))
                    return Array.Empty<string>();
            }

            if (info.ArgumentCompleter == null)
                return Array.Empty<string>();

            try
            {
                return info.ArgumentCompleter(currentArgs) ?? (IReadOnlyList<string>)Array.Empty<string>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Debug Terminal] Argument completer error for '{commandName}': {ex.Message}");
                return Array.Empty<string>();
            }
        }

        private static Func<string[], IReadOnlyList<string>> ResolveCompleter(Type declaringType, string methodName)
        {
            // Search in the declaring type first, then in ArgumentCompleters
            var searchTypes = new[] { declaringType, typeof(ArgumentCompleters) };

            foreach (var searchType in searchTypes)
            {
                var method = searchType.GetMethod(methodName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (method == null) continue;

                // Validate signature: static IReadOnlyList<string> Method(string[] args)
                var parameters = method.GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string[])) continue;
                if (!typeof(IReadOnlyList<string>).IsAssignableFrom(method.ReturnType)) continue;

                return (Func<string[], IReadOnlyList<string>>)Delegate.CreateDelegate(
                    typeof(Func<string[], IReadOnlyList<string>>), method);
            }

            return null;
        }

        /// <summary>
        /// Forces re-initialization (useful after domain reload).
        /// </summary>
        public static void Reinitialize()
        {
            lock (_lock)
            {
                _initialized = false;
            }
            Initialize();
        }

        public sealed class CommandInfo
        {
            public string Name { get; }
            public string Description { get; }
            public string Usage { get; }
            public string Category { get; }
            public Func<string[], string> Handler { get; }

            /// <summary>
            /// Optional delegate that provides argument completions for this command.
            /// </summary>
            public Func<string[], IReadOnlyList<string>> ArgumentCompleter { get; set; }

            public CommandInfo(string name, string description, Func<string[], string> handler, string usage = null, string category = null)
            {
                Name = name;
                Description = description;
                Handler = handler;
                Usage = usage;
                Category = category ?? "builtin";
            }
        }

        public sealed class CommandResult
        {
            public bool Success { get; set; }
            public string Output { get; set; }
            public string Error { get; set; }
        }
    }
}
