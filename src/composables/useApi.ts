import { invoke } from "@tauri-apps/api/core";

/**
 * Generic error handler that logs and returns user-friendly message
 */
function handleError(context: string, error: any): string {
  const message = typeof error === 'string' ? error : error?.message || 'Unknown error';
  console.error(`${context}:`, error);
  return `❌ ${context}: ${message}`;
}

/**
 * Invoke command with automatic error handling
 * @param command - The Tauri command name
 * @param args - Command arguments (optional)
 * @returns The command result or null on error
 */
export async function useInvokeCommand<T>(command: string, args: any = {}): Promise<T | null> {
  try {
    return await invoke<T>(command, args);
  } catch (e) {
    throw handleError(command, e);
  }
}
