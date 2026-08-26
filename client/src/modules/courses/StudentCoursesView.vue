<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { toProblem, type ProblemDetails } from '@/api/http'
import CursorLoadMore from '@/components/CursorLoadMore.vue'
import ProblemAlert from '@/components/ProblemAlert.vue'

import * as courseApi from './course.api'
import type { StudentCourse } from './models/StudentCourse'

const courses = ref<StudentCourse[]>([])
const nextCursor = ref<string | null>(null)
const hasMore = ref(false)
const loading = ref(false)
const problem = ref<ProblemDetails | null>(null)

async function load(): Promise<void> {
  loading.value = true
  try {
    const page = await courseApi.getStudentCourses(nextCursor.value)
    courses.value.push(...page.items)
    nextCursor.value = page.nextCursor
    hasMore.value = page.hasMore
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <section>
    <h1 class="text-3xl font-bold">Khóa học của tôi</h1>
    <p class="mt-1 text-slate-600">Chỉ hiển thị khóa học đã được cấp và đang published.</p>
    <ProblemAlert class="mt-5" :problem="problem" />
    <p v-if="loading && courses.length === 0" class="mt-6">Đang tải…</p>
    <p
      v-else-if="!loading && courses.length === 0"
      class="mt-6 rounded-xl border bg-white p-10 text-center text-slate-500"
    >
      Bạn chưa có khóa học nào.
    </p>
    <div class="mt-6 grid gap-5 md:grid-cols-2 xl:grid-cols-3">
      <RouterLink
        v-for="course in courses"
        :key="course.id"
        :to="`/learn/courses/${course.id}`"
        class="overflow-hidden rounded-xl border bg-white shadow-sm hover:border-indigo-300"
      >
        <img
          v-if="course.thumbnailUrl"
          :src="course.thumbnailUrl"
          alt=""
          class="h-40 w-full object-cover"
        />
        <div class="p-5">
          <h2 class="font-bold">{{ course.title }}</h2>
          <p class="mt-2 line-clamp-2 text-sm text-slate-600">{{ course.description }}</p>
          <div class="mt-4 h-2 overflow-hidden rounded bg-slate-100">
            <div class="h-full bg-indigo-600" :style="{ width: `${course.percentage}%` }" />
          </div>
          <p class="mt-2 text-sm font-semibold">
            {{ course.completedLessons }}/{{ course.totalLessons }} · {{ course.percentage }}%
          </p>
        </div>
      </RouterLink>
    </div>
    <CursorLoadMore class="mt-6" :has-more="hasMore" :loading="loading" @load="load" />
  </section>
</template>
