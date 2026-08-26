<script setup lang="ts">
import { ref } from 'vue'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'
import type { StudentQuestion } from '@/modules/lessons/models/StudentQuestion'

import * as exerciseApi from './exercise.api'
import type { AnswerResult } from './models/AnswerResult'

const props = defineProps<{ question: StudentQuestion }>()
const selected = ref<number | null>(null)
const result = ref<AnswerResult | null>(null)
const problem = ref<ProblemDetails | null>(null)
const loading = ref(false)

async function submit(): Promise<void> {
  if (selected.value === null) return
  loading.value = true
  problem.value = null
  try {
    result.value = await exerciseApi.submitAnswer(props.question.id, selected.value)
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <form class="rounded-xl border bg-white p-5" @submit.prevent="submit">
    <h3 class="font-bold">{{ question.text }}</h3>
    <label v-for="option in question.options" :key="option.id" class="mt-3 flex gap-2 text-sm">
      <input v-model="selected" type="radio" :name="`question-${question.id}`" :value="option.id" />
      {{ option.content }}
    </label>
    <button
      class="mt-4 rounded-lg bg-slate-900 px-4 py-2 text-sm font-semibold text-white"
      :disabled="selected === null || loading"
    >
      {{ loading ? 'Đang chấm…' : 'Trả lời' }}
    </button>
    <div
      v-if="result"
      class="mt-4 rounded-lg p-3 text-sm"
      :class="result.correct ? 'bg-emerald-50 text-emerald-800' : 'bg-amber-50 text-amber-900'"
    >
      <p class="font-bold">{{ result.correct ? 'Chính xác' : 'Chưa chính xác' }}</p>
      <p v-if="result.explanation" class="mt-1">{{ result.explanation }}</p>
    </div>
    <ProblemAlert class="mt-4" :problem="problem" />
  </form>
</template>
