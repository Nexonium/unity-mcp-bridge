# Changelog

All notable changes to Unity MCP Bridge will be documented in this file.

## [1.2.0] - 2025-02-10

### Added

**Debug Terminal - Phase 1: Foundation**
- Unity log capture via `Application.logMessageReceived` with toggle (`logs.capture`)
- Persistent command history across sessions (saved to `Application.persistentDataPath`)
- MCP endpoints: `unity_terminal_get_logs`, `unity_terminal_get_history`

**Debug Terminal - Phase 2: Usability**
- PageUp/PageDown scrolling in terminal output
- Command aliases with persistence (`alias`, `unalias`)
- MCP endpoint: `unity_terminal_execute_batch` for sequential command execution
- CONTRIBUTING.md with custom command guide and example file

**Debug Terminal - Phase 3: Power Features**
- Component property get/set via reflection (`obj.get`, `obj.set`, `obj.members`)
  - Supports nested value types (e.g., `Transform.position.x`)
  - Type parsing for float, int, bool, string, Vector2, Vector3, Color, enums
- Argument autocomplete system with `CompleterMethod` attribute property
  - Built-in completers: GameObjectNames, SceneNames, AliasNames, CommandNames, ToggleNames, ComponentPath
  - Tab-completion for `obj.find`, `obj.inspect`, `obj.toggle`, `scene`, `help`, `alias`, `unalias`, `debug.toggle`
- Watch expressions panel with live-updating values (`watch`, `unwatch`)
- Debug toggle registry for named boolean flags (`debug.toggle`, `DebugToggles` API)

**Debug Terminal - Phase 4: Polish**
- `mem` command — detailed memory diagnostics (managed heap, profiler allocations, GC stats, texture memory)
- `sysinfo` command — system information (CPU, GPU, RAM, resolution, quality level)
- Clipboard support (Ctrl+C to copy, Ctrl+V to paste with cursor-aware insertion)
- Ctrl+Backspace word deletion with dot-aware word boundaries
- URP Rendering Debugger suppression while terminal is open (via reflection, no URP dependency)

### Changed
- Terminal command name completion now appends a space after single match

## [1.1.0] - 2025-01-xx

### Added
- Initial release with editor tools
- Console log access, compilation errors/warnings
- Play Mode control (play, stop, pause)
- Screenshots (Game View, Scene View with quality levels)
- Asset navigation (open, select, frame, hierarchy)
- Debug terminal with built-in commands (help, clear, echo, scene, time.scale, fps, gc, log, obj.list, obj.find, obj.inspect, obj.toggle)
- MCP server with full tool set
