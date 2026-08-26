<script setup lang="ts">
defineProps<{ hasMore: boolean; loading: boolean }>()
defineEmits<{ load: [] }>()
</script>

<template>
  <button
    v-if="hasMore"
    type="button"
    class="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold hover:bg-slate-50 disabled:opacity-60"
    :disabled="loading"
    @click="$emit('load')"
  >
    {{ loading ? 'Đang tải…' : 'Tải thêm' }}
  </button>
</template>
