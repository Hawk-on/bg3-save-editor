import { ref } from "vue";
import { apiPost } from "./useApi";

export interface SaveInfo {
  filePath: string;
  saveName: string;
  playerName: string;
  partyLevel: number;
  playTime: string;
  saveDate: string;
  gold: number;
}

export interface SaveState {
  info: SaveInfo;
  goldItems: Array<{
    handle: string;
    templateName: string;
    amount: number;
    location: string;
  }>;
  totalGold: number;
}

// Shared state (singleton pattern)
const saveInfo = ref<SaveInfo | null>(null);
const saveState = ref<SaveState | null>(null);
const currentSavePath = ref<string>("");

export function useSaveExtraction() {
  /**
   * Extract and load save file (combined operation in C# backend)
   */
  async function extractSave(selectedSavePath: string): Promise<string> {
    currentSavePath.value = selectedSavePath;

    const response = await apiPost<any>("/api/save/load", {
      Path: selectedSavePath
    });

    // Handle both camelCase (JS) and PascalCase (C#) property names
    const info = response.info || response.Info || {};
    saveState.value = {
      info: {
        filePath: info.filePath || info.FilePath || "",
        saveName: info.saveName || info.SaveName || "",
        playerName: info.playerName || info.PlayerName || "",
        partyLevel: info.partyLevel || info.PartyLevel || 0,
        playTime: info.playTime || info.PlayTime || "0",
        saveDate: info.saveDate || info.SaveDate || "",
        gold: response.totalGold || response.TotalGold || 0
      },
      goldItems: response.goldItems || response.GoldItems || [],
      totalGold: response.totalGold || response.TotalGold || 0
    };

    saveInfo.value = saveState.value.info;

    return `Save loaded successfully`;
  }

  /**
   * Read save metadata (already loaded with extractSave)
   */
  async function readSaveInfo() {
    // Data is already loaded in extractSave
    if (!saveInfo.value && currentSavePath.value) {
      await extractSave(currentSavePath.value);
    }
  }

  /**
   * Reset extraction state
   */
  function reset() {
    saveInfo.value = null;
    saveState.value = null;
  }

  return {
    saveInfo,
    saveState,
    currentSavePath,
    extractSave,
    readSaveInfo,
    reset
  };
}
