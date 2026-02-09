using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityMCPBridge.Terminal
{
    /// <summary>
    /// Provides argument completion for terminal commands.
    /// Methods here are referenced by name via TerminalCommandAttribute.CompleterMethod.
    /// </summary>
    public static class ArgumentCompleters
    {
        /// <summary>
        /// Complete GameObject names from the active scene.
        /// </summary>
        public static IReadOnlyList<string> GameObjectNames(string[] currentArgs)
        {
            var prefix = currentArgs.Length > 0 ? string.Join(" ", currentArgs) : "";
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var names = new List<string>();

            foreach (var root in roots)
                CollectNames(root.transform, names);

            if (string.IsNullOrEmpty(prefix))
                return names;

            return names.Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Complete scene names from build settings.
        /// </summary>
        public static IReadOnlyList<string> SceneNames(string[] currentArgs)
        {
            var prefix = currentArgs.Length > 0 ? currentArgs[0] : "";
            var names = new List<string>();

            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                names.Add(name);
            }

            if (string.IsNullOrEmpty(prefix))
                return names;

            return names.Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Complete existing alias names.
        /// </summary>
        public static IReadOnlyList<string> AliasNames(string[] currentArgs)
        {
            var terminal = DebugTerminal.Instance;
            if (terminal == null) return Array.Empty<string>();

            var prefix = currentArgs.Length > 0 ? currentArgs[0] : "";
            var aliases = terminal.GetAliases().Keys;

            if (string.IsNullOrEmpty(prefix))
                return aliases.OrderBy(a => a).ToList();

            return aliases.Where(a => a.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a).ToList();
        }

        /// <summary>
        /// Complete command names (for help command).
        /// </summary>
        public static IReadOnlyList<string> CommandNames(string[] currentArgs)
        {
            var prefix = currentArgs.Length > 0 ? currentArgs[0] : "";
            return TerminalCommandRegistry.GetCompletions(prefix);
        }

        /// <summary>
        /// Complete toggle names for debug.toggle.
        /// </summary>
        public static IReadOnlyList<string> ToggleNames(string[] currentArgs)
        {
            var prefix = currentArgs.Length > 0 ? currentArgs[0] : "";
            var names = DebugToggles.GetNames();

            if (string.IsNullOrEmpty(prefix))
                return names;

            return names.Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Complete for obj.get/obj.set: first arg = GameObject name, second = Component.property path.
        /// </summary>
        public static IReadOnlyList<string> ComponentPath(string[] currentArgs)
        {
            // No args yet: suggest GameObject names
            if (currentArgs.Length == 0)
                return GameObjectNames(currentArgs);

            // If last arg contains a dot, we're completing a member path
            var lastArg = currentArgs[^1];
            if (lastArg.Contains('.'))
            {
                // Everything before last arg = object name
                var objName = string.Join(" ", currentArgs[..^1]);
                if (string.IsNullOrEmpty(objName))
                {
                    // Last arg is the component path, no object name yet
                    return Array.Empty<string>();
                }

                return CompleteMemberPath(objName, lastArg);
            }

            // Check if we have a valid object name followed by a partial component name
            // Try progressively shorter object name interpretations
            for (var i = currentArgs.Length - 1; i >= 1; i--)
            {
                var objName = string.Join(" ", currentArgs[..i]);
                var obj = GameObject.Find(objName);
                if (obj != null)
                {
                    // Remaining args form the component path prefix
                    var pathPrefix = string.Join(" ", currentArgs[i..]);
                    var components = obj.GetComponents<Component>()
                        .Where(c => c != null)
                        .Select(c => c.GetType().Name)
                        .Distinct()
                        .Where(n => string.IsNullOrEmpty(pathPrefix) ||
                                    n.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(n => n)
                        .ToList();
                    return components;
                }
            }

            // Default: suggest GameObject names using all current args as prefix
            return GameObjectNames(currentArgs);
        }

        private static IReadOnlyList<string> CompleteMemberPath(string objectName, string partialPath)
        {
            var obj = GameObject.Find(objectName);
            if (obj == null) return Array.Empty<string>();

            var parts = partialPath.Split('.');

            // First part is component type name
            var component = FindComponent(obj, parts[0]);
            if (component == null)
            {
                // Complete component name
                return obj.GetComponents<Component>()
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name)
                    .Distinct()
                    .Where(n => n.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase))
                    .OrderBy(n => n)
                    .ToList();
            }

            // Navigate to the parent of the last part
            var currentType = component.GetType();
            for (var i = 1; i < parts.Length - 1; i++)
            {
                var member = FindMember(currentType, parts[i]);
                if (member == null) return Array.Empty<string>();
                currentType = GetMemberType(member);
            }

            // Complete the last part
            var lastPart = parts[^1];
            var basePath = string.Join(".", parts[..^1]);

            var completions = new List<string>();

            // Properties
            var props = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .Where(p => p.Name.StartsWith(lastPart, StringComparison.OrdinalIgnoreCase))
                .Select(p => $"{basePath}.{p.Name}");
            completions.AddRange(props);

            // Public fields
            var fields = currentType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.Name.StartsWith(lastPart, StringComparison.OrdinalIgnoreCase))
                .Select(f => $"{basePath}.{f.Name}");
            completions.AddRange(fields);

            return completions.OrderBy(c => c).ToList();
        }

        private static Component FindComponent(GameObject obj, string name)
        {
            return obj.GetComponents<Component>()
                .FirstOrDefault(c => c != null &&
                    string.Equals(c.GetType().Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static MemberInfo FindMember(Type type, string name)
        {
            var prop = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (prop != null) return prop;

            return type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static Type GetMemberType(MemberInfo member)
        {
            return member switch
            {
                PropertyInfo prop => prop.PropertyType,
                FieldInfo field => field.FieldType,
                _ => typeof(object)
            };
        }

        private static void CollectNames(Transform root, List<string> names)
        {
            names.Add(root.name);
            for (var i = 0; i < root.childCount; i++)
                CollectNames(root.GetChild(i), names);
        }
    }
}
