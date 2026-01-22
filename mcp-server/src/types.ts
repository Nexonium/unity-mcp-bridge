/**
 * Configuration for the Unity MCP Bridge server.
 */
export interface ServerConfig {
  /** Unity HTTP server host. Default: 127.0.0.1 */
  unityHost: string;
  /** Unity HTTP server port. Default: 7890 */
  unityPort: number;
  /** Request timeout in milliseconds. Default: 5000 */
  timeout: number;
}

/**
 * Unity Editor status response.
 */
export interface UnityStatus {
  status: string;
  unityVersion: string;
  projectName: string;
  isPlaying: boolean;
  isPaused: boolean;
  isCompiling: boolean;
  hasCompilationErrors: boolean;
  logCount: number;
  timestamp: string;
}

/**
 * Log entry from Unity console.
 */
export interface LogEntry {
  message: string;
  stackTrace: string;
  type: 'log' | 'warning' | 'error' | 'assert' | 'exception';
  timestamp: string;
}

/**
 * Response containing log entries.
 */
export interface LogsResponse {
  count: number;
  logs: LogEntry[];
}

/**
 * Compilation error or warning.
 */
export interface CompilationError {
  message: string;
  file: string;
  line: number;
  column: number;
  assembly: string;
  severity: 'error' | 'warning';
  timestamp: string;
}

/**
 * Response containing compilation errors.
 */
export interface CompilationErrorsResponse {
  count: number;
  errors: CompilationError[];
}

/**
 * Response containing compilation warnings.
 */
export interface CompilationWarningsResponse {
  count: number;
  warnings: CompilationError[];
}

/**
 * Compilation status response.
 */
export interface CompilationStatus {
  isCompiling: boolean;
  hasErrors: boolean;
  hasWarnings: boolean;
  errorCount: number;
  warningCount: number;
}

/**
 * Generic action response.
 */
export interface ActionResponse {
  success: boolean;
  message: string;
  isPaused?: boolean;
}
