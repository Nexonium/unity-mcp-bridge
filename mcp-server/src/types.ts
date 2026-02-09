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

/**
 * Screenshot quality preset.
 */
export type ScreenshotQuality = 'low' | 'medium' | 'high';

/**
 * Screenshot view type.
 */
export type ScreenshotView = 'game' | 'scene';

/**
 * Screenshot capture request parameters.
 */
export interface ScreenshotRequest {
  /** View to capture (game or scene). Default: game */
  view?: ScreenshotView;
  /** Quality preset (low, medium, high). Default: low */
  quality?: ScreenshotQuality;
}

/**
 * Screenshot capture response.
 */
export interface ScreenshotResponse {
  success: boolean;
  /** Absolute path to the screenshot file */
  filePath?: string;
  /** Image width in pixels */
  width?: number;
  /** Image height in pixels */
  height?: number;
  /** File size in bytes */
  fileSize?: number;
  /** Estimated token cost for AI processing */
  estimatedTokens?: number;
  /** View that was captured */
  view?: ScreenshotView;
  /** Quality preset used */
  quality?: ScreenshotQuality;
  /** Timestamp when screenshot was taken */
  timestamp?: string;
  /** Error message if capture failed */
  error?: string;
}

/**
 * Open asset request parameters.
 */
export interface OpenAssetRequest {
  /** Asset path relative to project (e.g., "Assets/Prefabs/Player.prefab") */
  path: string;
}

/**
 * Open asset response.
 */
export interface OpenAssetResponse {
  success: boolean;
  message?: string;
  assetType?: string;
  error?: string;
}

/**
 * Select object request parameters.
 */
export interface SelectObjectRequest {
  /** Full hierarchy path to the object (e.g., "Canvas/Panel/Button") */
  path?: string;
  /** Object name to search for */
  name?: string;
}

/**
 * Select object response.
 */
export interface SelectObjectResponse {
  success: boolean;
  message?: string;
  objectName?: string;
  error?: string;
}

/**
 * Frame selected response.
 */
export interface FrameSelectedResponse {
  success: boolean;
  message?: string;
  objectName?: string;
  error?: string;
}

/**
 * Hierarchy item representing a GameObject.
 */
export interface HierarchyItem {
  name: string;
  path: string;
  depth: number;
  childCount: number;
  active: boolean;
}

/**
 * Get hierarchy request parameters.
 */
export interface GetHierarchyRequest {
  /** Maximum depth to traverse (default: 10) */
  maxDepth?: number;
}

/**
 * Get hierarchy response.
 */
export interface GetHierarchyResponse {
  success: boolean;
  inPrefabMode: boolean;
  prefabName?: string;
  objectCount: number;
  hierarchy: HierarchyItem[];
  error?: string;
}

/**
 * Terminal command execution request.
 */
export interface TerminalExecuteRequest {
  /** Command line to execute (e.g., "time.scale 0.5") */
  command: string;
}

/**
 * Terminal command execution response.
 */
export interface TerminalExecuteResponse {
  success: boolean;
  command: string;
  output?: string;
  error?: string;
}

/**
 * Terminal status response.
 */
export interface TerminalStatusResponse {
  active: boolean;
  visible: boolean;
  logCount: number;
  commandCount: number;
  isPlaying: boolean;
}

/**
 * Terminal batch execute request.
 */
export interface TerminalBatchExecuteRequest {
  commands: string[];
}

/**
 * Single result within a batch execute response.
 */
export interface TerminalBatchResult {
  success: boolean;
  command: string;
  output?: string;
  error?: string;
}

/**
 * Terminal batch execute response.
 */
export interface TerminalBatchExecuteResponse {
  success: boolean;
  count: number;
  results: TerminalBatchResult[];
}

/**
 * Terminal logs request parameters.
 */
export interface TerminalLogsRequest {
  limit?: number;
  type?: string;
}

/**
 * Terminal log entry from the runtime debug terminal.
 */
export interface TerminalLogEntry {
  text: string;
  type: string;
  timestamp: string;
}

/**
 * Terminal logs response.
 */
export interface TerminalLogsResponse {
  active: boolean;
  count: number;
  logs: TerminalLogEntry[];
  error?: string;
}

/**
 * Terminal history request parameters.
 */
export interface TerminalHistoryRequest {
  limit?: number;
}

/**
 * Terminal history response.
 */
export interface TerminalHistoryResponse {
  active: boolean;
  count: number;
  history: string[];
  error?: string;
}
