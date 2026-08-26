<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'

import * as courseApi from './course.api'
import type { StudentCourseDetail } from './models/StudentCourseDetail'

const route = useRoute()
const course = ref<StudentCourseDetail | null>(null)
const problem = ref<ProblemDetails | null>(null)
const loading = ref(true)

onMounted(async () => {
  try {
    course.value = await courseApi.getStudentCourse(Number(route.params.id))
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <section>
    <p v-if="loading">Đang tải…</p>
    <ProblemAlert :problem="problem" />
    <template v-if="course">
      <RouterLink to="/learn" class="text-sm font-semibold text-indigo-700">← Khóa học</RouterLink>
      <h1 class="mt-3 text-3xl font-bold">{{ course.title }}</h1>
      <p class="mt-2 max-w-3xl text-slate-600">{{ course.description }}</p>
      <p class="mt-4 font-bold">
        Tiến độ {{ course.completedLessons }}/{{ course.totalLessons }} · {{ course.percentage }}%
      </p>
      <ol class="mt-6 space-y-3">
        <li v-for="lesson in course.lessons" :key="lesson.id">
          <RouterLink
            v-if="lesson.canAccess"
            :to="`/learn/lessons/${lesson.id}`"
            class="flex justify-between rounded-xl border bg-white p-5 hover:border-indigo-300"
          >
            <span class="font-semibold">{{ lesson.sortOrder }}. {{ lesson.title }}</span>
            <span class="text-sm">{{ lesson.state }}</span>
          </RouterLink>
          <div
            v-else
            class="flex justify-between rounded-xl border bg-slate-100 p-5 text-slate-500"
            data-testid="locked-lesson"
          >
            <span>{{ lesson.sortOrder }}. {{ lesson.title }}</span>
            <span>Đã khóa</span>
          </div>
        </li>
      </ol>
    </template>
  </section>
</template>
