<p align="center">
  <h1 align="center">Unity MCP Bridge</h1>
  <p align="center">
    <strong>Connect AI assistants to Unity Editor via Model Context Protocol</strong>
  </p>
  <p align="center">
    <a href="#features">Features</a> &bull;
    <a href="#installation">Installation</a> &bull;
    <a href="#usage">Usage</a> &bull;
    <a href="#available-tools">Tools</a> &bull;
    <a href="#configuration">Configuration</a> &bull;
    <a href="#troubleshooting">Troubleshooting</a>
  </p>
</p>

---

Unity MCP Bridge enables AI assistants like **Claude in Cursor** to interact directly with Unity Editor. Read console logs, check compilation errors, control Play Mode, capture screenshots, navigate assets, and more — all without leaving your code editor.

## Features

- **Console Logs** — Real-time access to Unity console with filtering by type (log/warning/error)
- **Compilation Errors** — Instant notification of C# compilation errors and warnings with file paths and line numbers
- **Play Mode Control** — Start, stop, and pause Play Mode remotely
- **Screenshots** — Capture Game View and Scene View with configurable quality (low/medium/high)
- **Asset Navigation** — Open prefabs, scenes, and scripts; select and frame objects in the hierarchy
- **Hierarchy Inspection** — View the full GameObject hierarchy of scenes and prefabs
- **Asset Refresh** — Trigger asset database refresh after code changes
- **Debug Terminal** — In-game IMGUI debug console with 20+ built-in commands, custom command support, argument autocomplete, watch expressions, and clipboard support
- **Background Operation** — Works even when Unity is not in focus (for most operations)
- **Easy Setup** — Simple installation via Unity Package Manager

## Requirements

- **Unity** 6000.0 (Unity 6) or later
- **Node.js** 18+ (for MCP server)
- **Cursor IDE** with MCP support (or any MCP-compatible client)

## Installation

### Step 1: Install Unity Package

**Option A: Install via Package Manager (Recommended)**

1. In Unity, go to **Window > Package Manager**
2. Click **+** in the top-left corner
3. Select **Add package from git URL...**
4. Enter:
   ```
   https://github.com/Nexonium/unity-mcp-bridge.git
   ```
5. Click **Add**

To install a specific version, append the tag:
```
https://github.com/Nexonium/unity-mcp-bridge.git#v1.2.0-pre.1
```

**Option B: Add to manifest.json**

