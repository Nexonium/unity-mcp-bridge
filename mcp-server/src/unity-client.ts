import {
  ServerConfig,
  UnityStatus,
  LogsResponse,
  CompilationErrorsResponse,
  CompilationWarningsResponse,
  CompilationStatus,
  ActionResponse,
  ScreenshotRequest,
  ScreenshotResponse,
  OpenAssetRequest,
  OpenAssetResponse,
  SelectObjectRequest,
  SelectObjectResponse,
  FrameSelectedResponse,
  GetHierarchyRequest,
  GetHierarchyResponse,
  TerminalExecuteRequest,
  TerminalExecuteResponse,
  TerminalStatusResponse,
  TerminalBatchExecuteRequest,
  TerminalBatchExecuteResponse,
  TerminalLogsRequest,
  TerminalLogsResponse,
  TerminalHistoryRequest,
  TerminalHistoryResponse,
  TerminalCommandsRequest,
  TerminalCommandsResponse,
} from './types.js';

/**
 * HTTP client for communicating with Unity Editor.
 * Single Responsibility: Handle all HTTP communication with Unity.
 */
export class UnityClient {
  private readonly baseUrl: string;
  private readonly timeout: number;

  constructor(config: Partial<ServerConfig> = {}) {
    const host = config.unityHost ?? '127.0.0.1';
    const port = config.unityPort ?? 7890;
    this.baseUrl = `http://${host}:${port}`;
    this.timeout = config.timeout ?? 5000;
  }

