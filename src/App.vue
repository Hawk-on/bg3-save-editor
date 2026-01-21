<script setup lang="ts">
import { ref } from "vue";
import { invoke } from "@tauri-apps/api/core";

// Interfaces
interface GoldItemDisplay {
  name: string;
  amount: number;
}

interface SaveState {
  total_gold: number;
  items: GoldItemDisplay[];
}

// State
const lslibStatus = ref("");
const savePath = ref("C:\\Git\\BG3 savegame editor\\sample_save.lsv");
const extractionStatus = ref("");
const saveInfo = ref<any>(null);
const isLoading = ref(false);
const goldState = ref<SaveState | null>(null);
const editedGold = ref(0);
const isSaving = ref(false);
const saveStatus = ref("");
const outputPath = ref("");

// Actions
async function checkLslib() {
  try {
    lslibStatus.value = await invoke("check_lslib_status");
    // Also verify divine.exe integration
    const divineStatus = await invoke("verify_divine_integration");
    lslibStatus.value += " | " + divineStatus;
  } catch (e) {
    lslibStatus.value = "❌ Error: " + e;
  }
}

async function extractSave() {
  if (!savePath.value) return;
  
  isLoading.value = true;
  extractionStatus.value = "Extracting save file... (This may take a moment)";
  goldState.value = null; // Reset
  
  try {
    const result = await invoke("extract_save", { savePath: savePath.value });
    extractionStatus.value = "✅ " + (result as string);
    await readInfo();
    await loadGold();
  } catch (e) {
    extractionStatus.value = "❌ Error: " + e;
  } finally {
    isLoading.value = false;
  }
}

async function readInfo() {
  try {
    saveInfo.value = await invoke("read_save_info");
  } catch (e) {
    console.error(e);
  }
}

async function loadGold() {
  try {
    // This parses the 100MB file, might be slow
    goldState.value = await invoke("get_gold_count");
    editedGold.value = goldState.value?.total_gold || 0;
  } catch (e) {
    console.error("Failed to load gold info", e);
  }
}

async function updateGoldValue() {
  if (!goldState.value) return;
  
  isSaving.value = true;
  saveStatus.value = "Updating gold value...";
  
  try {
    await invoke("update_gold", { newGold: editedGold.value });
    saveStatus.value = "✅ Gold value updated in save data";
    // Reload to confirm
    await loadGold();
  } catch (e) {
    saveStatus.value = "❌ Error updating gold: " + e;
  } finally {
    isSaving.value = false;
  }
}

async function exportSave() {
  if (!outputPath.value) {
    saveStatus.value = "❌ Please specify an output path";
    return;
  }
  
  isSaving.value = true;
  saveStatus.value = "Repacking save file...";
  
  try {
    const result = await invoke("repack_save", { outputPath: outputPath.value });
    saveStatus.value = "✅ " + result;
  } catch (e) {
    saveStatus.value = "❌ Error repacking save: " + e;
  } finally {
    isSaving.value = false;
  }
}
</script>

<template>
  <div class="app-container">
    <header>
      <h1>🔮 BG3 Save Editor</h1>
      <p class="subtitle">Mod your Baldur's Gate 3 save files with ease.</p>
    </header>

    <main>
      <!-- Tools Status -->
      <section class="card status-card">
        <div class="status-header">
          <span>System Status:</span>
          <span :class="{'status-ok': lslibStatus.includes('found'), 'status-err': lslibStatus.includes('Error')}">
            {{ lslibStatus || 'Unchecked' }}
          </span>
        </div>
        <button class="btn-secondary" @click="checkLslib" v-if="!lslibStatus">Check Tools</button>
      </section>

      <!-- Save Loading -->
      <section class="card load-card">
        <h2>📂 Load Save File</h2>
        <div class="input-group">
          <input v-model="savePath" placeholder="Type absolute path to .lsv file" />
          <button class="btn-primary" @click="extractSave" :disabled="isLoading">
            {{ isLoading ? 'Processing...' : 'Load & Extract' }}
          </button>
        </div>
        <p class="status-text">{{ extractionStatus }}</p>
      </section>

      <!-- Dashboard -->
      <div v-if="saveInfo" class="dashboard-grid">
        <!-- Overview -->
        <section class="card info-card">
          <h3>Campaign Info</h3>
          <div class="info-row">
            <strong>Save Name:</strong> <span>{{ saveInfo["Save Name"] }}</span>
          </div>
          <div class="info-row">
            <strong>Level:</strong> <span>{{ saveInfo["Current Level"] }}</span>
          </div>
          <div class="info-row">
            <strong>Difficulty:</strong> 
            <span v-if="saveInfo.Difficulty">{{ saveInfo.Difficulty[0] }}</span>
          </div>
        </section>

        <!-- Gold Editor -->
        <section class="card gold-card">
          <h3>💰 Wealth Management</h3>
          <div v-if="goldState" class="gold-display">
             <div class="total-gold">
               <span class="label">Current Gold</span>
               <span class="value">{{ goldState.total_gold.toLocaleString() }}</span>
             </div>
             
             <div class="gold-editor">
               <label for="gold-input">Set New Gold Amount:</label>
               <input 
                 id="gold-input"
                 type="number" 
                 v-model.number="editedGold" 
                 min="0"
                 class="gold-input"
               />
               <button 
                 class="btn-primary" 
                 @click="updateGoldValue"
                 :disabled="isSaving"
               >
                 {{ isSaving ? 'Updating...' : 'Update Gold' }}
               </button>
             </div>
             
             <div class="items-list">
               <h4>Gold Items Found:</h4>
               <ul>
                 <li v-for="(item, idx) in goldState.items" :key="idx">
                   {{ item.name }}: <span class="gold-amount">{{ item.amount }}</span>
                 </li>
               </ul>
             </div>
          </div>
          <div v-else-if="isLoading" class="loading-spinner">Analyzing wealth...</div>
          <div v-else class="placeholder">Load a save to view gold.</div>
        </section>
      </div>

      <!-- Export Section -->
      <section v-if="saveInfo" class="card export-card">
        <h2>💾 Export Modified Save</h2>
        <div class="input-group">
          <input 
            v-model="outputPath" 
            placeholder="Output path (e.g., C:\path\to\modified_save.lsv)" 
          />
          <button 
            class="btn-primary" 
            @click="exportSave"
            :disabled="isSaving || !outputPath"
          >
            {{ isSaving ? 'Exporting...' : 'Export Save' }}
          </button>
        </div>
        <p class="status-text">{{ saveStatus }}</p>
        <p class="hint">⚠️ Make sure to backup your original save file before using the modified one!</p>
      </section>
    </main>
  </div>