Add to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.nexonium.unity-mcp-bridge": "https://github.com/Nexonium/unity-mcp-bridge.git#v1.2.0-pre.1"
  }
}
```

**Option C: Download and Install Locally**

1. Download or clone this repository
2. In Unity, go to **Window > Package Manager**
3. Click **+** > **Add package from disk...**
4. Navigate to the downloaded folder and select `package.json`

### Step 2: Build MCP Server

```bash
cd mcp-server
npm install
npm run build
```

### Step 3: Configure Cursor

Add to your Cursor MCP settings file:

**Windows:** `%USERPROFILE%\.cursor\mcp.json`  
**macOS/Linux:** `~/.cursor/mcp.json`

```json
{
  "mcpServers": {
    "unity": {
      "command": "node",
      "args": ["C:/path/to/unity-mcp-bridge/mcp-server/dist/index.js"]
    }
  }
}
```

> **Note:** Use forward slashes `/` in paths, even on Windows.

### Step 4: Start the Server

1. In Unity, go to **Window > Unity MCP Bridge > Server**
2. Click **Start Server**
3. (Optional) Enable **Auto-start on Unity Open** for convenience
4. Restart Cursor to load the MCP server

## Usage

Once configured, the AI assistant in Cursor can use Unity tools automatically. Try asking:

- *"Check if there are any compilation errors"*
- *"Show me the recent Unity console logs"*
- *"Start Play Mode and take a screenshot"*
- *"Open the Player prefab and show me its hierarchy"*
- *"Take a screenshot of the Scene View"*
- *"Execute 'obj.find Player' in the debug terminal"*
- *"Run 'mem' in the terminal to check memory usage"*

### Example Workflows

**Debugging workflow:**

1. You edit a C# file in Cursor
2. Ask: *"Are there any compilation errors?"*
3. AI runs `unity_refresh` then `unity_get_compilation_errors`
4. AI shows you errors with file paths and line numbers
5. You fix the error, AI verifies compilation succeeds

**Visual inspection workflow:**

1. Ask: *"Open the MessengerNotification prefab"*
2. AI runs `unity_open_asset` to enter Prefab Mode
3. AI runs `unity_get_hierarchy` to see the structure
4. AI runs `unity_frame_selected` and `unity_screenshot` to capture the view
5. AI describes what it sees and suggests improvements

## Available Tools

### Editor Status

| Tool | Description |
|------|-------------|
| `unity_status` | Get Unity Editor status (version, project, play mode state) |
| `unity_compilation_status` | Check if Unity is compiling and error/warning counts |

### Console Logs

| Tool | Description |
|------|-------------|
| `unity_get_logs` | Get console logs with optional type filter and limit |
| `unity_clear_logs` | Clear all stored console logs |
| `unity_get_compilation_errors` | Get compilation errors with file paths and line numbers |
| `unity_get_compilation_warnings` | Get compilation warnings |

### Play Mode

| Tool | Description |
|------|-------------|
| `unity_play` | Enter Play Mode |
| `unity_stop` | Exit Play Mode |
| `unity_pause` | Toggle pause state |
| `unity_refresh` | Refresh Asset Database (triggers recompilation) |

### Screenshots

| Tool | Description |
|------|-------------|
| `unity_screenshot` | Capture Game View or Scene View with configurable quality |

**Parameters:**
- `view` — `"game"` (default) or `"scene"`
- `quality` — `"low"` (640x480, ~500 tokens), `"medium"` (1280x720, ~1200 tokens), `"high"` (native, ~2700+ tokens)

### Asset Navigation

| Tool | Description |
|------|-------------|
| `unity_open_asset` | Open prefab, scene, or script by asset path |
| `unity_select_object` | Select a GameObject by hierarchy path or name |
| `unity_frame_selected` | Frame selected object in Scene View (like pressing F) |
| `unity_get_hierarchy` | Get the full hierarchy of objects in the current scene or prefab |

### Debug Terminal

| Tool | Description |
|------|-------------|
| `unity_terminal_execute` | Execute a command in the runtime debug terminal (requires Play Mode) |
| `unity_terminal_status` | Get terminal status (active, visible, command count) |
| `unity_terminal_get_logs` | Get terminal log entries with optional type/count filters |
| `unity_terminal_get_history` | Get command history |
| `unity_terminal_execute_batch` | Execute multiple commands sequentially |

## Debug Terminal

The Debug Terminal is an in-game IMGUI console for runtime debugging. Press **~** (backtick) to toggle.

### Built-in Commands

| Command | Description |
|---------|-------------|
| `help [cmd]` | List commands or show help for a specific command |
| `clear` | Clear terminal output |
| `scene [name]` | Show/load scenes |
| `fps` | Show current FPS |
| `mem` | Detailed memory usage (managed heap, allocations, GC stats) |
| `sysinfo` | System info (CPU, GPU, RAM, resolution, quality) |
| `gc` | Force garbage collection |
| `time.scale [val]` | Get/set time scale |
| `obj.find <name>` | Find a GameObject |
| `obj.inspect <name>` | Inspect components |
| `obj.toggle <name>` | Toggle active state |
| `obj.get <name> <path>` | Get component property (e.g., `obj.get Main Camera Transform.position.x`) |
| `obj.set <name> <path> <val>` | Set component property |
| `obj.members <name> <type>` | List component members |
| `watch <name> <path>` | Add a live watch expression |
| `unwatch <id\|all>` | Remove watch expressions |
| `debug.toggle [name]` | Toggle debug visualization flags |
| `alias [name] [cmd]` | Create/list command aliases |
| `logs.capture [on\|off]` | Toggle Unity log capture |

### Terminal Features

- **Tab Completion** for commands and arguments (GameObject names, scenes, components, aliases)
- **Command History** with Up/Down arrows (persisted across sessions)
- **Clipboard** support (Ctrl+C / Ctrl+V)
- **Ctrl+Backspace** to delete word backward
- **PageUp/PageDown** scrolling
- **Watch Panel** displays live-updating values below the terminal
- **Command Aliases** with persistence (e.g., `alias p obj.find Player`)

### Custom Commands

Add game-specific commands using the `[TerminalCommand]` attribute:

```csharp
using UnityMCPBridge.Terminal;