  /**
   * Makes an HTTP request to Unity.
   */
  private async request<T>(path: string, method: 'GET' | 'POST' = 'GET', body?: unknown): Promise<T> {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), this.timeout);

    try {
      const response = await fetch(`${this.baseUrl}${path}`, {
        method,
        headers: {
          'Content-Type': 'application/json',
        },
        body: body ? JSON.stringify(body) : undefined,
        signal: controller.signal,
      });

      if (!response.ok) {
        const error = await response.text();
        throw new Error(`Unity request failed: ${response.status} - ${error}`);
      }

      return await response.json() as T;
    } catch (error) {
      if (error instanceof Error && error.name === 'AbortError') {
        throw new Error('Unity request timed out. Is the Unity Editor running with MCP Bridge?');
      }
      if (error instanceof TypeError && error.message.includes('fetch')) {
        throw new Error('Cannot connect to Unity. Make sure the MCP Bridge server is running in Unity Editor.');
      }
      throw error;
    } finally {
      clearTimeout(timeoutId);
    }
  }

  /**
   * Gets the current status of Unity Editor.
   */
  async getStatus(): Promise<UnityStatus> {
    return this.request<UnityStatus>('/status');
  }

  /**
   * Gets all console logs from Unity.
   */
  async getLogs(): Promise<LogsResponse> {
    return this.request<LogsResponse>('/logs');
  }

  /**
   * Clears all stored logs.
   */
  async clearLogs(): Promise<ActionResponse> {
    return this.request<ActionResponse>('/logs/clear', 'POST');
  }

  /**
   * Gets compilation errors.
   */
  async getCompilationErrors(): Promise<CompilationErrorsResponse> {
    return this.request<CompilationErrorsResponse>('/compilation/errors');
  }

  /**
   * Gets compilation warnings.
   */
  async getCompilationWarnings(): Promise<CompilationWarningsResponse> {
    return this.request<CompilationWarningsResponse>('/compilation/warnings');
  }

  /**
   * Gets compilation status.
   */
  async getCompilationStatus(): Promise<CompilationStatus> {
    return this.request<CompilationStatus>('/compilation/status');
  }

  /**
   * Enters Play Mode in Unity.
   */
  async play(): Promise<ActionResponse> {
    return this.request<ActionResponse>('/editor/play', 'POST');
  }

  /**
   * Exits Play Mode in Unity.
   */
  async stop(): Promise<ActionResponse> {
    return this.request<ActionResponse>('/editor/stop', 'POST');
  }

  /**
   * Toggles pause state in Unity.
   */
  async pause(): Promise<ActionResponse> {
    return this.request<ActionResponse>('/editor/pause', 'POST');
  }

  /**
   * Refreshes the Asset Database.
   */
  async refresh(): Promise<ActionResponse> {
    return this.request<ActionResponse>('/editor/refresh', 'POST');
  }

  /**
   * Takes a screenshot of the specified Unity view.
   * @param options Screenshot options (view and quality)
   */
  async takeScreenshot(options: ScreenshotRequest = {}): Promise<ScreenshotResponse> {
    return this.request<ScreenshotResponse>('/editor/screenshot', 'POST', {
      view: options.view ?? 'game',
      quality: options.quality ?? 'low',
    });
  }

  /**
   * Opens an asset in Unity Editor (prefab, scene, script, etc.).
   * @param options Asset path to open
   */
  async openAsset(options: OpenAssetRequest): Promise<OpenAssetResponse> {
    return this.request<OpenAssetResponse>('/editor/open-asset', 'POST', {
      path: options.path,
    });
  }

  /**
   * Selects a GameObject in the scene hierarchy.
   * @param options Object path or name to select
   */
  async selectObject(options: SelectObjectRequest): Promise<SelectObjectResponse> {
    return this.request<SelectObjectResponse>('/editor/select-object', 'POST', options);
  }

  /**
   * Frames the currently selected object in Scene View (like pressing F).
   */
  async frameSelected(): Promise<FrameSelectedResponse> {
    return this.request<FrameSelectedResponse>('/editor/frame-selected', 'POST');
  }

  /**
   * Gets the hierarchy of GameObjects in the current scene or prefab.
   * @param options Options for hierarchy retrieval
   */
  async getHierarchy(options: GetHierarchyRequest = {}): Promise<GetHierarchyResponse> {
    return this.request<GetHierarchyResponse>('/editor/hierarchy', 'GET', options.maxDepth ? { maxDepth: options.maxDepth } : undefined);
  }

  /**
   * Executes a command in the runtime debug terminal.
   * @param options Command to execute
   */
  async executeTerminalCommand(options: TerminalExecuteRequest): Promise<TerminalExecuteResponse> {
    return this.request<TerminalExecuteResponse>('/terminal/execute', 'POST', {
      command: options.command,
    });
  }

  /**
   * Gets the status of the runtime debug terminal.
   */
  async getTerminalStatus(): Promise<TerminalStatusResponse> {
    return this.request<TerminalStatusResponse>('/terminal/status');
  }

  /**
   * Executes multiple commands sequentially in the runtime debug terminal.
   */
  async executeTerminalBatch(options: TerminalBatchExecuteRequest): Promise<TerminalBatchExecuteResponse> {
    return this.request<TerminalBatchExecuteResponse>('/terminal/execute-batch', 'POST', {
      commands: options.commands,
    });
  }

  /**
   * Gets the runtime terminal log buffer.
   */
  async getTerminalLogs(options: TerminalLogsRequest = {}): Promise<TerminalLogsResponse> {
    return this.request<TerminalLogsResponse>('/terminal/logs', 'GET',
      (options.limit || options.type) ? options : undefined);
  }

  /**
   * Gets the runtime terminal command history.
   */
  async getTerminalHistory(options: TerminalHistoryRequest = {}): Promise<TerminalHistoryResponse> {
    return this.request<TerminalHistoryResponse>('/terminal/history', 'GET',
      options.limit ? { limit: options.limit } : undefined);
  }

  /**
   * Gets all available terminal commands with metadata.
   */
  async getTerminalCommands(options: TerminalCommandsRequest = {}): Promise<TerminalCommandsResponse> {
    return this.request<TerminalCommandsResponse>('/terminal/commands', 'GET',
      options.category ? { category: options.category } : undefined);
  }

  /**
   * Checks if Unity is reachable.
   */
  async isConnected(): Promise<boolean> {
    try {
      await this.getStatus();
      return true;
    } catch {
      return false;
    }
  }
}
