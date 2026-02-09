# Contributing to Unity MCP Bridge

Thank you for your interest in contributing! This document provides guidelines and instructions for contributing.

## How to Contribute

### Reporting Bugs

1. Check if the bug has already been reported in [Issues](https://github.com/Nexonium/unity-mcp-bridge/issues)
2. If not, create a new issue with:
   - Clear title and description
   - Steps to reproduce
   - Expected vs actual behavior
   - Unity version, OS, Node.js version

### Suggesting Features

1. Check existing issues and discussions
2. Create a new issue with the "enhancement" label
3. Describe the feature and why it would be useful

### Pull Requests

1. Fork the repository
2. Create a feature branch from `main`
3. Make your changes
4. Test your changes in Unity
5. Submit a pull request

## Development Setup

### Prerequisites

- Unity 6000.0 or later
- Node.js 18+
- npm

### Setting Up

1. Clone the repository
2. Open the `Editor` folder as a Unity package or link it to a Unity project
3. For MCP server development:
   ```bash
   cd mcp-server
   npm install
   npm run build
   ```

### Code Style

**C# (Unity)**
- Follow Unity/Microsoft C# conventions
- Use XML documentation for public APIs
- Keep classes focused (Single Responsibility)

**TypeScript (MCP Server)**
- Use TypeScript strict mode
- Document public functions
- Handle errors gracefully

## Project Structure

```
unity-mcp-bridge/
├── Editor/                 # Unity Editor scripts
│   ├── Core/              # Interfaces, models, bridges (TerminalBridge)
│   ├── Services/          # Log and compilation services
│   ├── Server/            # HTTP server and request handling
│   ├── Settings/          # User preferences
│   └── UI/                # Editor windows
├── Runtime/               # Play Mode runtime components
│   └── Terminal/          # Debug terminal system
│       ├── Commands/      # Built-in + custom commands ([TerminalCommand])
│       ├── DebugTerminal.cs          # Core terminal UI (IMGUI)
│       ├── TerminalCommandRegistry.cs # Reflection-based command discovery
│       ├── TerminalCommandAttribute.cs # [TerminalCommand] attribute
│       ├── TerminalSettings.cs       # Runtime configuration
│       ├── TerminalLog.cs            # Log entry model
│       ├── ReflectionResolver.cs     # Component property get/set via reflection
│       ├── ArgumentCompleters.cs     # Tab-completion providers
│       ├── WatchManager.cs           # Live watch expressions
│       └── DebugToggles.cs           # Named boolean toggle registry
├── mcp-server/            # Node.js MCP server
│   └── src/               # TypeScript source
└── package.json           # Unity package manifest
```

## Adding Custom Terminal Commands

Create debug commands for your game using the `[TerminalCommand]` attribute:

```csharp
using UnityMCPBridge.Terminal;

public static class MyGameCommands
{
    [TerminalCommand("hp", "Set player health", "hp [value]")]
    public static string SetHealth(string[] args)
    {
        if (args.Length == 0)
            return $"Health: {Player.Instance.Health}";

        if (int.TryParse(args[0], out var value))
        {
            Player.Instance.Health = value;
            return $"Health set to {value}";
        }
        return $"Invalid value: '{args[0]}'";
    }
}
```

Commands are auto-discovered via reflection on Play Mode start. See
`Runtime/Terminal/Commands/ExampleGameCommands.cs.example` for more patterns.

### Argument Autocomplete

Add Tab-completion to your commands by specifying a `CompleterMethod`:

```csharp
[TerminalCommand("teleport", "Teleport to a location", "teleport <place>",
    CompleterMethod = "TeleportLocations")]
public static string Teleport(string[] args) { /* ... */ }

// Completer must be: static IReadOnlyList<string> Method(string[] currentArgs)
public static IReadOnlyList<string> TeleportLocations(string[] currentArgs)
{
    var all = new[] { "spawn", "boss", "shop", "dungeon" };
    var prefix = currentArgs.Length > 0 ? currentArgs[0] : "";
    return all.Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
}
```

Built-in completers in `ArgumentCompleters.cs`: `GameObjectNames`, `SceneNames`,
`AliasNames`, `CommandNames`, `ToggleNames`, `ComponentPath`.

### Watch Expressions

The watch system evaluates expressions every frame and displays results in a panel
below the terminal. Use it to monitor runtime values:

```
watch Main Camera Transform.position
watch Player SpriteRenderer.color
unwatch all
```

### Debug Toggles

Register named boolean flags that game code can query:

```csharp
// In your game initialization
DebugToggles.Register("hitboxes", "Show collision hitboxes", false);
DebugToggles.Register("grid", "Show placement grid", false);

// In your rendering/update code
if (DebugToggles.Get("hitboxes"))
    DrawHitboxes();

// Subscribe to changes
DebugToggles.OnToggleChanged += (name, value) => Debug.Log($"{name}: {value}");
```

Toggle from the terminal: `debug.toggle hitboxes` or `debug.toggle grid on`.

## Testing

- Test in Unity Editor before submitting
- Verify MCP tools work correctly
- Check for compilation errors/warnings

## Questions?

Feel free to open an issue for any questions!
