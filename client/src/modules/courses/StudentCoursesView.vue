<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'
import { useAuthStore } from '@/modules/auth/auth.store'

import * as courseApi from './course.api'
import type { StudentCourse } from './models/StudentCourse'
import type { StudentCourseProgressFilter } from './models/StudentCourseProgressFilter'

type CourseFilter = 'ALL' | StudentCourseProgressFilter

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const courses = ref<StudentCourse[]>([])
const nextCursor = ref<string | null>(null)
const hasMore = ref(false)
const loading = ref(false)
const problem = ref<ProblemDetails | null>(null)
const activeFilter = ref<CourseFilter>('ALL')
const thumbnailErrors = ref<Set<number>>(new Set())

let abortController: AbortController | null = null

const filterOptions: Array<{ value: CourseFilter; label: string }> = [
  { value: 'ALL', label: 'Tất cả khóa học' },
  { value: 'IN_PROGRESS', label: 'Đang học' },
  { value: 'NOT_STARTED', label: 'Chưa bắt đầu' },
  { value: 'COMPLETED', label: 'Đã hoàn thành' },
]

const userName = computed(() => auth.user?.fullName?.trim() || 'bạn')

const emptyTitle = computed(() => {
  if (activeFilter.value === 'IN_PROGRESS') return 'Chưa có khóa học đang học'
  if (activeFilter.value === 'NOT_STARTED') return 'Không có khóa học chưa bắt đầu'
  if (activeFilter.value === 'COMPLETED') return 'Chưa có khóa học hoàn thành'
  return 'Bạn chưa được phân công khóa học'
})

const emptyDescription = computed(() => {
  if (activeFilter.value !== 'ALL') {
    return 'Thử chọn một trạng thái khác để xem các khóa học còn lại.'
  }

  return 'Khi quản trị viên phân công một khóa học Published cho bạn, khóa học sẽ xuất hiện tại đây.'
})

function normalizeFilter(value: unknown): CourseFilter {
  if (value === 'IN_PROGRESS' || value === 'NOT_STARTED' || value === 'COMPLETED') {
    return value
  }

  return 'ALL'
}

function isCanceled(error: unknown): boolean {
  return (
    (error as Error)?.name === 'CanceledError' ||
    (error as { code?: string })?.code === 'ERR_CANCELED'
  )
}

function isCompleted(course: StudentCourse): boolean {
  return course.totalLessons > 0 && course.completedLessons >= course.totalLessons
}

function isInProgress(course: StudentCourse): boolean {
  return course.completedLessons > 0 && !isCompleted(course)
}

function courseStateLabel(course: StudentCourse): string {
  if (isCompleted(course)) return 'Đã hoàn thành'
  if (isInProgress(course)) return 'Đang học'
  return 'Chưa bắt đầu'
}

function courseActionLabel(course: StudentCourse): string {
  if (isCompleted(course)) return 'Xem lại khóa học'
  if (isInProgress(course)) return 'Tiếp tục học'
  return 'Bắt đầu ngay'
}

function courseStatusClass(course: StudentCourse): string {
  if (isCompleted(course)) {
    return 'border-[#b9dfcc] bg-[#edf8f2] text-[#16764f]'
  }

  if (isInProgress(course)) {
    return 'border-[#edc2c7] bg-[#fff3f4] text-[#a0001c]'
  }

  return 'border-[#ddd5d4] bg-white/90 text-[#756765]'
}

function bookmarkClass(course: StudentCourse): string {
  if (isCompleted(course)) return 'bg-[#16875b]'
  if (isInProgress(course)) return 'bg-[#c9152b]'
  return 'bg-[#c9bfbd]'
}

function progressBarClass(course: StudentCourse): string {
  if (isCompleted(course)) return 'bg-[#16875b]'
  if (isInProgress(course)) return 'bg-[#c9152b]'
  return 'bg-[#cfc4c2]'
}

function progressWidth(value: number): string {
  return `${Math.max(0, Math.min(100, value))}%`
}

function markThumbnailError(courseId: number): void {
  const next = new Set(thumbnailErrors.value)
  next.add(courseId)
  thumbnailErrors.value = next
}

function thumbnailAvailable(course: StudentCourse): boolean {
  return Boolean(course.thumbnailUrl) && !thumbnailErrors.value.has(course.id)
}

