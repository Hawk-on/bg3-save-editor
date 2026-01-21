<script setup lang="ts">
import { ref } from "vue";
import { invoke } from "@tauri-apps/api/core";

const greetMsg = ref("");
const name = ref("");
const lslibStatus = ref("");
const savePath = ref("C:\\Git\\BG3 savegame editor\\sample_save.lsv");
const extractionStatus = ref("");
const saveInfo = ref("");

async function greet() {
  greetMsg.value = await invoke("greet", { name: name.value });
}

async function checkLslib() {
  try {
    lslibStatus.value = await invoke("check_lslib_status");
  } catch (e) {
    lslibStatus.value = "Error: " + e;
  }
}

async function extractSave() {
  extractionStatus.value = "Extracting...";
  try {
    const result = await invoke("extract_save", { savePath: savePath.value });
    extractionStatus.value = result as string;
    await readInfo();
  } catch (e) {
    extractionStatus.value = "Error: " + e;
  }
}

async function readInfo() {
  try {
    const info = await invoke("read_save_info");
    saveInfo.value = JSON.stringify(info, null, 2);
  } catch (e) {
    saveInfo.value = "Error reading info: " + e;
  }
}
</script>

<template>
  <div class="container">
    <h1>BG3 Save Editor</h1>

    <div class="row">
      <form @submit.prevent="greet">
        <input v-model="name" placeholder="Enter a name..." />
        <button type="submit">Greet</button>
      </form>
      <p>{{ greetMsg }}</p>
    </div>

    <hr/>

    <div class="section">
      <button @click="checkLslib">Check LSLib Status</button>
      <p>{{ lslibStatus }}</p>
    </div>

    <div class="section">
      <h3>Extract Save</h3>
      <input v-model="savePath" placeholder="Path to .lsv file" style="width: 100%" />
      <button @click="extractSave">Extract & Load Info</button>
      <p>{{ extractionStatus }}</p>
    </div>

    <div class="section" v-if="saveInfo">
      <h3>Save Data</h3>
      <pre>{{ saveInfo }}</pre>
    </div>
  </div>
</template>

<style scoped>
.container { padding: 20px; max-width: 800px; margin: 0 auto; }
.section { margin-top: 20px; border: 1px solid #ccc; padding: 10px; border-radius: 8px; }
pre { text-align: left; background: #222; color: #cfc; padding: 10px; overflow: auto; max-height: 400px; }
input { padding: 5px; margin-right: 10px; }
button { padding: 5px 10px; cursor: pointer; }
</style>
