<template>
  <section class="card status-card">
    <div class="status-header">
      <span>System Status:</span>
      <span :class="{
        'status-ok': status.includes('found'), 
        'status-err': status.includes('Error')
      }">
        {{ status || 'Unchecked' }}
      </span>
    </div>
    <button class="btn-secondary" @click="checkLslib" v-if="!status">
      Check Tools
    </button>
  </section>
</template>

<script setup lang="ts">
import { useLsLib } from "../composables/useLsLib";

const { lslibStatus: status, checkLslib } = useLsLib();
</script>

<style scoped>
@import '~/styles/globals.css';
@import '~/styles/components.css';

.status-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.status-header span:last-child {
  font-weight: 600;
}

h2 {
  margin-bottom: 16px;
  font-size: 1.1rem;
}
</style>
