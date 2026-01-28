import { ref } from "vue";
import { useInvokeCommand } from "./useApi";

// Shared state (singleton pattern)
const saveInfo = ref<any>(null);

export function useSaveExtraction() {
  /**
   * Extract and convert save file to editable format
   */
  async function extractSave(selectedSavePath: string): Promise<string> {
    const result = await useInvokeCommand<string>("extract_save", 
      { savePath: selectedSavePath });
    return result || "";
  }

  /**
   * Read save metadata (campaign info, difficulty, etc)
   */
  async function readSaveInfo() {
    try {
      saveInfo.value = await useInvokeCommand("read_save_info");
    } catch (e) {
      console.error("Failed to read save info", e);
    }
  }

  /**
   * Reset extraction state
   */
  function reset() {
    saveInfo.value = null;
  }

  return {
    saveInfo,
    extractSave,
    readSaveInfo,
    reset
  };
}
