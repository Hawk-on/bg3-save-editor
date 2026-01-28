<template>
  <section v-if="saveInfo" class="card info-card">
    <h3>Campaign Info</h3>
    <div class="info-row">
      <strong>Save Name:</strong>
      <span>{{ saveInfo["Save Name"] }}</span>
    </div>
    <div class="info-row">
      <strong>Level:</strong>
      <span>{{ saveInfo["Current Level"] }}</span>
    </div>
    <div class="info-row">
      <strong>Difficulty:</strong>
      <span v-if="saveInfo.Difficulty">{{ saveInfo.Difficulty[0] }}</span>
    </div>
  </section>
</template>

<script setup lang="ts">
import { inject } from "vue";

const saveInfo = inject<any>("saveInfo", null);
</script>

<style scoped>
@import '~/styles/globals.css';
@import '~/styles/components.css';

h3 {
  margin-top: 0;
  margin-bottom: 16px;
}
</style>
