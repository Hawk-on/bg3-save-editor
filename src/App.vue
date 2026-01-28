<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useLsLib } from "./composables/useLsLib";
import { useSaveList } from "./composables/useSaveList";
import { useSaveExtraction } from "./composables/useSaveExtraction";
import { useGoldEditor } from "./composables/useGoldEditor";
import LslibStatus from "./components/LslibStatus.vue";
import SavesFolder from "./components/SavesFolder.vue";
import SaveInfo from "./components/SaveInfo.vue";
import GoldEditor from "./components/GoldEditor.vue";

// ============================================================================
// Compose Injected Composables
// ============================================================================
const { checkLslib } = useLsLib();
const { loadSavesList, selectedSave: selectedSavePath } = useSaveList();
const {
  saveInfo,
  extractSave,
  readSaveInfo,
  reset: resetSaveExtraction
} = useSaveExtraction();
const {
  loadGoldInfo,
  reset: resetGoldEditor
} = useGoldEditor();

// ============================================================================
// Component References for Imperative Updates
// ============================================================================
const savesFolderRef = ref();
const goldEditorRef = ref();

// ============================================================================
// Handler Functions
// ============================================================================

/**
 * Handle save extraction from SavesFolder component
 */
async function handleExtractSave(savePath: string) {
  try {
    savesFolderRef.value?.setStatus("Extracting save file... (This may take a moment)");
    const result = await extractSave(savePath);
    savesFolderRef.value?.setStatus("✅ " + result);
    await readSaveInfo();
    await loadGoldInfo();
  } catch (e) {
    savesFolderRef.value?.setStatus(e as string);
  } finally {
    savesFolderRef.value?.setLoading(false);
  }
}

/**
 * Handle gold save completion - reload the modified save
 */
async function handleGoldSaved(newSavePath: string) {
  selectedSavePath.value = newSavePath;
  resetSaveExtraction();
  resetGoldEditor();
  await handleExtractSave(newSavePath);
}

// ============================================================================
// Lifecycle Hooks
// ============================================================================

onMounted(async () => {
  await checkLslib();
  await loadSavesList();
});
</script>

<template>
  <div class="app-container">
    <header>
      <h1>🔮 BG3 Save Editor</h1>
      <p class="subtitle">Mod your Baldur's Gate 3 save files with ease.</p>
    </header>

    <main>
      <!-- Tools Status -->
      <LslibStatus />

      <!-- Save Loading -->
      <SavesFolder 
        ref="savesFolderRef"
        @extract="handleExtractSave"
      />

      <!-- Dashboard -->
      <div v-if="saveInfo" class="dashboard-grid">
        <SaveInfo />
        <GoldEditor 
          ref="goldEditorRef"
          @gold-saved="handleGoldSaved"
        />
      </div>
    </main>
  </div>
</template>

<style scoped>
@import '~/styles/globals.css';
@import '~/styles/components.css';

.app-container {
  max-width: 900px;
  margin: 0 auto;
  padding: 40px 20px;
}

header {
  text-align: center;
  margin-bottom: 40px;
}

h1 {
  font-size: 3rem;
  background: linear-gradient(135deg, var(--gold-primary), #d97706);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.subtitle {
  color: var(--text-secondary);
  font-size: 1.1rem;
  margin-top: 8px;
}

main {
  display: flex;
  flex-direction: column;
  gap: 24px;
}
</style>
