import { ref, computed } from "vue";
import { open } from "@tauri-apps/plugin-dialog";
import { useInvokeCommand } from "./useApi";

export interface SaveEntry {
  name: string;
  path: string;
  modified: string;
}

export function useSaveList() {
  const savesFolder = ref("%LOCALAPPDATA%\\Larian Studios\\Baldur's Gate 3\\PlayerProfiles\\Public\\Savegames\\Story");
  const availableSaves = ref<SaveEntry[]>([]);
  const selectedSave = ref("");
  const extractionStatus = ref("");
  const isLoading = ref(false);

  const hasAvailableSaves = computed(() => availableSaves.value.length > 0);

  /**
   * Open a folder browser dialog to select saves location
   */
  async function browseSavesFolder() {
    try {
      const selected = await open({
        multiple: false,
        directory: true,
        defaultPath: savesFolder.value
      });
      
      if (selected) {
        savesFolder.value = selected as string;
        await loadSavesList();
      }
    } catch (e) {
      console.error("Failed to open folder dialog", e);
    }
  }

  /**
   * Load list of available saves from the selected folder
   */
  async function loadSavesList() {
    try {
      availableSaves.value = await useInvokeCommand<SaveEntry[]>("list_saves", 
        { folderPath: savesFolder.value }) || [];
      
      if (hasAvailableSaves.value) {
        selectedSave.value = availableSaves.value[0].path;
        extractionStatus.value = `Found ${availableSaves.value.length} save(s)`;
      } else {
        extractionStatus.value = "No saves found in folder";
      }
    } catch (e) {
      extractionStatus.value = e as string;
    }
  }

  return {
    savesFolder,
    availableSaves,
    selectedSave,
    extractionStatus,
    isLoading,
    hasAvailableSaves,
    browseSavesFolder,
    loadSavesList
  };
}
