import { ref, computed } from "vue";
import { useInvokeCommand } from "./useApi";

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
      goldState.value = await useInvokeCommand<SaveState>("get_gold_count") || null;
      if (goldState.value) {
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
    
    isLoading.value = true;
    saveStatus.value = "Saving changes...";
    
    try {
      const result = await useInvokeCommand<string>("modify_and_save_gold", 
        { newGold: editedGold.value }) || "";
      
      const newSaveMatch = result.match(/New save: (.+\.lsv)/);
      
      if (newSaveMatch?.[1]) {
        const modifiedSavePath = newSaveMatch[1];
        saveStatus.value = "✅ Changes saved! Reloading modified save...";
        
        if (onSaveSuccess) {
          await onSaveSuccess(modifiedSavePath);
        }
        
        saveStatus.value = "✅ " + result + "\n\n✓ Modified save loaded successfully!";
      } else {
        saveStatus.value = "✅ " + result;
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
