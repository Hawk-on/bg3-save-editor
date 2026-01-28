<template>
  <section class="card gold-card">
    <h3>💰 Wealth Management</h3>
    <div v-if="isGoldLoaded" class="gold-display">
      <div class="total-gold">
        <span class="label">Total Gold</span>
        <span v-if="!isEditing" class="value">{{ goldState!.total_gold.toLocaleString() }}</span>
        <input 
          v-else 
          v-model.number="editedGold" 
          type="number" 
          class="gold-input"
          min="0"
          :max="999999"
        />
      </div>
      
      <div class="items-list">
        <h4>Sources Found:</h4>
        <ul>
          <li v-for="(item, idx) in goldState!.items" :key="idx">
            {{ item.name }}: <span class="gold-amount">{{ item.amount }}</span>
          </li>
        </ul>
      </div>
      
      <div class="button-group">
        <button 
          v-if="!isEditing" 
          @click="enableEditing" 
          class="btn-primary"
          :disabled="isLoading"
        >
          ✏️ Edit Gold
        </button>
        <template v-else>
          <button @click="onSaveChanges" class="btn-success" :disabled="isLoading">
            {{ isLoading ? 'Saving...' : '💾 Save Changes' }}
          </button>
          <button @click="cancelEditing" class="btn-secondary" :disabled="isLoading">
            ❌ Cancel
          </button>
        </template>
      </div>
      
      <p v-if="saveStatus" class="save-status">{{ saveStatus }}</p>
    </div>
    <div v-else-if="isLoading" class="loading-spinner">Analyzing wealth...</div>
    <div v-else class="placeholder">Load a save to view gold.</div>
  </section>
</template>

<script setup lang="ts">
import { useGoldEditor } from "../composables/useGoldEditor";

const {
  goldState,
  isEditing,
  editedGold,
  saveStatus,
  isLoading,
  isGoldLoaded,
  enableEditing,
  cancelEditing,
  saveGoldChanges
} = useGoldEditor();

const emit = defineEmits<{
  "gold-saved": [newSavePath: string];
}>();

async function onSaveChanges() {
  await saveGoldChanges((newSavePath) => {
    emit("gold-saved", newSavePath);
    return Promise.resolve();
  });
}
</script>

<style scoped>
@import '../styles/globals.css';
@import '../styles/components.css';

h3 {
  margin-top: 0;
  margin-bottom: 20px;
}

.total-gold {
  text-align: center;
  padding: 20px;
  background: var(--gold-bg);
  border-radius: var(--radius-md);
  border: 1px solid var(--gold-bg-border);
  margin-bottom: 16px;
}

.total-gold .label {
  display: block;
  font-size: 0.9rem;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: var(--gold-primary);
}

.total-gold .value {
  display: block;
  font-size: 2.5rem;
  font-weight: 800;
  color: var(--gold-primary);
  margin-top: 8px;
}

.gold-input {
  font-size: 2.5rem;
  font-weight: 800;
  color: var(--gold-primary);
  background: rgba(251, 191, 36, 0.15);
  border: 2px solid var(--gold-primary);
  border-radius: var(--radius-sm);
  padding: 8px 16px;
  text-align: center;
  width: 100%;
  max-width: 300px;
  margin-top: 8px;
  margin-left: auto;
  margin-right: auto;
  display: block;
}

.gold-input:focus {
  outline: none;
  border-color: var(--gold-secondary);
  box-shadow: 0 0 0 3px rgba(251, 191, 36, 0.2);
}

.gold-display {
  animation: fadeIn 0.3s ease-in;
}
</style>
