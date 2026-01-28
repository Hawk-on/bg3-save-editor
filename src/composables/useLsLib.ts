import { ref } from "vue";
import { useInvokeCommand } from "./useApi";

// Shared state (singleton pattern)
const lslibStatus = ref("");

export function useLsLib() {
  /**
   * Check if LSLib tools are installed and accessible
   */
  async function checkLslib() {
    try {
      lslibStatus.value = await useInvokeCommand<string>("check_lslib_status") || "";
    } catch (e) {
      lslibStatus.value = e as string;
    }
  }

  return {
    lslibStatus,
    checkLslib
  };
}
