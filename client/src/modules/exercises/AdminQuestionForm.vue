<script setup lang="ts">
import { reactive, ref } from 'vue'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'

import type { QuestionWriteRequest } from './models/QuestionWriteRequest'

const props = defineProps<{
  lessonId: number
  create: (lessonId: number, request: QuestionWriteRequest) => Promise<void>
}>()
const emit = defineEmits<{ saved: [] }>()
const loading = ref(false)
const problem = ref<ProblemDetails | null>(null)
const form = reactive({
  text: '',
  explanation: '',
  correct: '',
  incorrect: '',
  sortOrder: 1,
})

async function submit(): Promise<void> {
  loading.value = true
  problem.value = null
  try {
    await props.create(props.lessonId, {
      text: form.text,
      type: 'MULTIPLE_CHOICE',
      explanation: form.explanation || null,
      sortOrder: form.sortOrder,
      options: [
        { content: form.correct, isCorrect: true, sortOrder: 1 },
        { content: form.incorrect, isCorrect: false, sortOrder: 2 },
      ],
    })
    form.text = ''
    form.explanation = ''
    form.correct = ''
    form.incorrect = ''
    emit('saved')
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <form class="mt-4 rounded-lg bg-slate-50 p-4" @submit.prevent="submit">
    <h4 class="text-sm font-bold">Thêm câu hỏi</h4>
    <div class="mt-3 grid gap-2 md:grid-cols-2">
      <input
        v-model.trim="form.text"
        required
        class="rounded-lg border px-3 py-2 md:col-span-2"
        placeholder="Nội dung câu hỏi"
      />
      <input
        v-model.trim="form.correct"
        required
        class="rounded-lg border px-3 py-2"
        placeholder="Đáp án đúng"
      />
      <input
        v-model.trim="form.incorrect"
        required
        class="rounded-lg border px-3 py-2"
        placeholder="Đáp án sai"
      />
      <input
        v-model.trim="form.explanation"
        class="rounded-lg border px-3 py-2"
        placeholder="Giải thích"
      />
      <input
        v-model.number="form.sortOrder"
        min="1"
        type="number"
        class="rounded-lg border px-3 py-2"
      />
    </div>
    <ProblemAlert class="mt-3" :problem="problem" />
    <button class="mt-3 rounded-lg bg-slate-900 px-3 py-2 text-sm text-white" :disabled="loading">
      Lưu câu hỏi
    </button>
  </form>
</template>
