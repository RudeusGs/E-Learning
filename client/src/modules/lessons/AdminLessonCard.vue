<script setup lang="ts">
import type { AdminLessonListItem } from './models/AdminLessonListItem'

defineProps<{ lesson: AdminLessonListItem }>()
</script>

<template>
  <article class="rounded-xl border bg-white p-5">
    <div class="flex flex-wrap items-start justify-between gap-3">
      <div>
        <strong>{{ lesson.sortOrder }}. {{ lesson.title }}</strong>
        <span class="ml-2 text-xs font-semibold text-indigo-700">{{ lesson.status }}</span>
        <p v-if="lesson.description" class="mt-2 text-sm text-slate-600">
          {{ lesson.description }}
        </p>
      </div>
      <RouterLink
        :to="`/admin/lessons/${lesson.id}/edit`"
        class="rounded-lg border px-3 py-2 text-sm font-semibold hover:border-indigo-300"
      >
        Sửa & câu hỏi
      </RouterLink>
    </div>
  </article>
</template>
