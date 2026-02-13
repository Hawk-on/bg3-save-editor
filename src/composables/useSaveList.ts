import { ref, computed } from "vue";
import { apiPost } from "./useApi";

export interface SaveEntry {
  name: string;
  path: string;
  modified: string;
  size?: number;
}

// Shared state (singleton pattern)
const savesFolder = ref("C:\\Users\\Hawkon\\AppData\\Local\\Larian Studios\\Baldur's Gate 3\\PlayerProfiles\\Public\\Savegames\\Story");
const availableSaves = ref<SaveEntry[]>([]);
const selectedSave = ref("");
const extractionStatus = ref("");
const isLoading = ref(false);

export function useSaveList() {

  const hasAvailableSaves = computed(() => availableSaves.value.length > 0);

  /**
   * Browse for saves folder (HTTP API doesn't support folder dialog, so we'll need manual input)
   */
  async function browseSavesFolder() {
    const newPath = prompt("Enter the path to your saves folder:", savesFolder.value);
    if (newPath) {
      savesFolder.value = newPath;
      await loadSavesList();
    }
  }

  /**
   * Load list of available saves from the selected folder
   */
  async function loadSavesList() {
    try {
      const response = await apiPost<SaveEntry[]>("/api/save/list", {
        Path: savesFolder.value
      });

      availableSaves.value = response.map((save: any) => ({
        name: save.name || save.Name,
        path: save.path || save.Path,
        modified: save.modified || save.Modified,
        size: save.size || save.Size
      }));

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
