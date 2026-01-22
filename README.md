<p align="center">
  <h1 align="center">Unity MCP Bridge</h1>
  <p align="center">
    <strong>Connect AI assistants to Unity Editor via Model Context Protocol</strong>
  </p>
  <p align="center">
    <a href="#features">Features</a> •
    <a href="#installation">Installation</a> •
    <a href="#usage">Usage</a> •
    <a href="#available-tools">Tools</a> •
    <a href="#configuration">Configuration</a> •
    <a href="#troubleshooting">Troubleshooting</a>
  </p>
</p>

---

Unity MCP Bridge enables AI assistants like **Claude in Cursor** to interact directly with Unity Editor. Read console logs, check compilation errors, control Play Mode, and more — all without leaving your code editor.

## Features

- **Console Logs** — Real-time access to Unity console with filtering by type (log/warning/error)
- **Compilation Errors** — Instant notification of C# compilation errors and warnings with file paths and line numbers
- **Play Mode Control** — Start, stop, and pause Play Mode remotely
- **Asset Refresh** — Trigger asset database refresh after code changes
- **Background Operation** — Works even when Unity is not in focus (for most operations)
- **Easy Setup** — Simple installation via Unity Package Manager

> **Coming Soon:** Test execution, GameObject inspection, scene management

## Requirements

- **Unity** 6000.0 (Unity 6) or later
- **Node.js** 18+ (for MCP server)
- **Cursor IDE** with MCP support (or any MCP-compatible client)

## Installation

### Step 1: Install Unity Package

**Option A: Download and Install Locally (Recommended)**

1. Download or clone this repository
2. In Unity, go to **Window > Package Manager**
3. Click **+** > **Add package from disk...**
4. Navigate to the downloaded folder and select `package.json`

**Option B: Add via Git URL**

Add to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.pixelcoven.unity-mcp-bridge": "https://github.com/Nexonium/unity-mcp-bridge.git"
  }
}
```

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

1. In Unity, go to **Window > Unity MCP Bridge**
2. Click **Start Server**
3. (Optional) Enable **Auto-start on Unity Open** for convenience
4. Restart Cursor to load the MCP server

## Usage

Once configured, the AI assistant in Cursor can use Unity tools automatically. Try asking:

- *"Check if there are any compilation errors"*
- *"Show me the recent Unity console logs"*
- *"Start Play Mode"*
- *"Refresh the asset database"*

### Example Workflow

1. You edit a C# file in Cursor
2. Ask: *"Are there any compilation errors?"*
3. AI runs `unity_refresh` → `unity_compilation_status` → `unity_get_compilation_errors`
4. AI shows you errors with file paths and line numbers
5. You fix the error, AI verifies compilation succeeds
6. Ask: *"Run the game"* — AI starts Play Mode

## Available Tools

| Tool | Description |
|------|-------------|
| `unity_status` | Get Unity Editor status (version, project, play mode state) |
| `unity_get_logs` | Get console logs with optional type filter and limit |
| `unity_clear_logs` | Clear all stored console logs |
| `unity_get_compilation_errors` | Get compilation errors with file paths and line numbers |
| `unity_get_compilation_warnings` | Get compilation warnings |
| `unity_compilation_status` | Check if Unity is compiling and error/warning counts |
| `unity_play` | Enter Play Mode |
| `unity_stop` | Exit Play Mode |
| `unity_pause` | Toggle pause state |
| `unity_refresh` | Refresh Asset Database (triggers recompilation) |

## Configuration

### Unity Settings (Window > Unity MCP Bridge)

| Setting | Default | Description |
|---------|---------|-------------|
| **Port** | 7890 | HTTP server port |
| **Max Log Entries** | 500 | Maximum log entries kept in memory |
| **Auto-start** | false | Start server automatically when Unity opens |
| **Include Stack Traces** | true | Include stack traces in log entries |
| **Include Warnings** | true | Track compilation warnings (not just errors) |

### Environment Variables (MCP Server)

| Variable | Default | Description |
|----------|---------|-------------|
| `UNITY_HOST` | 127.0.0.1 | Unity HTTP server host |
| `UNITY_PORT` | 7890 | Unity HTTP server port |
| `UNITY_TIMEOUT` | 5000 | Request timeout in milliseconds |

## Architecture

```
┌─────────────────┐                          ┌─────────────────┐
│     Cursor      │◄── MCP Protocol ────────►│   MCP Server    │
│   (AI Agent)    │       (stdio)            │   (Node.js)     │
└─────────────────┘                          └────────┬────────┘
                                                      │
                                                 HTTP REST
                                                      │
                                             ┌────────▼────────┐
                                             │  Unity Editor   │
                                             │  (HTTP Server)  │
                                             │  Port 7890      │
                                             └─────────────────┘
```

## Troubleshooting

### "Cannot connect to Unity"

- Make sure Unity Editor is open
- Check that MCP Bridge server is running (Window > Unity MCP Bridge)
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

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

If you find this project useful, consider supporting its development:

[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support-ff5e5b?logo=ko-fi)](https://ko-fi.com/nexonium)

---

<p align="center">
  Made with Claude AI assistance
</p>