async function load(reset = false): Promise<void> {
  if (loading.value && !reset) return

  abortController?.abort()
  abortController = new AbortController()
  const signal = abortController.signal

  loading.value = true
  problem.value = null

  if (reset) {
    courses.value = []
    nextCursor.value = null
    hasMore.value = false
    thumbnailErrors.value = new Set()
  }

  try {
    const page = await courseApi.getStudentCourses(
      reset ? null : nextCursor.value,
      activeFilter.value === 'ALL' ? null : activeFilter.value,
      signal,
      20,
    )

    if (signal.aborted) return

    courses.value = reset ? page.items : [...courses.value, ...page.items]
    nextCursor.value = page.nextCursor
    hasMore.value = page.hasMore
  } catch (error) {
    if (isCanceled(error)) return
    problem.value = toProblem(error)
  } finally {
    if (!signal.aborted) {
      loading.value = false
    }
  }
}

async function setFilter(filter: CourseFilter): Promise<void> {
  if (filter === activeFilter.value) return

  const nextQuery = { ...route.query }

  if (filter === 'ALL') {
    delete nextQuery.status
  } else {
    nextQuery.status = filter
  }

  await router.replace({ query: nextQuery })
}

watch(
  () => route.query.status,
  () => {
    activeFilter.value = normalizeFilter(route.query.status)
    void load(true)
  },
  { immediate: true },
)

onBeforeUnmount(() => {
  abortController?.abort()
})
</script>

