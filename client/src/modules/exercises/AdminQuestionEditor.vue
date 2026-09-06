<script setup lang="ts">
import { reactive, ref } from 'vue'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'
import * as lessonApi from '@/modules/lessons/lesson.api'

import type { AdminQuestion } from './models/AdminQuestion'

const props = defineProps<{ question: AdminQuestion }>()
const emit = defineEmits<{ changed: [] }>()
const editing = ref(false)
const loading = ref(false)
const problem = ref<ProblemDetails | null>(null)
const form = reactive({
  text: props.question.text,
  explanation: props.question.explanation ?? '',
  sortOrder: props.question.sortOrder,
  options: props.question.options.map((option) => ({ ...option })),
})

function chooseCorrect(index: number): void {
  form.options.forEach((option, optionIndex) => {
    option.isCorrect = optionIndex === index
  })
}

async function save(): Promise<void> {
  loading.value = true
  problem.value = null
  try {
    await lessonApi.updateQuestion(props.question.id, {
      text: form.text,
      type: props.question.type,
      explanation: form.explanation || null,
      sortOrder: form.sortOrder,
      version: props.question.version,
      options: form.options.map((option, index) => ({
        content: option.content,
        isCorrect: option.isCorrect,
        sortOrder: index + 1,
      })),
    })
    editing.value = false
    emit('changed')
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}

async function remove(): Promise<void> {
  if (!window.confirm('Xóa câu hỏi này?')) return
  loading.value = true
  problem.value = null
  try {
    await lessonApi.deleteQuestion(props.question.id)
    emit('changed')
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <article class="rounded-lg border p-4">
    <template v-if="!editing">
      <div class="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h4 class="font-semibold">{{ question.sortOrder }}. {{ question.text }}</h4>
          <p class="mt-1 text-xs text-slate-500">{{ question.type }}</p>
        </div>
        <div class="flex gap-2">
          <button type="button" class="rounded border px-3 py-1 text-sm" @click="editing = true">
            Sửa
          </button>
          <button
            type="button"
            class="rounded border border-rose-300 px-3 py-1 text-sm text-rose-700"
            :disabled="loading"
            @click="remove"
          >
            Xóa
          </button>
        </div>
      </div>
      <ul class="mt-3 grid gap-2 sm:grid-cols-2">
        <li
          v-for="option in question.options"
          :key="option.id"
          class="rounded bg-slate-50 px-3 py-2 text-sm"
          :class="option.isCorrect ? 'font-semibold text-emerald-700' : ''"
        >
          {{ option.content }} <span v-if="option.isCorrect">✓</span>
        </li>
      </ul>
      <p v-if="question.explanation" class="mt-3 text-sm text-slate-600">
        {{ question.explanation }}
      </p>
    </template>
    <form v-else class="space-y-3" @submit.prevent="save">
      <input v-model.trim="form.text" required class="w-full rounded-lg border px-3 py-2" />
      <input
        v-model.trim="form.explanation"
        class="w-full rounded-lg border px-3 py-2"
        placeholder="Giải thích"
      />
      <input
        v-model.number="form.sortOrder"
        min="1"
        type="number"
        class="w-28 rounded-lg border px-3 py-2"
        aria-label="Thứ tự câu hỏi"
      />
      <fieldset class="space-y-2">
        <legend class="text-sm font-semibold">Lựa chọn</legend>
        <div v-for="(option, index) in form.options" :key="option.id" class="flex gap-2">
          <input
            :checked="option.isCorrect"
            type="radio"
            :name="`correct-${question.id}`"
            @change="chooseCorrect(index)"
          />
          <input
            v-model.trim="option.content"
            required
            class="min-w-0 flex-1 rounded-lg border px-3 py-2"
          />
        </div>
      </fieldset>
      <ProblemAlert :problem="problem" />
      <div class="flex gap-2">
        <button class="rounded-lg bg-slate-900 px-4 py-2 text-white" :disabled="loading">
          Lưu
        </button>
        <button type="button" class="rounded-lg border px-4 py-2" @click="editing = false">
          Hủy
        </button>
      </div>
    </form>
    <ProblemAlert v-if="!editing" class="mt-3" :problem="problem" />
  </article>
</template>
