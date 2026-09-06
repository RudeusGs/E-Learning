<script setup lang="ts">
import { ref, watch } from 'vue'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'
import type { StudentQuestion } from '@/modules/lessons/models/StudentQuestion'

import * as exerciseApi from './exercise.api'
import type { AnswerResult } from './models/AnswerResult'

const props = withDefaults(defineProps<{
  question: StudentQuestion
  index?: number
  disabled?: boolean
  lockedReason?: string
}>(), {
  index: 0,
  disabled: false,
  lockedReason: '',
})

const emit = defineEmits<{ answered: [questionId: number, result: AnswerResult] }>()
const selected = ref<number | null>(null)
const result = ref<AnswerResult | null>(null)
const problem = ref<ProblemDetails | null>(null)
const loading = ref(false)

function optionLetter(index: number): string {
  return String.fromCharCode(65 + index)
}

async function submit(): Promise<void> {
  if (props.disabled || selected.value === null || loading.value) return
  loading.value = true
  problem.value = null
  try {
    result.value = await exerciseApi.submitAnswer(props.question.id, selected.value)
    emit('answered', props.question.id, result.value)
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}

watch(selected, () => {
  if (result.value) result.value = null
  problem.value = null
})
watch(() => props.question.id, () => {
  selected.value = null
  result.value = null
  problem.value = null
})
</script>

<template>
  <form class="relative flex min-h-full flex-col rounded-xl border border-[#e4dcdb] bg-white p-4 shadow-[0_2px_10px_rgba(50,25,25,0.025)]" @submit.prevent="submit">
    <div v-if="disabled" class="absolute inset-0 z-10 grid place-items-center rounded-xl bg-white/82 p-5 text-center backdrop-blur-[1px]">
      <div>
        <span class="mx-auto grid size-9 place-items-center rounded-full bg-[#f1edec] text-[#756765]">
          <svg class="size-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9"><rect x="6" y="10" width="12" height="9" rx="2"/><path d="M8.5 10V7.5a3.5 3.5 0 0 1 7 0V10"/></svg>
        </span>
        <p class="mt-2 text-xs font-black text-[#514745]">Bài tập đang khóa</p>
        <p class="mt-1 text-[11px] leading-5 text-[#81716f]">{{ lockedReason || 'Hoàn thành phần học bắt buộc trước.' }}</p>
      </div>
    </div>

    <div class="flex items-start justify-between gap-3">
      <span class="rounded-md bg-[#f1edec] px-2 py-1 text-[10px] font-black text-[#5f5351]">Câu {{ index + 1 }}</span>
      <span v-if="question.passed" class="rounded-md bg-[#e9f7ef] px-2 py-1 text-[9px] font-black uppercase text-[#16764f]">Đã đúng</span>
    </div>
    <h3 class="mt-3 text-sm font-black leading-6 text-[#302929]">{{ question.text }}</h3>

    <div class="mt-3 space-y-2">
      <label v-for="(option, optionIndex) in question.options" :key="option.id" class="flex cursor-pointer items-center gap-3 rounded-lg border px-3 py-2.5 text-xs transition" :class="selected === option.id ? 'border-[#d88f98] bg-[#fff4f5]' : 'border-[#e3dbda] hover:border-[#d8b5b8]'">
        <input v-model="selected" type="radio" :name="`question-${question.id}`" :value="option.id" class="sr-only" />
        <span class="grid size-6 shrink-0 place-items-center rounded-md text-[10px] font-black" :class="selected === option.id ? 'bg-[#c9152b] text-white' : 'bg-[#f1edec] text-[#756765]'">{{ optionLetter(optionIndex) }}</span>
        <span class="min-w-0 flex-1 font-semibold leading-5 text-[#554b49]">{{ option.content }}</span>
      </label>
    </div>

    <div class="mt-auto pt-4">
      <div v-if="result" class="mb-3 rounded-lg border px-3 py-2.5 text-[11px] leading-5" :class="result.correct ? 'border-[#b9dfcc] bg-[#eff9f4] text-[#245b43]' : 'border-[#edcfaa] bg-[#fff8ed] text-[#7a592c]'">
        <strong>{{ result.correct ? 'Chính xác' : 'Chưa chính xác' }}</strong>
        <p v-if="result.explanation" class="mt-1">{{ result.explanation }}</p>
      </div>
      <ProblemAlert class="mb-3" :problem="problem" />
      <button type="submit" :disabled="selected === null || loading || disabled" class="h-9 w-full rounded-lg bg-[#c9152b] px-4 text-xs font-bold text-white hover:bg-[#a0001c] disabled:cursor-not-allowed disabled:opacity-45">
        {{ loading ? 'Đang chấm…' : result ? 'Chấm lại' : 'Trả lời' }}
      </button>
    </div>
  </form>
</template>
