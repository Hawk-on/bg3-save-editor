import { ref } from "vue";
import { useInvokeCommand } from "./useApi";

export function useLsLib() {
  const lslibStatus = ref("");

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
