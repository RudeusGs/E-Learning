<script setup lang="ts">
import type { ProblemDetails } from '@/api/http'

defineProps<{ problem: ProblemDetails | null }>()
</script>

<template>
  <div
    v-if="problem"
    role="alert"
    class="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"
  >
    <p class="font-semibold">{{ problem.title }}</p>
    <p v-if="problem.detail" class="mt-1">{{ problem.detail }}</p>
    <p class="mt-1 text-xs">
      {{ problem.code }}<span v-if="problem.traceId"> · {{ problem.traceId }}</span>
    </p>
  </div>
</template>
