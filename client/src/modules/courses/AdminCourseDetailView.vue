<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'
import AdminLessonCard from '@/modules/lessons/AdminLessonCard.vue'
import * as lessonApi from '@/modules/lessons/lesson.api'
import type { AdminLesson } from '@/modules/lessons/models/AdminLesson'
import type { LessonStatus } from '@/types/LessonStatus'

const route = useRoute()
const courseId = Number(route.params.id)
const lessons = ref<AdminLesson[]>([])
const loading = ref(false)
const problem = ref<ProblemDetails | null>(null)
const form = reactive({
  title: '',
  description: '',
  contentHtml: '<p>Nội dung bài học</p>',
  videoUrl: '',
  sortOrder: 1,
  status: 'DRAFT' as LessonStatus,
})

async function load(): Promise<void> {
  loading.value = true
  try {
    lessons.value = await lessonApi.getAdminLessons(courseId)
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}

async function create(): Promise<void> {
  problem.value = null
  try {
    await lessonApi.createLesson(courseId, {
      title: form.title,
      description: form.description || null,
      contentHtml: form.contentHtml || null,
      videoUrl: form.videoUrl || null,
      sortOrder: form.sortOrder,
      status: form.status,
    })
    form.title = ''
    form.sortOrder += 1
    await load()
  } catch (error) {
    problem.value = toProblem(error)
  }
}

onMounted(load)
</script>

<template>
  <section>
    <RouterLink to="/admin/courses" class="text-sm font-semibold text-indigo-700"
      >← Khóa học</RouterLink
    >
    <h1 class="mt-3 text-3xl font-bold">Quản lý bài học</h1>
    <ProblemAlert class="mt-5" :problem="problem" />
    <div class="mt-6 grid gap-6 lg:grid-cols-[22rem_1fr]">
      <form class="h-fit rounded-xl border bg-white p-5" @submit.prevent="create">
        <h2 class="font-bold">Thêm bài học</h2>
        <div class="mt-4 space-y-3">
          <input
            v-model.trim="form.title"
            required
            class="w-full rounded-lg border px-3 py-2"
            placeholder="Tên bài học"
          />
          <textarea
            v-model.trim="form.description"
            class="w-full rounded-lg border px-3 py-2"
            placeholder="Mô tả"
          />
          <textarea
            v-model="form.contentHtml"
            class="h-36 w-full rounded-lg border px-3 py-2 font-mono text-sm"
            placeholder="HTML an toàn"
          />
          <input
            v-model.trim="form.videoUrl"
            class="w-full rounded-lg border px-3 py-2"
            placeholder="YouTube HTTPS URL"
          />
          <div class="grid grid-cols-2 gap-3">
            <select v-model="form.status" class="rounded-lg border px-3 py-2">
              <option value="DRAFT">DRAFT</option>
              <option value="PUBLISHED">PUBLISHED</option>
            </select>
            <input
              v-model.number="form.sortOrder"
              min="1"
              type="number"
              class="rounded-lg border px-3 py-2"
            />
          </div>
          <button class="w-full rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white">
            Tạo bài học
          </button>
        </div>
      </form>
      <div class="space-y-3">
        <p v-if="loading">Đang tải…</p>
        <p
          v-else-if="lessons.length === 0"
          class="rounded-xl border bg-white p-8 text-center text-slate-500"
        >
          Chưa có bài học.
        </p>
        <AdminLessonCard v-for="lesson in lessons" :key="lesson.id" :lesson="lesson" />
      </div>
    </div>
  </section>
</template>
