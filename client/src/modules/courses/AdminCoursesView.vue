<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'

import { toProblem, type ProblemDetails } from '@/api/http'
import CursorLoadMore from '@/components/CursorLoadMore.vue'
import ProblemAlert from '@/components/ProblemAlert.vue'
import type { CourseStatus } from '@/types/CourseStatus'

import * as courseApi from './course.api'
import type { AdminCourse } from './models/AdminCourse'

const courses = ref<AdminCourse[]>([])
const nextCursor = ref<string | null>(null)
const hasMore = ref(false)
const loading = ref(false)
const saving = ref(false)
const problem = ref<ProblemDetails | null>(null)
const search = ref('')
const form = reactive({
  title: '',
  description: '',
  thumbnailUrl: '',
  status: 'DRAFT' as CourseStatus,
  sortOrder: 1,
})

async function load(reset = false): Promise<void> {
  loading.value = true
  problem.value = null
  try {
    const page = await courseApi.getAdminCourses(reset ? null : nextCursor.value, search.value)
    courses.value = reset ? page.items : [...courses.value, ...page.items]
    nextCursor.value = page.nextCursor
    hasMore.value = page.hasMore
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}

async function create(): Promise<void> {
  saving.value = true
  problem.value = null
  try {
    await courseApi.createCourse({
      title: form.title,
      description: form.description || null,
      thumbnailUrl: form.thumbnailUrl || null,
      status: form.status,
      sortOrder: form.sortOrder,
    })
    form.title = ''
    form.description = ''
    form.thumbnailUrl = ''
    await load(true)
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    saving.value = false
  }
}

onMounted(() => load(true))
</script>

<template>
  <section>
    <div class="flex flex-wrap items-end justify-between gap-4">
      <div>
        <h1 class="text-3xl font-bold">Khóa học</h1>
        <p class="mt-1 text-slate-600">Danh sách dùng cursor/keyset, không dùng deep offset.</p>
      </div>
      <form class="flex gap-2" @submit.prevent="load(true)">
        <input
          v-model.trim="search"
          class="rounded-lg border px-3 py-2"
          maxlength="100"
          placeholder="Tìm khóa học"
        />
        <button class="rounded-lg bg-slate-900 px-4 py-2 text-white">Tìm</button>
      </form>
    </div>

    <ProblemAlert class="mt-5" :problem="problem" />
    <div class="mt-6 grid gap-6 lg:grid-cols-[22rem_1fr]">
      <form class="h-fit rounded-xl border bg-white p-5 shadow-sm" @submit.prevent="create">
        <h2 class="text-lg font-bold">Tạo khóa học</h2>
        <div class="mt-4 space-y-3">
          <input
            v-model.trim="form.title"
            required
            maxlength="200"
            class="w-full rounded-lg border px-3 py-2"
            placeholder="Tên khóa học"
          />
          <textarea
            v-model.trim="form.description"
            maxlength="4000"
            class="w-full rounded-lg border px-3 py-2"
            placeholder="Mô tả"
          />
          <input
            v-model.trim="form.thumbnailUrl"
            class="w-full rounded-lg border px-3 py-2"
            placeholder="https://... ảnh đại diện"
          />
          <div class="grid grid-cols-2 gap-3">
            <select v-model="form.status" class="rounded-lg border px-3 py-2">
              <option value="DRAFT">DRAFT</option>
              <option value="PUBLISHED">PUBLISHED</option>
            </select>
            <input
              v-model.number="form.sortOrder"
              min="0"
              type="number"
              class="rounded-lg border px-3 py-2"
            />
          </div>
          <button
            class="w-full rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white"
            :disabled="saving"
          >
            {{ saving ? 'Đang lưu…' : 'Tạo khóa học' }}
          </button>
        </div>
      </form>

      <div>
        <p v-if="loading && courses.length === 0" class="text-slate-500">Đang tải…</p>
        <p
          v-else-if="!loading && courses.length === 0"
          class="rounded-xl border bg-white p-8 text-center text-slate-500"
        >
          Chưa có khóa học.
        </p>
        <div class="grid gap-3">
          <RouterLink
            v-for="course in courses"
            :key="course.id"
            :to="`/admin/courses/${course.id}`"
            class="rounded-xl border bg-white p-5 shadow-sm hover:border-indigo-300"
          >
            <div class="flex justify-between gap-4">
              <div>
                <h2 class="font-bold">{{ course.title }}</h2>
                <p class="mt-1 text-sm text-slate-500">
                  {{ course.lessonCount }} bài học · {{ course.studentCount }} học viên
                </p>
              </div>
              <span class="text-xs font-bold text-indigo-700">{{ course.status }}</span>
            </div>
          </RouterLink>
        </div>
        <CursorLoadMore class="mt-5" :has-more="hasMore" :loading="loading" @load="load()" />
      </div>
    </div>
  </section>
</template>
