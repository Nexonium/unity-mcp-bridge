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
