<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'
import ExerciseCard from '@/modules/exercises/ExerciseCard.vue'

import * as lessonApi from './lesson.api'
import type { StudentLesson } from './models/StudentLesson'

const route = useRoute()
const lesson = ref<StudentLesson | null>(null)
const loading = ref(true)
const completing = ref(false)
const completionPercentage = ref<number | null>(null)
const problem = ref<ProblemDetails | null>(null)
const embedUrl = computed(() =>
  lesson.value?.video?.provider === 'YOUTUBE'
    ? `https://www.youtube-nocookie.com/embed/${encodeURIComponent(lesson.value.video.externalId)}`
    : null,
)

async function load(): Promise<void> {
  loading.value = true
  problem.value = null
  try {
    lesson.value = await lessonApi.getStudentLesson(Number(route.params.id))
    await lessonApi.startLesson(lesson.value.id)
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}

async function complete(): Promise<void> {
  if (!lesson.value) return
  completing.value = true
  try {
    const result = await lessonApi.completeLesson(lesson.value.id)
    lesson.value.progressStatus = 'COMPLETED'
    completionPercentage.value = result.courseProgress.percentage
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    completing.value = false
  }
}

onMounted(load)
</script>

<template>
  <section>
    <p v-if="loading">Đang tải…</p>
    <div
      v-else-if="problem?.code === 'LESSON_LOCKED' || problem?.status === 403"
      class="rounded-xl border border-amber-300 bg-amber-50 p-8"
      data-testid="locked-state"
    >
      <h1 class="text-2xl font-bold text-amber-950">Bài học chưa mở khóa</h1>
      <p class="mt-2 text-amber-900">Hãy hoàn thành bài học đứng ngay trước bài này.</p>
      <ProblemAlert class="mt-4" :problem="problem" />
    </div>
    <ProblemAlert v-else-if="problem" :problem="problem" />
    <template v-if="lesson">
      <RouterLink
        :to="`/learn/courses/${lesson.courseId}`"
        class="text-sm font-semibold text-indigo-700"
        >← Lộ trình</RouterLink
      >
      <h1 class="mt-3 text-3xl font-bold">{{ lesson.title }}</h1>
      <p class="mt-2 text-slate-600">{{ lesson.description }}</p>
      <div v-if="embedUrl" class="mt-6 aspect-video overflow-hidden rounded-xl bg-black">
        <iframe class="h-full w-full" :src="embedUrl" title="YouTube lesson" allowfullscreen />
      </div>
      <!-- eslint-disable vue/no-v-html -->
      <article
        class="prose mt-6 max-w-none rounded-xl border bg-white p-6"
        v-html="lesson.contentHtml"
      />
      <!-- eslint-enable vue/no-v-html -->
      <div class="mt-8 space-y-4">
        <h2 class="text-xl font-bold">Bài tập</h2>
        <ExerciseCard
          v-for="question in lesson.questions"
          :key="question.id"
          :question="question"
        />
      </div>
      <div class="mt-8 flex items-center gap-4">
        <button
          type="button"
          class="rounded-lg bg-emerald-600 px-5 py-3 font-bold text-white disabled:opacity-60"
          :disabled="completing || lesson.progressStatus === 'COMPLETED'"
          @click="complete"
        >
          {{
            lesson.progressStatus === 'COMPLETED'
              ? 'Đã hoàn thành'
              : completing
                ? 'Đang cập nhật…'
                : 'Đánh dấu hoàn thành'
          }}
        </button>
        <p v-if="completionPercentage !== null" class="font-bold text-emerald-700">
          Tiến độ khóa học: {{ completionPercentage }}%
        </p>
      </div>
    </template>
  </section>
</template>