</template>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;600;800&display=swap');

:global(body) {
  margin: 0;
  background-color: #0f172a;
  color: #e2e8f0;
  font-family: 'Inter', sans-serif;
}

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
  background: linear-gradient(135deg, #fbbf24, #d97706);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  margin: 0;
}

.subtitle {
  color: #94a3b8;
  font-size: 1.1rem;
}

/* Cards */
.card {
  background: rgba(30, 41, 59, 0.7);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 16px;
  padding: 24px;
  margin-bottom: 24px;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
}

.dashboard-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 24px;
}

/* Inputs & Buttons */
.input-group {
  display: flex;
  gap: 12px;
  margin-top: 16px;
}

input {
  flex: 1;
  background: #334155;
  border: 1px solid #475569;
  color: white;
  padding: 12px;
  border-radius: 8px;
  font-size: 1rem;
}

button {
  cursor: pointer;
  border: none;
  border-radius: 8px;
  padding: 12px 24px;
  font-weight: 600;
  transition: all 0.2s;
}

.btn-primary {
  background: linear-gradient(135deg, #3b82f6, #2563eb);
  color: white;
}

.btn-primary:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(37, 99, 235, 0.3);
}

.btn-primary:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.btn-secondary {
  background: #334155;
  color: #e2e8f0;
}

/* Status */
.status-text {
  margin-top: 10px;
  font-size: 0.9rem;
  color: #94a3b8;
}

.status-ok { color: #4ade80; }
.status-err { color: #f87171; }

/* Info Styles */
.info-row {
  display: flex;
  justify-content: space-between;
  padding: 8px 0;
  border-bottom: 1px solid rgba(255,255,255,0.05);
}

/* Gold Styles */
.total-gold {
  text-align: center;
  padding: 20px;
  background: rgba(251, 191, 36, 0.1);
  border-radius: 12px;
  border: 1px solid rgba(251, 191, 36, 0.2);
  margin-bottom: 16px;
}

.total-gold .label {
  display: block;
  font-size: 0.9rem;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: #fbbf24;
}

.total-gold .value {
  font-size: 2.5rem;
  font-weight: 800;
  color: #fbbf24;
}

.items-list ul {
  list-style: none;
  padding: 0;
  max-height: 150px;
  overflow-y: auto;
}

.items-list li {
  display: flex;
  justify-content: space-between;
  padding: 6px;
  border-bottom: 1px solid rgba(255,255,255,0.05);
  font-size: 0.9rem;
}

.gold-amount {
  color: #fbbf24;
  font-weight: 600;
}

.hint {
  font-size: 0.8rem;
  text-align: center;
  color: #64748b;
  margin-top: 12px;
}

.gold-editor {
  margin: 20px 0;
  padding: 16px;
  background: rgba(59, 130, 246, 0.1);
  border-radius: 8px;
  border: 1px solid rgba(59, 130, 246, 0.2);
}

.gold-editor label {
  display: block;
  margin-bottom: 8px;
  font-weight: 600;
  color: #60a5fa;
}

.gold-input {
  width: 100%;
  margin-bottom: 12px;
  font-size: 1.2rem;
  text-align: center;
}

.export-card {
  border: 2px solid rgba(34, 197, 94, 0.3);
}

.placeholder {
  text-align: center;
  color: #64748b;
  padding: 20px;
}
</style>
