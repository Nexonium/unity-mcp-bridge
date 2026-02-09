using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace UnityMCPBridge.Terminal.Commands
{
    public static class BuiltInCommands
    {
        [TerminalCommand("help", "List available commands or show details for a specific command", "help [command]", CompleterMethod = "CommandNames")]
        public static string Help(string[] args)
        {
            if (args.Length > 0)
            {
                var info = TerminalCommandRegistry.GetCommand(args[0].ToLowerInvariant());
                if (info == null)
                    return $"Unknown command: '{args[0]}'";

                var sb = new StringBuilder();
                sb.AppendLine($"  {info.Name} - {info.Description}");
                if (!string.IsNullOrEmpty(info.Usage))
                    sb.AppendLine($"  Usage: {info.Usage}");
                return sb.ToString().TrimEnd();
            }

            var names = TerminalCommandRegistry.GetCommandNames();
            var result = new StringBuilder();
            result.AppendLine("Available commands:");
            foreach (var name in names)
            {
                var cmd = TerminalCommandRegistry.GetCommand(name);
                result.AppendLine($"  {name,-16} {cmd?.Description}");
            }
            result.Append("Type 'help <command>' for details.");
            return result.ToString();
        }

        [TerminalCommand("clear", "Clear terminal output")]
        public static string Clear(string[] args)
        {
            DebugTerminal.Instance?.ClearLog();
            return null;
        }

        [TerminalCommand("echo", "Echo text back", "echo <text>")]
        public static string Echo(string[] args)
        {
            return args.Length > 0 ? string.Join(" ", args) : "";
        }

        [TerminalCommand("scene", "Show current scene or load a scene", "scene [name|index]", CompleterMethod = "SceneNames")]
        public static string Scene(string[] args)
        {
            if (args.Length == 0)
            {
                var active = SceneManager.GetActiveScene();
                var sb = new StringBuilder();
                sb.AppendLine($"Active scene: {active.name}");
                sb.AppendLine($"  Path: {active.path}");
                sb.AppendLine($"  Build index: {active.buildIndex}");
                sb.Append($"  Loaded scenes: {SceneManager.sceneCount}");
                return sb.ToString();
            }

            var sceneName = args[0];
            try
            {
                if (int.TryParse(sceneName, out var index))
                    SceneManager.LoadScene(index);
                else
                    SceneManager.LoadScene(sceneName);
                return $"Loading scene: {sceneName}";
            }
            catch (Exception ex)
            {
                return $"Failed to load scene '{sceneName}': {ex.Message}";
            }
        }

        [TerminalCommand("time.scale", "Get or set Time.timeScale", "time.scale [value]")]
        public static string TimeScale(string[] args)
        {
            if (args.Length == 0)
                return $"Time.timeScale = {Time.timeScale}";

            if (!float.TryParse(args[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var value))
                return $"Invalid value: '{args[0]}'. Expected a number.";

            value = Mathf.Clamp(value, 0f, 100f);
            Time.timeScale = value;
            return $"Time.timeScale = {value}";
        }

        [TerminalCommand("fps", "Show current FPS")]
        public static string Fps(string[] args)
        {
            var fps = 1f / Time.unscaledDeltaTime;
            return $"{fps:F1} FPS (frame time: {Time.unscaledDeltaTime * 1000f:F1}ms)";
        }

        [TerminalCommand("obj.list", "List root GameObjects in active scene")]
        public static string ObjList(string[] args)
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            if (roots.Length == 0)
                return "No root objects in active scene.";

            var sb = new StringBuilder();
            sb.AppendLine($"Root objects ({roots.Length}):");
            foreach (var root in roots)
            {
                var activeMarker = root.activeSelf ? "+" : "-";
                sb.AppendLine($"  [{activeMarker}] {root.name} ({root.transform.childCount} children)");
            }
            return sb.ToString().TrimEnd();
        }

        [TerminalCommand("obj.find", "Find a GameObject by name", "obj.find <name>", CompleterMethod = "GameObjectNames")]
        public static string ObjFind(string[] args)
        {
            if (args.Length == 0)
                return "Usage: obj.find <name>";

            var name = string.Join(" ", args);
            var obj = GameObject.Find(name);
            if (obj == null)
            {
                // Try inactive objects
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                obj = all.FirstOrDefault(o => o.name == name && o.scene.isLoaded);
            }

            if (obj == null)
                return $"Object not found: '{name}'";

            var sb = new StringBuilder();
            sb.AppendLine($"Found: {GetPath(obj.transform)}");
            sb.AppendLine($"  Active: {obj.activeSelf} (in hierarchy: {obj.activeInHierarchy})");
            sb.AppendLine($"  Layer: {LayerMask.LayerToName(obj.layer)} ({obj.layer})");
            sb.AppendLine($"  Tag: {obj.tag}");
            sb.AppendLine($"  Children: {obj.transform.childCount}");
            sb.Append($"  Components: {string.Join(", ", obj.GetComponents<Component>().Select(c => c.GetType().Name))}");
            return sb.ToString();
        }

        [TerminalCommand("obj.inspect", "Inspect a GameObject's components", "obj.inspect <name>", CompleterMethod = "GameObjectNames")]
        public static string ObjInspect(string[] args)
        {
            if (args.Length == 0)
                return "Usage: obj.inspect <name>";

            var name = string.Join(" ", args);
            var obj = GameObject.Find(name);
            if (obj == null)
                return $"Object not found: '{name}'";

            var sb = new StringBuilder();
            sb.AppendLine($"Components on '{obj.name}':");
            foreach (var comp in obj.GetComponents<Component>())
            {
                if (comp == null) continue;
                var type = comp.GetType();
                sb.AppendLine($"  [{type.Name}]");

                if (comp is Behaviour behaviour)
                    sb.AppendLine($"    enabled: {behaviour.enabled}");

                if (comp is Renderer renderer)
                {
                    sb.AppendLine($"    material: {renderer.sharedMaterial?.name ?? "none"}");
                    sb.AppendLine($"    bounds: {renderer.bounds}");
                }

                if (comp is Collider collider)
                    sb.AppendLine($"    isTrigger: {collider.isTrigger}");
            }
            return sb.ToString().TrimEnd();
        }

        [TerminalCommand("obj.toggle", "Toggle a GameObject active/inactive", "obj.toggle <name>", CompleterMethod = "GameObjectNames")]
        public static string ObjToggle(string[] args)
        {
            if (args.Length == 0)
                return "Usage: obj.toggle <name>";

            var name = string.Join(" ", args);
            var obj = GameObject.Find(name);

            // Also try inactive
            if (obj == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                obj = all.FirstOrDefault(o => o.name == name && o.scene.isLoaded);
            }

            if (obj == null)
                return $"Object not found: '{name}'";

            obj.SetActive(!obj.activeSelf);
            return $"'{obj.name}' is now {(obj.activeSelf ? "active" : "inactive")}";
        }

        [TerminalCommand("log", "Write a message to Unity Debug.Log", "log <message>")]
        public static string Log(string[] args)
        {
            if (args.Length == 0)
                return "Usage: log <message>";

            var message = string.Join(" ", args);
            Debug.Log($"[Terminal] {message}");
            return $"Logged: {message}";
        }

        [TerminalCommand("gc", "Force garbage collection and show memory")]
        public static string Gc(string[] args)
        {
            var before = GC.GetTotalMemory(false) / 1024f / 1024f;
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            var after = GC.GetTotalMemory(true) / 1024f / 1024f;
            return $"GC collected. Memory: {before:F1}MB -> {after:F1}MB (freed {before - after:F1}MB)";
        }

        [TerminalCommand("mem", "Show detailed memory usage")]
        public static string Mem(string[] args)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Memory Usage:");
            sb.AppendLine($"  Managed heap:  {GC.GetTotalMemory(false) / 1048576f:F1} MB");
            sb.AppendLine($"  Total allocated: {Profiler.GetTotalAllocatedMemoryLong() / 1048576f:F1} MB");
            sb.AppendLine($"  Total reserved:  {Profiler.GetTotalReservedMemoryLong() / 1048576f:F1} MB");
            sb.AppendLine($"  Unused reserved: {Profiler.GetTotalUnusedReservedMemoryLong() / 1048576f:F1} MB");
            sb.AppendLine($"  Mono heap:       {Profiler.GetMonoHeapSizeLong() / 1048576f:F1} MB");
            sb.AppendLine($"  Mono used:       {Profiler.GetMonoUsedSizeLong() / 1048576f:F1} MB");
            sb.AppendLine($"  GC collections:  Gen0={GC.CollectionCount(0)} Gen1={GC.CollectionCount(1)} Gen2={GC.CollectionCount(2)}");
            sb.Append($"  Texture memory:  {Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576f:F1} MB");
            return sb.ToString();
        }

        [TerminalCommand("sysinfo", "Show system and runtime information")]
        public static string SysInfo(string[] args)
        {
            var sb = new StringBuilder();
            sb.AppendLine("System Info:");
            sb.AppendLine($"  CPU:         {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
            sb.AppendLine($"  RAM:         {SystemInfo.systemMemorySize} MB");
            sb.AppendLine($"  GPU:         {SystemInfo.graphicsDeviceName}");
            sb.AppendLine($"  VRAM:        {SystemInfo.graphicsMemorySize} MB");
            sb.AppendLine($"  Graphics API: {SystemInfo.graphicsDeviceType}");
            sb.AppendLine($"  Resolution:  {Screen.currentResolution.width}x{Screen.currentResolution.height}@{Screen.currentResolution.refreshRateRatio}");
            sb.AppendLine($"  Quality:     {QualitySettings.names[QualitySettings.GetQualityLevel()]} (level {QualitySettings.GetQualityLevel()})");
            sb.AppendLine($"  Target FPS:  {Application.targetFrameRate}");
            sb.AppendLine($"  VSync:       {QualitySettings.vSyncCount}");
            sb.Append($"  Platform:    {Application.platform} ({SystemInfo.operatingSystem})");
            return sb.ToString();
        }

        [TerminalCommand("alias", "Create or list command aliases", "alias [name] [command]", CompleterMethod = "AliasNames")]
        public static string Alias(string[] args)
        {
            var terminal = DebugTerminal.Instance;
            if (terminal == null)
                return "Terminal not active.";

            var aliases = terminal.GetAliases();

            if (args.Length == 0)
            {
                if (aliases.Count == 0)
                    return "No aliases defined. Usage: alias <name> <command>";

                var sb = new StringBuilder();
                sb.AppendLine($"Aliases ({aliases.Count}):");
                foreach (var kvp in aliases)
                    sb.AppendLine($"  {kvp.Key} = {kvp.Value}");
                return sb.ToString().TrimEnd();
            }

            if (args.Length == 1)
            {
                var name = args[0].ToLowerInvariant();
                if (aliases.TryGetValue(name, out var existing))
                    return $"  {name} = {existing}";
                return $"No alias '{name}'. Usage: alias <name> <command>";
            }

            var aliasName = args[0].ToLowerInvariant();
            var command = string.Join(" ", args[1..]);
            terminal.SetAlias(aliasName, command);
            return $"Alias set: {aliasName} = {command}";
        }

        [TerminalCommand("unalias", "Remove a command alias", "unalias <name>", CompleterMethod = "AliasNames")]
        public static string Unalias(string[] args)
        {
            var terminal = DebugTerminal.Instance;
            if (terminal == null)
                return "Terminal not active.";

            if (args.Length == 0)
                return "Usage: unalias <name>";

            var name = args[0].ToLowerInvariant();
            return terminal.RemoveAlias(name)
                ? $"Alias '{name}' removed."
                : $"No alias '{name}'.";
        }

        [TerminalCommand("logs.capture", "Toggle Unity log capture on/off", "logs.capture [on|off]")]
        public static string LogsCapture(string[] args)
        {
            var terminal = DebugTerminal.Instance;
            if (terminal == null)
                return "Terminal not active.";

            if (args.Length == 0)
                return $"Unity log capture is {(terminal.CaptureUnityLogs ? "ON" : "OFF")}";

            var toggle = args[0].ToLowerInvariant();
            return toggle switch
            {
                "on" or "true" or "1" => SetCapture(terminal, true),
                "off" or "false" or "0" => SetCapture(terminal, false),
                _ => $"Invalid argument: '{args[0]}'. Use 'on' or 'off'."
            };

            static string SetCapture(DebugTerminal t, bool value)
            {
                t.CaptureUnityLogs = value;
                return $"Unity log capture {(value ? "enabled" : "disabled")}.";
            }
        }

        // -- Component get/set --

        [TerminalCommand("obj.get", "Get a component property value", "obj.get <name> <Component.property[.sub]>", CompleterMethod = "ComponentPath")]
        public static string ObjGet(string[] args)
        {
            if (args.Length < 2)
                return "Usage: obj.get <objectName> <Component.property[.sub]>\nExample: obj.get Main Camera Transform.position.x";

            // Last arg containing a dot is the member path
            var pathIndex = FindMemberPathIndex(args);
            if (pathIndex < 0)
                return "No component path found. Use format: obj.get <name> Component.property";

            var objectName = string.Join(" ", args[..pathIndex]);
            var memberPath = args[pathIndex];

            var obj = FindGameObject(objectName);
            if (obj == null)
                return $"Object not found: '{objectName}'";

            var result = ReflectionResolver.GetValue(obj, memberPath);
            return result.Success ? $"{memberPath} = {result.Display}" : result.Error;
        }

        [TerminalCommand("obj.set", "Set a component property value", "obj.set <name> <Component.property[.sub]> <value>", CompleterMethod = "ComponentPath")]
        public static string ObjSet(string[] args)
        {
            if (args.Length < 3)
                return "Usage: obj.set <objectName> <Component.property[.sub]> <value>\nExample: obj.set Main Camera Transform.position.x 5";

            // Last arg is the value, second-to-last containing a dot is the member path
            var value = args[^1];
            var pathIndex = FindMemberPathIndex(args, skipLast: 1);
            if (pathIndex < 0)
                return "No component path found. Use format: obj.set <name> Component.property value";

            var objectName = string.Join(" ", args[..pathIndex]);
            var memberPath = args[pathIndex];

            var obj = FindGameObject(objectName);
            if (obj == null)
                return $"Object not found: '{objectName}'";

            var result = ReflectionResolver.SetValue(obj, memberPath, value);
            return result.Success ? result.Display : result.Error;
        }

        [TerminalCommand("obj.members", "List accessible members of a component", "obj.members <name> <ComponentType>", CompleterMethod = "ComponentPath")]
        public static string ObjMembers(string[] args)
        {
            if (args.Length < 2)
                return "Usage: obj.members <objectName> <ComponentType>\nExample: obj.members Main Camera Transform";

            var componentName = args[^1];
            var objectName = string.Join(" ", args[..^1]);

            var obj = FindGameObject(objectName);
            if (obj == null)
                return $"Object not found: '{objectName}'";

            return ReflectionResolver.ListMembers(obj, componentName);
        }

        // -- Watch expressions --

        [TerminalCommand("watch", "Add a watch expression", "watch <objectName> <Component.property[.sub]>")]
        public static string Watch(string[] args)
        {
            if (args.Length < 2)
            {
                if (args.Length == 0)
                    return WatchManager.FormatAll();
                return "Usage: watch <objectName> <Component.property[.sub]>";
            }

            var pathIndex = FindMemberPathIndex(args);
            if (pathIndex < 0)
                return "No component path found. Use format: watch <name> Component.property";

            var objectName = string.Join(" ", args[..pathIndex]);
            var memberPath = args[pathIndex];

            var id = WatchManager.Add(objectName, memberPath);
            return id >= 0
                ? $"Watch #{id} added: {objectName} {memberPath}"
                : "Failed to add watch.";
        }

        [TerminalCommand("unwatch", "Remove a watch expression", "unwatch <id|all>")]
        public static string Unwatch(string[] args)
        {
            if (args.Length == 0)
                return "Usage: unwatch <id|all>";

            if (args[0].Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                WatchManager.Clear();
                return "All watches removed.";
            }

            if (!int.TryParse(args[0], out var id))
                return $"Invalid watch ID: '{args[0]}'";

            return WatchManager.Remove(id)
                ? $"Watch #{id} removed."
                : $"Watch #{id} not found.";
        }

        // -- Debug toggles --

        [TerminalCommand("debug.toggle", "Toggle a debug visualization flag", "debug.toggle [name] [on|off]", CompleterMethod = "ToggleNames")]
        public static string DebugToggle(string[] args)
        {
            if (args.Length == 0)
                return DebugToggles.ListAll();

            var name = args[0];

            if (args.Length >= 2)
            {
                var toggle = args[1].ToLowerInvariant();
                return toggle switch
                {
                    "on" or "true" or "1" => SetToggle(name, true),
                    "off" or "false" or "0" => SetToggle(name, false),
                    _ => $"Invalid value: '{args[1]}'. Use 'on' or 'off'."
                };
            }

            var newVal = DebugToggles.Toggle(name);
            return $"debug.toggle '{name}': {(newVal ? "ON" : "OFF")}";

            static string SetToggle(string n, bool val)
            {
                DebugToggles.Set(n, val);
                return $"debug.toggle '{n}': {(val ? "ON" : "OFF")}";
            }
        }

        // -- Helpers --

        private static GameObject FindGameObject(string name)
        {
            var obj = GameObject.Find(name);
            if (obj != null) return obj;

            // Try inactive objects
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            return all.FirstOrDefault(o => o.name == name && o.scene.isLoaded);
        }

        /// <summary>
        /// Find the index of the argument that contains a dot (the member path).
        /// Searches from the end, optionally skipping the last N args.
        /// </summary>
        private static int FindMemberPathIndex(string[] args, int skipLast = 0)
        {
            var end = args.Length - skipLast;
            for (var i = end - 1; i >= 0; i--)
            {
                if (args[i].Contains('.'))
                    return i;
            }
            return -1;
        }

        private static string GetPath(Transform t)
        {
            var path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