<template>
  <section class="space-y-8">
    <!-- Hero -->
    <section
      class="student-hero relative overflow-hidden rounded-2xl border border-[#e6dedd] bg-[#eee9e8] px-6 py-8 sm:px-9 sm:py-10 lg:px-11 lg:py-11"
    >
      <div class="student-dot-pattern absolute inset-0 opacity-55" />
      <div
        class="pointer-events-none absolute right-0 top-0 h-full w-[42%] bg-gradient-to-l from-[#c9152b]/[0.07] to-transparent"
      />
      <div
        class="pointer-events-none absolute -right-14 -top-20 size-60 rounded-full border-[42px] border-[#a0001c]/[0.035]"
      />

      <div class="relative max-w-3xl">
        <p class="text-[10px] font-black uppercase tracking-[0.18em] text-[#a0001c]">
          Không gian học tập
        </p>
        <h1
          class="mt-2 text-[28px] font-black leading-tight tracking-[-0.03em] text-[#1c1b1b] sm:text-[34px]"
        >
          Chào {{ userName }},
        </h1>
        <p class="mt-3 max-w-2xl text-sm leading-6 text-[#6b5d5c] sm:text-base sm:leading-7">
          Tiếp tục hành trình học tập của bạn. Dưới đây là các khóa học đang được phân công và tiến
          độ hiện tại.
        </p>
      </div>
    </section>

    <ProblemAlert :problem="problem" />

    <!-- Filters -->
    <div class="border-b border-[#e2d9d8]">
      <div class="flex gap-1 overflow-x-auto">
        <button
          v-for="option in filterOptions"
          :key="option.value"
          type="button"
          class="relative h-11 shrink-0 px-3 text-sm font-bold transition sm:px-4"
          :class="
            activeFilter === option.value ? 'text-[#a0001c]' : 'text-[#756765] hover:text-[#a0001c]'
          "
          @click="setFilter(option.value)"
        >
          {{ option.label }}
          <span
            class="absolute inset-x-2 bottom-0 h-0.5 rounded-full transition"
            :class="activeFilter === option.value ? 'bg-[#c9152b]' : 'bg-transparent'"
          />
        </button>
      </div>
    </div>

    <!-- Initial skeleton -->
    <div v-if="loading && courses.length === 0" class="grid gap-6 md:grid-cols-2 xl:grid-cols-3">
      <article
        v-for="index in 6"
        :key="index"
        class="overflow-hidden rounded-2xl border border-[#e5dedd] bg-white"
      >
        <div class="aspect-video animate-pulse bg-[#eee8e7]" />
        <div class="space-y-3 p-5 sm:p-6">
          <div class="h-3 w-28 animate-pulse rounded bg-[#eee8e7]" />
          <div class="h-5 w-3/4 animate-pulse rounded bg-[#e8e1e0]" />
          <div class="h-4 w-full animate-pulse rounded bg-[#f0ebea]" />
          <div class="h-4 w-5/6 animate-pulse rounded bg-[#f0ebea]" />
          <div class="mt-5 h-9 w-32 animate-pulse rounded-lg bg-[#eee8e7]" />
        </div>
        <div class="h-2 animate-pulse bg-[#e8e1e0]" />
      </article>
    </div>

    <!-- Empty state -->
    <div
      v-else-if="courses.length === 0"
      class="flex min-h-[390px] flex-col items-center justify-center rounded-2xl border border-dashed border-[#d9cecd] bg-white/65 px-6 py-12 text-center"
    >
      <div
        class="grid size-16 place-items-center rounded-2xl border border-[#eadfdf] bg-[#faf6f6] text-[#a48f8d]"
      >
        <svg
          class="size-8"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.6"
          aria-hidden="true"
        >
          <path d="M4 5.5A2.5 2.5 0 0 1 6.5 3H20v16H6.5A2.5 2.5 0 0 0 4 21.5v-16Z" />
          <path d="M4 18.5A2.5 2.5 0 0 1 6.5 16H20" />
        </svg>
      </div>

      <h2 class="mt-5 text-lg font-black text-[#342d2d]">{{ emptyTitle }}</h2>
      <p class="mt-2 max-w-md text-sm leading-6 text-[#81716f]">
        {{ emptyDescription }}
      </p>

      <button
        v-if="activeFilter !== 'ALL'"
        type="button"
        class="mt-5 h-10 rounded-lg border border-[#d8a5ab] bg-white px-4 text-sm font-bold text-[#a0001c] transition hover:bg-[#fff4f5]"
        @click="setFilter('ALL')"
      >
        Xem tất cả khóa học
      </button>
    </div>

    <!-- Course grid -->
    <div v-else class="grid gap-6 md:grid-cols-2 xl:grid-cols-3">
      <article
        v-for="course in courses"
        :key="course.id"
        class="group relative flex min-h-full flex-col overflow-hidden rounded-2xl border border-[#e5dedd] bg-white shadow-[0_2px_12px_rgba(50,25,25,0.025)] transition duration-300 hover:-translate-y-0.5 hover:border-[#dbb8bb] hover:shadow-[0_14px_34px_rgba(67,27,33,0.08)]"
      >
        <!-- Bookmark -->
        <span
          class="bookmark-tab absolute right-5 top-0 z-20 h-9 w-6 shadow-sm"
          :class="bookmarkClass(course)"
          aria-hidden="true"
        />

        <!-- Thumbnail -->
        <RouterLink
          :to="`/student/courses/${course.id}`"
          class="relative block aspect-video overflow-hidden bg-[#f0ebea]"
        >
          <img
            v-if="thumbnailAvailable(course)"
            :src="course.thumbnailUrl ?? ''"
            :alt="`Ảnh đại diện khóa học ${course.title}`"
            class="absolute inset-0 size-full object-cover transition duration-500 group-hover:scale-[1.035]"
            loading="lazy"
            @error="markThumbnailError(course.id)"
          />

          <div
            v-else
            class="course-thumbnail-grid absolute inset-0 flex items-center justify-center bg-[#f5f1f0]"
          >
            <div
              class="grid size-16 place-items-center rounded-2xl border border-[#e4d7d8] bg-white/85 text-[#a0001c] shadow-sm"
            >
              <svg
                class="size-8"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="1.5"
                aria-hidden="true"
              >
                <path d="M4 5.5A2.5 2.5 0 0 1 6.5 3H20v16H6.5A2.5 2.5 0 0 0 4 21.5v-16Z" />
                <path d="M4 18.5A2.5 2.5 0 0 1 6.5 16H20" />
                <path d="M9 7h7M9 11h5" />
              </svg>
            </div>
          </div>

          <span
            class="absolute left-3 top-3 inline-flex items-center gap-1.5 rounded-lg border px-2.5 py-1.5 text-[10px] font-black backdrop-blur"
            :class="courseStatusClass(course)"
          >
            <span
              v-if="isCompleted(course)"
              class="grid size-3.5 place-items-center rounded-full bg-[#16875b] text-white"
            >
              <svg
                class="size-2.5"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="3"
                aria-hidden="true"
              >
                <path d="m6 12 4 4 8-8" />
              </svg>
            </span>
            <span v-else class="size-1.5 rounded-full bg-current opacity-80" />
            {{ courseStateLabel(course) }}
          </span>
        </RouterLink>

        <!-- Body -->
        <div class="flex flex-1 flex-col p-5 sm:p-6">
          <div class="mb-2 flex items-center justify-between gap-3">
            <span class="text-[10px] font-black uppercase tracking-[0.13em] text-[#a0001c]">
              {{ course.completedLessons }}/{{ course.totalLessons }} bài học
            </span>
            <span class="text-xs font-black tabular-nums text-[#5f5351]">
              {{ course.percentage }}%
            </span>
          </div>

          <RouterLink :to="`/student/courses/${course.id}`" class="block">
            <h2
              class="line-clamp-2 text-[18px] font-black leading-7 tracking-[-0.02em] text-[#302929] transition group-hover:text-[#a0001c]"
            >
              {{ course.title }}
            </h2>
          </RouterLink>

          <p class="mt-2 line-clamp-3 text-sm leading-6 text-[#756765]">
            {{
              course.description ||
              'Mở khóa từng bài học theo lộ trình và theo dõi tiến độ của bạn trong khóa học này.'
            }}
          </p>

          <div class="mt-auto pt-5">
            <RouterLink
              :to="`/student/courses/${course.id}`"
              class="group/action inline-flex h-9 items-center gap-2 rounded-lg text-sm font-black transition"
              :class="
                isCompleted(course) ? 'text-[#3e4f47] hover:text-[#a0001c]' : 'text-[#a0001c]'
              "
            >
              {{ courseActionLabel(course) }}

              <svg
                class="size-4 transition-transform group-hover/action:translate-x-0.5"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                aria-hidden="true"
              >
                <path d="M5 12h14M14 7l5 5-5 5" />
              </svg>
            </RouterLink>
          </div>
        </div>

        <!-- Progress footer -->
        <div class="h-2 w-full overflow-hidden bg-[#eee8e7]">
          <div
            class="h-full transition-[width] duration-500"
            :class="progressBarClass(course)"
            :style="{ width: progressWidth(course.percentage) }"
          />
        </div>
      </article>
    </div>

    <!-- Cursor pagination -->
    <div
      v-if="courses.length > 0"
      class="flex flex-col items-center justify-between gap-3 border-t border-[#e4dcdb] pt-5 sm:flex-row"
    >
      <p class="text-xs font-semibold text-[#958582]">
        Đang hiển thị {{ courses.length }} khóa học
      </p>

      <button
        v-if="hasMore"
        type="button"
        :disabled="loading"
        class="inline-flex h-10 items-center gap-2 rounded-lg border border-[#d9c4c5] bg-white px-4 text-xs font-bold text-[#a0001c] transition hover:bg-[#fff4f5] disabled:cursor-not-allowed disabled:opacity-55"
        @click="load(false)"
      >
        <svg
          v-if="loading"
          class="size-4 animate-spin"
          viewBox="0 0 24 24"
          fill="none"
          aria-hidden="true"
        >
          <circle class="opacity-30" cx="12" cy="12" r="9" stroke="currentColor" stroke-width="3" />
          <path
            d="M21 12a9 9 0 0 0-9-9"
            stroke="currentColor"
            stroke-width="3"
            stroke-linecap="round"
          />
        </svg>
        {{ loading ? 'Đang tải…' : 'Tải thêm khóa học' }}
      </button>

      <span v-else class="text-xs font-semibold text-[#a39492]"> Đã hiển thị toàn bộ kết quả </span>
    </div>
  </section>
</template>

<style scoped>
.student-dot-pattern {
  background-image: radial-gradient(circle at 2px 2px, rgb(160 0 28 / 0.11) 1.2px, transparent 0);
  background-size: 24px 24px;
}

.course-thumbnail-grid {
  background-image:
    linear-gradient(to right, rgb(127 16 32 / 0.045) 1px, transparent 1px),
    linear-gradient(to bottom, rgb(127 16 32 / 0.045) 1px, transparent 1px);
  background-size: 22px 22px;
}

.bookmark-tab {
  clip-path: polygon(0 0, 100% 0, 100% 100%, 50% 78%, 0 100%);
}
</style>
