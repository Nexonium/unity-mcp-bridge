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
│   ├── Core/              # Interfaces, models, main bridge
│   ├── Services/          # Log and compilation services
│   ├── Server/            # HTTP server implementation
│   ├── Settings/          # User preferences
│   └── UI/                # Editor windows
├── mcp-server/            # Node.js MCP server
│   └── src/               # TypeScript source
└── package.json           # Unity package manifest
```

## Testing

- Test in Unity Editor before submitting
- Verify MCP tools work correctly
- Check for compilation errors/warnings

## Questions?

Feel free to open an issue for any questions!
