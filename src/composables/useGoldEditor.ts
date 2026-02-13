import { ref, computed } from "vue";
import { apiPost } from "./useApi";
import { useSaveExtraction } from "./useSaveExtraction";

export interface GoldItemDisplay {
  name: string;
  amount: number;
}

export interface SaveState {
  total_gold: number;
  items: GoldItemDisplay[];
}

// Shared state (singleton pattern)
const goldState = ref<SaveState | null>(null);
const isEditing = ref(false);
const editedGold = ref(0);
const saveStatus = ref("");
const isLoading = ref(false);

export function useGoldEditor() {

  const isGoldLoaded = computed(() => goldState.value !== null);
  const hasChangedGold = computed(() =>
    isGoldLoaded.value && editedGold.value !== goldState.value!.total_gold
  );

  /**
   * Load gold information from the extracted save
   */
  async function loadGoldInfo() {
    try {
      const { saveState: extractedState } = useSaveExtraction();

      if (extractedState.value) {
        goldState.value = {
          total_gold: extractedState.value.totalGold,
          items: extractedState.value.goldItems.map(item => ({
            name: item.templateName,
            amount: item.amount
          }))
        };
        editedGold.value = goldState.value.total_gold;
      }
    } catch (e) {
      console.error("Failed to load gold info", e);
    }
  }

  /**
   * Enable editing mode for gold amount
   */
  function enableEditing() {
    isEditing.value = true;
    editedGold.value = goldState.value?.total_gold || 0;
  }

  /**
   * Cancel editing and discard changes
   */
  function cancelEditing() {
    isEditing.value = false;
    saveStatus.value = "";
  }

  /**
   * Save modified gold amount back to save file
   */
  async function saveGoldChanges(onSaveSuccess?: (newSavePath: string) => Promise<void>) {
    if (!goldState.value) return;

    const { currentSavePath } = useSaveExtraction();
    if (!currentSavePath.value) {
      saveStatus.value = "❌ No save file loaded";
      return;
    }

    isLoading.value = true;
    saveStatus.value = "Saving changes...";

    try {
      const response = await apiPost<{ success: boolean; outputPath: string }>("/api/save/gold", {
        Path: currentSavePath.value,
        Amount: editedGold.value
      });

      if (response.success && response.outputPath) {
        const modifiedSavePath = response.outputPath;
        saveStatus.value = "✅ Changes saved! Reloading modified save...";

        if (onSaveSuccess) {
          await onSaveSuccess(modifiedSavePath);
        }

        saveStatus.value = `✅ Gold modified successfully!\nNew save: ${modifiedSavePath}`;
      } else {
        saveStatus.value = "✅ Changes saved!";
      }

      isEditing.value = false;
    } catch (e) {
      saveStatus.value = e as string;
    } finally {
      isLoading.value = false;
    }
  }

  /**
   * Reset gold editor state
   */
  function reset() {
    goldState.value = null;
    isEditing.value = false;
    editedGold.value = 0;
    saveStatus.value = "";
  }

  return {
    goldState,
    isEditing,
    editedGold,
    saveStatus,
    isLoading,
    isGoldLoaded,
    hasChangedGold,
    loadGoldInfo,
    enableEditing,
    cancelEditing,
    saveGoldChanges,
    reset
  };
}
