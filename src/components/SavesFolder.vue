<template>
  <section class="card load-card">
    <h2>📂 Select Saves Folder</h2>
    <div class="input-group">
      <input v-model="savesFolder" placeholder="Path to Story folder" />
      <button class="btn-secondary" @click="browseSavesFolder" :disabled="isLoading">
        📁 Browse
      </button>
      <button class="btn-primary" @click="loadSavesList" :disabled="isLoading">
        🔄 Load Saves
      </button>
    </div>
    
    <div v-if="hasAvailableSaves" class="saves-list">
      <h3>Available Saves:</h3>
      <select v-model="selectedSave" class="save-select">
        <option v-for="save in availableSaves" :key="save.path" :value="save.path">
          {{ save.name }}
        </option>
      </select>
      <button class="btn-primary" @click="onExtractSave" :disabled="isLoading || !selectedSave">
        {{ isLoading ? 'Processing...' : 'Load & Extract' }}
      </button>
    </div>
    
    <p class="status-text">{{ extractionStatus }}</p>
  </section>
</template>

<script setup lang="ts">
import { useSaveList } from "../composables/useSaveList";

const {
  savesFolder,
  availableSaves,
  selectedSave,
  extractionStatus,
  isLoading,
  hasAvailableSaves,
  browseSavesFolder,
  loadSavesList
} = useSaveList();

const emit = defineEmits<{
  extract: [savePath: string];
}>();

function onExtractSave() {
  if (selectedSave.value) {
    isLoading.value = true;
    emit("extract", selectedSave.value);
  }
}

defineExpose({
  setLoading: (loading: boolean) => {
    isLoading.value = loading;
  },
  setStatus: (status: string) => {
    extractionStatus.value = status;
  }
});
</script>

<style scoped>
@import '~/styles/globals.css';
@import '~/styles/components.css';

h2 {
  margin-top: 0;
  margin-bottom: 16px;
}

.saves-list {
  margin-top: 20px;
  padding-top: 20px;
  border-top: 1px solid var(--border-color);
}

.saves-list h3 {
  font-size: 1rem;
  margin: 0 0 12px 0;
  color: var(--text-secondary);
}
</style>
