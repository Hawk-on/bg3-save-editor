/**
 * API configuration
 */
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5062';

/**
 * Generic error handler that logs and returns user-friendly message
 */
function handleError(context: string, error: any): string {
  const message = typeof error === 'string' ? error : error?.message || 'Unknown error';
  console.error(`${context}:`, error);
  return `❌ ${context}: ${message}`;
}

/**
 * Make HTTP POST request to the backend API
 * @param endpoint - The API endpoint (e.g., '/api/save/load')
 * @param body - Request body object
 * @returns The response data
 */
export async function apiPost<T>(endpoint: string, body: any = {}): Promise<T> {
  try {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`HTTP ${response.status}: ${errorText}`);
    }

    return await response.json();
  } catch (e) {
    throw handleError(endpoint, e);
  }
}