public static class MyCommands
{
    [TerminalCommand("hp", "Set player health", "hp [value]",
        CompleterMethod = "GameObjectNames")]
    public static string SetHealth(string[] args)
    {
        // Your game logic here
        return "Done";
    }
}
```

Commands are auto-discovered via reflection. See `CONTRIBUTING.md` for full details.

## Configuration

### Unity Settings (Window > Unity MCP Bridge > Server)

| Setting | Default | Description |
|---------|---------|-------------|
| **Port** | 7890 | HTTP server port |
| **Max Log Entries** | 500 | Maximum log entries kept in memory |
| **Auto-start** | false | Start server automatically when Unity opens |
| **Include Stack Traces** | true | Include stack traces in log entries |
| **Include Warnings** | true | Track compilation warnings (not just errors) |
| **Screenshot Quality** | Low | Default screenshot quality (Low / Medium / High) |
| **Screenshot Cleanup** | 30 min | Auto-delete screenshots older than this (0 = disabled) |

### Environment Variables (MCP Server)

| Variable | Default | Description |
|----------|---------|-------------|
| `UNITY_HOST` | 127.0.0.1 | Unity HTTP server host |
| `UNITY_PORT` | 7890 | Unity HTTP server port |
| `UNITY_TIMEOUT` | 5000 | Request timeout in milliseconds |

## Architecture

```
+-------------------+                          +-------------------+
|     Cursor        |<--- MCP Protocol ------->|   MCP Server      |
|   (AI Agent)      |       (stdio)            |   (Node.js)       |
+-------------------+                          +---------+---------+
                                                         |
                                                    HTTP REST
                                                         |
                                               +---------+---------+
                                               |  Unity Editor     |
                                               |  (HTTP Server)    |
                                               |  Port 7890        |
                                               +-------------------+
```

**Request flow:**

1. AI sends tool call via MCP protocol (stdio)
2. MCP Server (TypeScript) translates to HTTP request
3. Unity HTTP Server receives on background thread
4. Read-only requests handled directly; actions queued for main thread
5. Response flows back through the same path

## Troubleshooting

### "Cannot connect to Unity"

- Make sure Unity Editor is open
- Check that MCP Bridge server is running (Window > Unity MCP Bridge > Server)
- Verify the port matches in both Unity and MCP server config

### "Request timeout"

- Unity might be busy (compiling, loading assets)
- Try clicking in Unity window to "wake it up"
- Increase `UNITY_TIMEOUT` environment variable for slow operations

### Port already in use

- Change the port in Unity MCP Bridge settings
- Update `UNITY_PORT` in your MCP server config or environment

### Server stops after code changes

This is expected — Unity recompiles and reloads the domain. If **Auto-start** is enabled, the server restarts automatically.

### Screenshots are empty or wrong

- Make sure a Camera exists in the scene (Game View requires a camera)
- For Scene View screenshots, ensure the Scene View window is open
- Try using `unity_frame_selected` to position the camera before capturing

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Support

If you find this project useful, consider supporting its development:

[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support-ff5e5b?logo=ko-fi)](https://ko-fi.com/nexonium)

---

<p align="center">
  Made with Claude AI assistance
</p>
