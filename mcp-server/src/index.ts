#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from '@modelcontextprotocol/sdk/types.js';
import { UnityClient } from './unity-client.js';

/**
 * MCP Server for Unity Editor integration.
 * Provides tools for interacting with Unity through the MCP Bridge.
 */
class UnityMCPServer {
  private readonly server: Server;
  private readonly unityClient: UnityClient;

  constructor() {
    // Initialize Unity client with default or environment config
    this.unityClient = new UnityClient({
      unityHost: process.env.UNITY_HOST ?? '127.0.0.1',
      unityPort: parseInt(process.env.UNITY_PORT ?? '7890', 10),
      timeout: parseInt(process.env.UNITY_TIMEOUT ?? '5000', 10),
    });

    // Initialize MCP server
    this.server = new Server(
      {
        name: 'unity-mcp-server',
        version: '1.0.0',
      },
      {
        capabilities: {
          tools: {},
        },
      }
    );

    this.setupToolHandlers();
    this.setupErrorHandling();
  }

  /**
   * Sets up the tool handlers for MCP.
   */
  private setupToolHandlers(): void {
    // List available tools
    this.server.setRequestHandler(ListToolsRequestSchema, async () => ({
      tools: [
        {
          name: 'unity_status',
          description: 'Get the current status of Unity Editor (version, project name, play mode state, compilation status)',
          inputSchema: {
            type: 'object',
            properties: {},
          },
        },
        {
          name: 'unity_get_logs',
          description: 'Get console logs from Unity Editor. Returns recent log entries with messages, types (log/warning/error), and timestamps.',
          inputSchema: {
            type: 'object',
            properties: {
              type: {
                type: 'string',
                description: 'Filter logs by type: "all", "log", "warning", "error", "exception", "assert"',
                enum: ['all', 'log', 'warning', 'error', 'exception', 'assert'],
              },
              limit: {
                type: 'number',
                description: 'Maximum number of logs to return (default: all)',
              },
            },
          },
        },
        {
          name: 'unity_clear_logs',
          description: 'Clear all stored console logs in Unity.',
          inputSchema: {
            type: 'object',
            properties: {},
          },
        },
        {
          name: 'unity_get_compilation_errors',
          description: 'Get compilation errors from Unity. Returns errors with file paths, line numbers, and messages.',
          inputSchema: {
            type: 'object',
            properties: {},
          },
        },
        {
          name: 'unity_get_compilation_warnings',
          description: 'Get compilation warnings from Unity.',
          inputSchema: {
            type: 'object',
            properties: {},
          },
        },
        {
          name: 'unity_compilation_status',
          description: 'Check if Unity is currently compiling and whether there are errors or warnings.',
          inputSchema: {
            type: 'object',
            properties: {},
          },
        },
        {
          name: 'unity_play',
          description: 'Enter Play Mode in Unity Editor.',
          inputSchema: {
            type: 'object',
            properties: {},
          },
        },
        {
          name: 'unity_stop',
          description: 'Exit Play Mode in Unity Editor.',
          inputSchema: {
            type: 'object',
            properties: {},
          },
        },
        {
          name: 'unity_pause',
          description: 'Toggle pause state in Unity Editor (pause/resume play mode).',
          inputSchema: {
            type: 'object',
            properties: {},
          },
        },
        {
          name: 'unity_refresh',
          description: 'Refresh the Asset Database in Unity. Use this after modifying files to trigger recompilation.',
          inputSchema: {
            type: 'object',
            properties: {},
          },
        },
        {
          name: 'unity_screenshot',
          description: 'Take a screenshot of the Unity Editor view. Returns the file path to the screenshot. Use the Read tool to view the image after capturing. Default quality is "low" (~400-600 tokens). Use "medium" (~1200 tokens) or "high" (2700+ tokens) only when more detail is needed.',
          inputSchema: {
            type: 'object',
            properties: {
              view: {
                type: 'string',
                description: 'Which view to capture: "game" (Game View) or "scene" (Scene View)',
                enum: ['game', 'scene'],
              },
              quality: {
                type: 'string',
                description: 'Screenshot quality: "low" (640x480, ~500 tokens), "medium" (1280x720, ~1200 tokens), "high" (native resolution, ~2700+ tokens)',
                enum: ['low', 'medium', 'high'],
              },
            },
          },
        },
        {
          name: 'unity_open_asset',
          description: 'Open an asset in Unity Editor. Prefabs open in Prefab Mode, scenes load as the active scene, scripts open in the code editor.',
          inputSchema: {
            type: 'object',
            properties: {
              path: {
                type: 'string',
                description: 'Asset path relative to project root (e.g., "Assets/Prefabs/Player.prefab")',
              },
            },
            required: ['path'],
          },
        },
        {
          name: 'unity_select_object',
          description: 'Select a GameObject in the current scene hierarchy. Use with unity_frame_selected to focus the camera on it.',
          inputSchema: {
            type: 'object',
            properties: {
              path: {
                type: 'string',
                description: 'Full hierarchy path to the object (e.g., "Canvas/Panel/Button"). Use "/" to separate parent/child.',
              },
              name: {
                type: 'string',
                description: 'Object name to search for (finds first match). Use if you don\'t know the full path.',
              },
            },
          },
        },
        {
          name: 'unity_frame_selected',
          description: 'Frame the currently selected object in Scene View (equivalent to pressing F in Unity). Centers the camera on the selected object.',
          inputSchema: {
            type: 'object',
            properties: {},
          },
        },
        {
          name: 'unity_get_hierarchy',
          description: 'Get the hierarchy of GameObjects in the current scene or prefab. Useful to discover object names and paths before using unity_select_object.',
          inputSchema: {
            type: 'object',
            properties: {
              maxDepth: {
                type: 'number',
                description: 'Maximum depth to traverse (default: 10). Use lower values for large hierarchies.',
              },
            },
          },
        },
      ],
    }));

    // Handle tool calls
    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;

      try {
        switch (name) {
          case 'unity_status':
            return await this.handleStatus();

          case 'unity_get_logs':
            return await this.handleGetLogs(args as { type?: string; limit?: number });

          case 'unity_clear_logs':
            return await this.handleClearLogs();

          case 'unity_get_compilation_errors':
            return await this.handleGetCompilationErrors();

          case 'unity_get_compilation_warnings':
            return await this.handleGetCompilationWarnings();

          case 'unity_compilation_status':
            return await this.handleCompilationStatus();

          case 'unity_play':
            return await this.handlePlay();

          case 'unity_stop':
            return await this.handleStop();

          case 'unity_pause':
            return await this.handlePause();

          case 'unity_refresh':
            return await this.handleRefresh();

          case 'unity_screenshot':
            return await this.handleScreenshot(args as { view?: string; quality?: string });

          case 'unity_open_asset':
            return await this.handleOpenAsset(args as { path: string });

          case 'unity_select_object':
            return await this.handleSelectObject(args as { path?: string; name?: string });

          case 'unity_frame_selected':
            return await this.handleFrameSelected();

          case 'unity_get_hierarchy':
            return await this.handleGetHierarchy(args as { maxDepth?: number });

          default:
            return {
              content: [{ type: 'text', text: `Unknown tool: ${name}` }],
              isError: true,
            };
        }
      } catch (error) {
        const message = error instanceof Error ? error.message : String(error);
        return {
          content: [{ type: 'text', text: `Error: ${message}` }],
          isError: true,
        };
      }
    });
  }

  private async handleStatus() {
    const status = await this.unityClient.getStatus();
    const text = [
      `Unity Editor Status:`,
      `- Version: ${status.unityVersion}`,
      `- Project: ${status.projectName}`,
      `- Play Mode: ${status.isPlaying ? 'Playing' : 'Stopped'}${status.isPaused ? ' (Paused)' : ''}`,
      `- Compiling: ${status.isCompiling ? 'Yes' : 'No'}`,
      `- Compilation Errors: ${status.hasCompilationErrors ? 'Yes' : 'No'}`,
      `- Log Count: ${status.logCount}`,
    ].join('\n');

    return { content: [{ type: 'text', text }] };
  }

  private async handleGetLogs(args: { type?: string; limit?: number }) {
    const response = await this.unityClient.getLogs();
    let logs = response.logs;

    // Filter by type if specified
    if (args.type && args.type !== 'all') {
      logs = logs.filter((log) => log.type === args.type);
    }

    // Limit results
    if (args.limit && args.limit > 0) {
      logs = logs.slice(-args.limit);
    }

    if (logs.length === 0) {
      return { content: [{ type: 'text', text: 'No logs found.' }] };
    }

    const text = logs
      .map((log) => {
        const prefix = log.type === 'error' || log.type === 'exception' 
          ? '[ERROR]' 
          : log.type === 'assert'
            ? '[ASSERT]'
            : log.type === 'warning' 
              ? '[WARN]' 
              : '[LOG]';
        const time = new Date(log.timestamp).toLocaleTimeString();
        let entry = `${prefix} ${time}: ${log.message}`;
        if (log.stackTrace && (log.type === 'error' || log.type === 'exception' || log.type === 'assert')) {
          entry += `\n  Stack: ${log.stackTrace.split('\n')[0]}`;
        }
        return entry;
      })
      .join('\n');

    return { content: [{ type: 'text', text: `Logs (${logs.length} entries):\n${text}` }] };
  }

  private async handleClearLogs() {
    const response = await this.unityClient.clearLogs();
    return { content: [{ type: 'text', text: response.message }] };
  }

  private async handleGetCompilationErrors() {
    const response = await this.unityClient.getCompilationErrors();

    if (response.count === 0) {
      return { content: [{ type: 'text', text: 'No compilation errors.' }] };
    }

    const text = response.errors
      .map((err) => `${err.file}(${err.line},${err.column}): error: ${err.message}`)
      .join('\n');

    return { content: [{ type: 'text', text: `Compilation Errors (${response.count}):\n${text}` }] };
  }

  private async handleGetCompilationWarnings() {
    const response = await this.unityClient.getCompilationWarnings();

    if (response.count === 0) {
      return { content: [{ type: 'text', text: 'No compilation warnings.' }] };
    }

    const text = response.warnings
      .map((warn) => `${warn.file}(${warn.line},${warn.column}): warning: ${warn.message}`)
      .join('\n');

    return { content: [{ type: 'text', text: `Compilation Warnings (${response.count}):\n${text}` }] };
  }

  private async handleCompilationStatus() {
    const status = await this.unityClient.getCompilationStatus();
    const text = [
      `Compilation Status:`,
      `- Currently Compiling: ${status.isCompiling ? 'Yes' : 'No'}`,
      `- Has Errors: ${status.hasErrors ? 'Yes' : 'No'} (${status.errorCount})`,
      `- Has Warnings: ${status.hasWarnings ? 'Yes' : 'No'} (${status.warningCount})`,
    ].join('\n');

    return { content: [{ type: 'text', text }] };
  }

  private async handlePlay() {
    const response = await this.unityClient.play();
    return { content: [{ type: 'text', text: response.message }] };
  }

  private async handleStop() {
    const response = await this.unityClient.stop();
    return { content: [{ type: 'text', text: response.message }] };
  }

  private async handlePause() {
    const response = await this.unityClient.pause();
    return { content: [{ type: 'text', text: response.message }] };
  }

  private async handleRefresh() {
    const response = await this.unityClient.refresh();
    return { content: [{ type: 'text', text: response.message }] };
  }

  private async handleScreenshot(args: { view?: string; quality?: string }) {
    const response = await this.unityClient.takeScreenshot({
      view: (args.view as 'game' | 'scene') ?? 'game',
      quality: (args.quality as 'low' | 'medium' | 'high') ?? 'low',
    });

    if (!response.success) {
      return {
        content: [{ type: 'text', text: `Screenshot failed: ${response.error}` }],
        isError: true,
      };
    }

    const text = [
      `Screenshot captured successfully!`,
      `- File: ${response.filePath}`,
      `- Size: ${response.width}x${response.height}`,
      `- View: ${response.view}`,
      `- Quality: ${response.quality}`,
      `- Estimated tokens: ~${response.estimatedTokens}`,
      ``,
      `Use the Read tool with the file path above to view the image.`,
    ].join('\n');

    return { content: [{ type: 'text', text }] };
  }

  private async handleOpenAsset(args: { path: string }) {
    if (!args.path) {
      return {
        content: [{ type: 'text', text: 'Error: path parameter is required' }],
        isError: true,
      };
    }

    const response = await this.unityClient.openAsset({ path: args.path });

    if (!response.success) {
      return {
        content: [{ type: 'text', text: `Failed to open asset: ${response.error}` }],
        isError: true,
      };
    }

    return {
      content: [{ type: 'text', text: `${response.message} (${response.assetType})` }],
    };
  }

  private async handleSelectObject(args: { path?: string; name?: string }) {
    if (!args.path && !args.name) {
      return {
        content: [{ type: 'text', text: 'Error: provide either path or name parameter' }],
        isError: true,
      };
    }

    const response = await this.unityClient.selectObject(args);

    if (!response.success) {
      return {
        content: [{ type: 'text', text: `Failed to select object: ${response.error}` }],
        isError: true,
      };
    }

    return {
      content: [{ type: 'text', text: response.message ?? `Selected: ${response.objectName}` }],
    };
  }

  private async handleFrameSelected() {
    const response = await this.unityClient.frameSelected();

    if (!response.success) {
      return {
        content: [{ type: 'text', text: `Failed to frame selected: ${response.error}` }],
        isError: true,
      };
    }

    return {
      content: [{ type: 'text', text: response.message ?? `Framed: ${response.objectName}` }],
    };
  }

  private async handleGetHierarchy(args: { maxDepth?: number }) {
    const response = await this.unityClient.getHierarchy({ maxDepth: args.maxDepth });

    if (!response.success) {
      return {
        content: [{ type: 'text', text: `Failed to get hierarchy: ${response.error}` }],
        isError: true,
      };
    }

    const modeInfo = response.inPrefabMode 
      ? `Prefab Mode: ${response.prefabName}` 
      : 'Scene Mode';

    // Format hierarchy as tree
    const lines: string[] = [
      `${modeInfo} (${response.objectCount} objects)`,
      '',
    ];

    for (const item of response.hierarchy) {
      const indent = '  '.repeat(item.depth);
      const activeMarker = item.active ? '' : ' [inactive]';
      const childInfo = item.childCount > 0 ? ` (${item.childCount} children)` : '';
      lines.push(`${indent}- ${item.name}${childInfo}${activeMarker}`);
    }

    return {
      content: [{ type: 'text', text: lines.join('\n') }],
    };
  }

  private setupErrorHandling(): void {
    this.server.onerror = (error) => {
      console.error('[MCP Error]', error);
    };

    process.on('SIGINT', async () => {
      await this.server.close();
      process.exit(0);
    });
  }

  /**
   * Starts the MCP server.
   */
  async run(): Promise<void> {
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    console.error('Unity MCP Server running on stdio');
  }
}

// Run the server
const server = new UnityMCPServer();
server.run().catch(console.error);
