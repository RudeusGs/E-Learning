<script setup lang="ts">
import { ref } from 'vue'

import AdminQuestionForm from '@/modules/exercises/AdminQuestionForm.vue'
import type { AdminQuestion } from '@/modules/exercises/models/AdminQuestion'
import type { QuestionWriteRequest } from '@/modules/exercises/models/QuestionWriteRequest'

import * as lessonApi from './lesson.api'
import type { AdminLesson } from './models/AdminLesson'

const props = defineProps<{ lesson: AdminLesson }>()
const questions = ref<AdminQuestion[]>([])
const expanded = ref(false)

async function toggle(): Promise<void> {
  expanded.value = !expanded.value
  if (expanded.value) questions.value = await lessonApi.getQuestions(props.lesson.id)
}

async function createQuestion(lessonId: number, request: QuestionWriteRequest): Promise<void> {
  await lessonApi.createQuestion(lessonId, request)
  questions.value = await lessonApi.getQuestions(lessonId)
}
</script>

<template>
  <article class="rounded-xl border bg-white p-5">
    <button
      type="button"
      class="flex w-full items-center justify-between text-left"
      @click="toggle"
    >
      <span
        ><strong>{{ lesson.sortOrder }}. {{ lesson.title }}</strong>
        <span class="ml-2 text-xs text-indigo-700">{{ lesson.status }}</span></span
      >
      <span>{{ expanded ? 'Thu gọn' : 'Câu hỏi' }}</span>
    </button>
    <div v-if="expanded" class="mt-4">
      <ul class="space-y-2 text-sm">
        <li v-for="question in questions" :key="question.id" class="rounded-lg border p-3">
          {{ question.text }} · {{ question.options.length }} lựa chọn
        </li>
        <li v-if="questions.length === 0" class="text-slate-500">Chưa có câu hỏi.</li>
      </ul>
      <AdminQuestionForm :lesson-id="lesson.id" :create="createQuestion" />
    </div>
  </article>
</template>
