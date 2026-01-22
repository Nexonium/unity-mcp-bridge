import {
  ServerConfig,
  UnityStatus,
  LogsResponse,
  CompilationErrorsResponse,
  CompilationWarningsResponse,
  CompilationStatus,
  ActionResponse,
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
  private async request<T>(path: string, method: 'GET' | 'POST' = 'GET'): Promise<T> {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), this.timeout);

    try {
      const response = await fetch(`${this.baseUrl}${path}`, {
        method,
        headers: {
          'Content-Type': 'application/json',
        },
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
