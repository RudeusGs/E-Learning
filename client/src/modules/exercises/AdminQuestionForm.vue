<script setup lang="ts">
import { reactive, ref, watch } from 'vue'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'
import type { QuestionType } from '@/types/QuestionType'

import type { QuestionWriteRequest } from './models/QuestionWriteRequest'

const props = defineProps<{
  lessonId: number
  create: (lessonId: number, request: QuestionWriteRequest) => Promise<void>
}>()
const emit = defineEmits<{ saved: [] }>()
const loading = ref(false)
const problem = ref<ProblemDetails | null>(null)
const correctIndex = ref(0)
const form = reactive({
  text: '',
  type: 'MULTIPLE_CHOICE' as QuestionType,
  explanation: '',
  sortOrder: 1,
  options: ['', ''],
})

watch(
  () => form.type,
  (type) => {
    form.options = type === 'TRUE_FALSE' ? ['Đúng', 'Sai'] : ['', '']
    correctIndex.value = 0
  },
)

function addOption(): void {
  if (form.options.length < 4) form.options.push('')
}

function removeOption(index: number): void {
  if (form.options.length <= 2) return
  form.options.splice(index, 1)
  if (correctIndex.value >= form.options.length) correctIndex.value = 0
}

async function submit(): Promise<void> {
  loading.value = true
  problem.value = null
  try {
    await props.create(props.lessonId, {
      text: form.text,
      type: form.type,
      explanation: form.explanation || null,
      sortOrder: form.sortOrder,
      options: form.options.map((content, index) => ({
        content,
        isCorrect: index === correctIndex.value,
        sortOrder: index + 1,
      })),
    })
    form.text = ''
    form.explanation = ''
    form.type = 'MULTIPLE_CHOICE'
    form.options = ['', '']
    correctIndex.value = 0
    form.sortOrder += 1
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
    <div class="mt-3 grid gap-3 md:grid-cols-2">
      <input
        v-model.trim="form.text"
        required
        maxlength="2000"
        class="rounded-lg border px-3 py-2 md:col-span-2"
        placeholder="Nội dung câu hỏi"
      />
      <select v-model="form.type" class="rounded-lg border px-3 py-2">
        <option value="MULTIPLE_CHOICE">Nhiều lựa chọn</option>
        <option value="TRUE_FALSE">Đúng / Sai</option>
      </select>
      <input
        v-model.number="form.sortOrder"
        min="1"
        type="number"
        class="rounded-lg border px-3 py-2"
        aria-label="Thứ tự câu hỏi"
      />
      <input
        v-model.trim="form.explanation"
        class="rounded-lg border px-3 py-2 md:col-span-2"
        placeholder="Giải thích đáp án"
      />
    </div>
    <fieldset class="mt-4 space-y-2">
      <legend class="text-sm font-semibold">Các lựa chọn (chọn một đáp án đúng)</legend>
      <div v-for="(_, index) in form.options" :key="index" class="flex items-center gap-2">
        <input v-model="correctIndex" type="radio" :value="index" aria-label="Đáp án đúng" />
        <input
          v-model.trim="form.options[index]"
          required
          maxlength="1000"
          class="min-w-0 flex-1 rounded-lg border px-3 py-2"
          :readonly="form.type === 'TRUE_FALSE'"
          :placeholder="`Lựa chọn ${index + 1}`"
        />
        <button
          v-if="form.type === 'MULTIPLE_CHOICE' && form.options.length > 2"
          type="button"
          class="rounded border px-2 py-1 text-sm text-rose-700"
          @click="removeOption(index)"
        >
          Xóa
        </button>
      </div>
    </fieldset>
    <button
      v-if="form.type === 'MULTIPLE_CHOICE' && form.options.length < 4"
      type="button"
      class="mt-3 text-sm font-semibold text-indigo-700"
      @click="addOption"
    >
      + Thêm lựa chọn
    </button>
    <ProblemAlert class="mt-3" :problem="problem" />
    <button
      class="mt-3 rounded-lg bg-slate-900 px-3 py-2 text-sm text-white disabled:opacity-60"
      :disabled="loading"
    >
      {{ loading ? 'Đang lưu…' : 'Lưu câu hỏi' }}
    </button>
  </form>
</template>
