import { ref } from "vue";

// Shared state (singleton pattern)
const lslibStatus = ref("");

export function useLsLib() {
  /**
   * Check if the backend API is available (replaces LSLib check)
   */
  async function checkLslib() {
    try {
      const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5062';
      await fetch(`${API_BASE_URL}/api/save/list`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ Path: '' })
      });

      // Any response (even error) means the server is running
      lslibStatus.value = "✅ Backend API is running";
    } catch (e) {
      lslibStatus.value = "❌ Backend API not available. Please start the backend server.";
      console.error("Failed to connect to backend:", e);
    }
  }

  return {
    lslibStatus,
    checkLslib
  };
}
