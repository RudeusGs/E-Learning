<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'
import * as courseApi from '@/modules/courses/course.api'
import type { StudentCourseDetail } from '@/modules/courses/models/StudentCourseDetail'

import type { LessonProgressDetail } from './models/LessonProgressDetail'
import type { StudentCourseProgress } from './models/StudentCourseProgress'
import * as progressApi from './progress.api'

type LessonProgressStatus = 'NOT_STARTED' | 'IN_PROGRESS' | 'COMPLETED'

interface TimelineLesson {
  id: number
  title: string
  sortOrder: number
  state: string
  canAccess: boolean
  status: LessonProgressStatus
  startedAtUtc: string | null
  completedAtUtc: string | null
}

const route = useRoute()

const course = ref<StudentCourseDetail | null>(null)
const progress = ref<StudentCourseProgress | null>(null)
const loading = ref(true)
const problem = ref<ProblemDetails | null>(null)

const courseId = computed(() => Number(route.params.courseId))

const timelineLessons = computed<TimelineLesson[]>(() => {
  if (!course.value || !progress.value) return []

  const progressByLessonId = new Map<number, LessonProgressDetail>(
    progress.value.lessons.map((item) => [item.lessonId, item]),
  )

  return course.value.lessons.map((summary) => {
    const detail = progressByLessonId.get(summary.id)
    const normalizedStatus = normalizeStatus(detail?.status, summary.state)

    return {
      id: summary.id,
      title: summary.title,
      sortOrder: summary.sortOrder,
      state: summary.state,
      canAccess: summary.canAccess,
      status: normalizedStatus,
      startedAtUtc: detail?.startedAtUtc ?? null,
      completedAtUtc: detail?.completedAtUtc ?? null,
    }
  })
})

const currentLesson = computed<TimelineLesson | null>(() => {
  const lessons = timelineLessons.value

  return (
    lessons.find((item) => item.status === 'IN_PROGRESS' && item.canAccess) ??
    lessons.find((item) => item.status !== 'COMPLETED' && item.canAccess) ??
    null
  )
})

const remainingLessons = computed(() => {
  if (!progress.value) return 0
  return Math.max(0, progress.value.totalLessons - progress.value.completedLessons)
})

const isCourseCompleted = computed(
  () =>
    Boolean(progress.value) &&
    progress.value!.totalLessons > 0 &&
    progress.value!.completedLessons >= progress.value!.totalLessons,
)

function normalizeStatus(status: string | undefined, state: string): LessonProgressStatus {
  if (status === 'COMPLETED' || state === 'COMPLETED') return 'COMPLETED'
  if (status === 'IN_PROGRESS') return 'IN_PROGRESS'
  return 'NOT_STARTED'
}

function statusLabel(item: TimelineLesson): string {
  if (item.status === 'COMPLETED') return 'Đã hoàn thành'
  if (item.status === 'IN_PROGRESS') return 'Đang học'
  if (item.canAccess) return 'Sẵn sàng'
  return 'Chưa mở khóa'
}

function actionLabel(item: TimelineLesson): string {
  if (item.status === 'COMPLETED') return 'Học lại'
  if (item.status === 'IN_PROGRESS') return 'Tiếp tục học'
  return 'Bắt đầu học'
}

function formatDate(value: string | null): string {
  if (!value) return ''

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''

  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date)
}

function progressWidth(value: number): string {
  return `${Math.max(0, Math.min(100, value))}%`
}

async function load(): Promise<void> {
  const requestedCourseId = courseId.value

  if (!Number.isFinite(requestedCourseId) || requestedCourseId <= 0) {
    course.value = null
    progress.value = null
    loading.value = false
    problem.value = {
      status: 400,
      code: 'INVALID_COURSE_ID',
      title: 'Khóa học không hợp lệ',
      detail: 'Mã khóa học không hợp lệ.',
    }
    return
  }

  loading.value = true
  problem.value = null
  course.value = null
  progress.value = null

  try {
    const [courseData, progressData] = await Promise.all([
      courseApi.getStudentCourse(requestedCourseId),
      progressApi.getStudentProgress(requestedCourseId),
    ])

    if (courseId.value !== requestedCourseId) return

    course.value = courseData
    progress.value = progressData
  } catch (error) {
    if (courseId.value === requestedCourseId) {
      problem.value = toProblem(error)
    }
  } finally {
    if (courseId.value === requestedCourseId) {
      loading.value = false
    }
  }
}

watch(
  () => route.params.courseId,
  () => {
    void load()
  },
  { immediate: true },
)
</script>

<template>
  <section class="mx-auto w-full max-w-[920px] pb-4">
    <!-- Loading -->
    <div v-if="loading" class="space-y-7">
      <div class="h-4 w-56 animate-pulse rounded bg-[#e9e2e1]" />

      <div class="h-[285px] animate-pulse rounded-2xl bg-[#eee8e7]" />

      <div>
        <div class="h-8 w-52 animate-pulse rounded bg-[#e8e1e0]" />
        <div class="mt-6 space-y-5">
          <div
            v-for="index in 5"
            :key="index"
            class="ml-4 h-32 animate-pulse rounded-2xl bg-[#eee8e7]"
          />
        </div>
      </div>
    </div>

    <template v-else>
      <ProblemAlert v-if="problem" :problem="problem" />

      <template v-if="course && progress">
        <!-- Back -->
        <RouterLink
          to="/student/courses"
          class="group inline-flex items-center gap-2 text-sm font-bold text-[#756765] transition hover:text-[#a0001c]"
        >
          <svg
            class="size-4 transition-transform group-hover:-translate-x-0.5"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            aria-hidden="true"
          >
            <path d="M19 12H5M10 7l-5 5 5 5" />
          </svg>
          Quay lại danh sách khóa học
        </RouterLink>

        <!-- Overall progress card -->
        <section
          class="relative mt-6 overflow-hidden rounded-2xl border border-[#e5dedd] bg-white p-6 shadow-[0_4px_18px_rgba(52,27,31,0.035)] sm:p-8"
        >
          <div
            class="academic-grid pointer-events-none absolute inset-0 opacity-35"
            aria-hidden="true"
          />

          <div
            class="bookmark-tab absolute right-7 top-0 z-10 flex h-16 w-11 items-start justify-center bg-[#c9152b] pt-3 text-white shadow-[0_5px_12px_rgba(160,0,28,0.16)] sm:right-9"
            aria-hidden="true"
          >
            <svg
              class="size-5"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="1.9"
            >
              <path d="M4 5.5A2.5 2.5 0 0 1 6.5 3H20v16H6.5A2.5 2.5 0 0 0 4 21.5v-16Z" />
              <path d="M4 18.5A2.5 2.5 0 0 1 6.5 16H20" />
            </svg>
          </div>

          <div class="relative pr-12 sm:pr-16">
            <p class="text-[10px] font-black uppercase tracking-[0.16em] text-[#a0001c]">
              Tiến độ khóa học
            </p>

            <h1
              class="mt-2 text-[31px] font-black leading-tight tracking-[-0.035em] text-[#1c1b1b] sm:text-[42px]"
            >
              {{ course.title }}
            </h1>

            <p class="mt-3 max-w-3xl text-sm leading-6 text-[#6b5d5c] sm:text-[15px]">
              {{
                course.description ||
                'Theo dõi quá trình hoàn thành các bài học Published trong khóa học.'
              }}
            </p>
          </div>

          <div class="relative mt-7 border-t border-[#eee7e6] pt-6">
            <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
              <div>
                <strong
                  class="text-[28px] font-black tracking-[-0.03em] text-[#a0001c] sm:text-[32px]"
                >
                  {{ progress.percentage }}% hoàn thành
                </strong>
                <p class="mt-1 text-xs font-semibold text-[#8d7c7a]">
                  {{
                    isCourseCompleted
                      ? 'Bạn đã hoàn thành toàn bộ lộ trình hiện tại.'
                      : currentLesson
                        ? `Bài tiếp theo: ${currentLesson.title}`
                        : 'Tiếp tục hoàn thành các bài học trong lộ trình.'
                  }}
                </p>
              </div>

              <div class="shrink-0 text-left sm:text-right">
                <p class="text-sm font-black text-[#4b403f]">
                  {{ progress.completedLessons }}
                  <span class="font-semibold text-[#9a8987]">/</span>
                  {{ progress.totalLessons }}
                  <span class="font-semibold text-[#756765]">bài học</span>
                </p>
                <p class="mt-1 text-[11px] font-semibold text-[#9a8987]">
                  {{ remainingLessons }} bài còn lại
                </p>
              </div>
            </div>

            <div
              class="mt-4 h-2.5 overflow-hidden rounded-full bg-[#e8e3e3]"
              role="progressbar"
              :aria-valuenow="progress.percentage"
              aria-valuemin="0"
              aria-valuemax="100"
              :aria-label="`Tiến độ khóa học ${progress.percentage}%`"
            >
              <div
                class="h-full rounded-full transition-[width] duration-500"
                :class="isCourseCompleted ? 'bg-[#16875b]' : 'bg-[#c9152b]'"
                :style="{ width: progressWidth(progress.percentage) }"
              />
            </div>

            <div class="mt-5 flex flex-wrap gap-2">
              <span
                class="inline-flex items-center gap-1.5 rounded-lg border border-[#b9dfcc] bg-[#f0faf5] px-2.5 py-1.5 text-[10px] font-bold text-[#16764f]"
              >
                <span class="size-1.5 rounded-full bg-current" />
                {{ progress.completedLessons }} đã hoàn thành
              </span>

              <span
                v-if="currentLesson"
                class="inline-flex items-center gap-1.5 rounded-lg border border-[#edc2c7] bg-[#fff3f4] px-2.5 py-1.5 text-[10px] font-bold text-[#a0001c]"
              >
                <span class="size-1.5 rounded-full bg-current" />
                {{
                  currentLesson.status === 'IN_PROGRESS' ? '1 đang học' : 'Bài kế tiếp đã sẵn sàng'
                }}
              </span>

              <span
                v-if="remainingLessons > 0"
                class="inline-flex items-center gap-1.5 rounded-lg border border-[#e1d9d8] bg-[#f7f4f3] px-2.5 py-1.5 text-[10px] font-bold text-[#756765]"
              >
                {{ remainingLessons }} chưa hoàn thành
              </span>
            </div>
          </div>
        </section>

        <!-- Learning path -->
        <section class="mt-10">
          <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-end">
            <div>
              <p class="text-[10px] font-black uppercase tracking-[0.16em] text-[#9b8583]">
                Learning path
              </p>
              <h2
                class="mt-1.5 text-[27px] font-black tracking-[-0.03em] text-[#302929] sm:text-[31px]"
              >
                Lộ trình học tập
              </h2>
              <p class="mt-2 text-sm leading-6 text-[#81716f]">
                Trạng thái và thời điểm bên dưới được lấy trực tiếp từ tiến độ của tài khoản hiện
                tại.
              </p>
            </div>

            <RouterLink
              v-if="currentLesson"
              :to="`/student/learn/${currentLesson.id}`"
              class="inline-flex h-10 shrink-0 items-center justify-center gap-2 self-start rounded-lg bg-[#c9152b] px-4 text-sm font-bold text-white shadow-[0_6px_16px_rgba(160,0,28,0.11)] transition hover:bg-[#a0001c] sm:self-auto"
            >
              {{ currentLesson.status === 'IN_PROGRESS' ? 'Tiếp tục học' : 'Học bài tiếp theo' }}
              <svg
                class="size-4"
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

          <!-- Empty -->
          <div
            v-if="timelineLessons.length === 0"
            class="mt-6 flex min-h-[300px] flex-col items-center justify-center rounded-2xl border border-dashed border-[#d9cecd] bg-white px-6 py-10 text-center"
          >
            <div
              class="grid size-14 place-items-center rounded-2xl border border-[#eadfdf] bg-[#faf6f6] text-[#a48f8d]"
            >
              <svg
                class="size-7"
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

            <h3 class="mt-4 text-lg font-black text-[#403735]">Chưa có bài học Published</h3>
            <p class="mt-2 max-w-md text-sm leading-6 text-[#81716f]">
              Tiến độ sẽ được tính khi khóa học có bài học đang ở trạng thái Published.
            </p>
          </div>

          <!-- Timeline -->
          <ol v-else class="relative mt-7 space-y-5 pl-3 sm:pl-4">
            <li
              v-for="(item, index) in timelineLessons"
              :key="item.id"
              class="group relative flex gap-5 sm:gap-6"
            >
              <div
                v-if="index < timelineLessons.length - 1"
                class="absolute bottom-[-20px] left-[11px] top-7 w-0.5 bg-[#e8e3e3] sm:left-[11px]"
                aria-hidden="true"
              />

              <!-- Timeline node -->
              <div class="relative z-10 mt-5 shrink-0">
                <span
                  class="grid size-6 place-items-center rounded-full"
                  :class="
                    item.status === 'COMPLETED'
                      ? 'bg-[#16875b] text-white'
                      : item.id === currentLesson?.id
                        ? 'bg-[#c9152b] text-white shadow-[0_0_0_5px_rgba(201,21,43,0.08)]'
                        : item.canAccess
                          ? 'border-2 border-[#c9152b] bg-white text-[#c9152b]'
                          : 'border-2 border-white bg-[#e8e3e3] text-[#8f817e]'
                  "
                >
                  <svg
                    v-if="item.status === 'COMPLETED'"
                    class="size-3.5"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2.8"
                    aria-hidden="true"
                  >
                    <path d="m6 12 4 4 8-8" />
                  </svg>

                  <span
                    v-else-if="item.id === currentLesson?.id"
                    class="size-2 rounded-full bg-white"
                  />

                  <svg
                    v-else-if="!item.canAccess"
                    class="size-3"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                    aria-hidden="true"
                  >
                    <rect x="6" y="10" width="12" height="9" rx="2" />
                    <path d="M8.5 10V7.5a3.5 3.5 0 0 1 7 0V10" />
                  </svg>

                  <span v-else class="size-2 rounded-full bg-[#c9152b]" />
                </span>
              </div>

              <!-- Accessible lesson -->
              <article
                class="min-w-0 flex-1 rounded-xl border p-4 transition sm:p-5"
                :class="
                  item.id === currentLesson?.id
                    ? 'border-[#d78791] bg-white shadow-[0_6px_24px_rgba(92,30,39,0.055)]'
                    : item.status === 'COMPLETED'
                      ? 'border-[#dfe5e1] bg-white hover:border-[#bdd9ca]'
                      : item.canAccess
                        ? 'border-[#e3d9d8] bg-white hover:border-[#d8afb3]'
                        : 'border-[#e6e0de] bg-[#f7f4f3] opacity-75'
                "
              >
                <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
                  <div class="min-w-0 flex-1">
                    <div class="flex flex-wrap items-center gap-2">
                      <span
                        class="text-[10px] font-black uppercase tracking-[0.13em]"
                        :class="item.id === currentLesson?.id ? 'text-[#a0001c]' : 'text-[#9a8987]'"
                      >
                        Bài {{ index + 1 }}
                      </span>

                      <span
                        class="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[9px] font-black uppercase tracking-wide"
                        :class="
                          item.status === 'COMPLETED'
                            ? 'border-[#b9dfcc] bg-[#eef9f3] text-[#16764f]'
                            : item.status === 'IN_PROGRESS'
                              ? 'border-[#edc2c7] bg-[#fff3f4] text-[#a0001c]'
                              : item.canAccess
                                ? 'border-[#e6c7ca] bg-[#fff9f9] text-[#9d2837]'
                                : 'border-[#ddd5d4] bg-[#eeebe9] text-[#766967]'
                        "
                      >
                        <span
                          v-if="item.status !== 'COMPLETED'"
                          class="size-1.5 rounded-full bg-current opacity-75"
                        />
                        <svg
                          v-else
                          class="size-3"
                          viewBox="0 0 24 24"
                          fill="none"
                          stroke="currentColor"
                          stroke-width="2.5"
                          aria-hidden="true"
                        >
                          <path d="m6 12 4 4 8-8" />
                        </svg>
                        {{ statusLabel(item) }}
                      </span>
                    </div>

                    <h3
                      class="mt-2 text-[16px] font-black leading-6 tracking-[-0.01em] sm:text-[18px]"
                      :class="
                        item.canAccess || item.status === 'COMPLETED'
                          ? 'text-[#342d2d]'
                          : 'text-[#756765]'
                      "
                    >
                      {{ item.title }}
                    </h3>

                    <div
                      class="mt-3 flex flex-wrap items-center gap-x-4 gap-y-2 text-[11px] font-semibold text-[#8d7c7a]"
                    >
                      <span>Thứ tự {{ item.sortOrder }}</span>

                      <span
                        v-if="item.completedAtUtc"
                        class="inline-flex items-center gap-1.5 text-[#527363]"
                      >
                        <svg
                          class="size-3.5"
                          viewBox="0 0 24 24"
                          fill="none"
                          stroke="currentColor"
                          stroke-width="1.8"
                          aria-hidden="true"
                        >
                          <path d="M7 3v3M17 3v3M4 8h16M5 5h14v15H5z" />
                          <path d="m8 14 2 2 5-5" />
                        </svg>
                        Hoàn thành {{ formatDate(item.completedAtUtc) }}
                      </span>

                      <span
                        v-else-if="item.startedAtUtc"
                        class="inline-flex items-center gap-1.5 text-[#9b5760]"
                      >
                        <svg
                          class="size-3.5"
                          viewBox="0 0 24 24"
                          fill="none"
                          stroke="currentColor"
                          stroke-width="1.8"
                          aria-hidden="true"
                        >
                          <circle cx="12" cy="12" r="9" />
                          <path d="M12 7v5l3 2" />
                        </svg>
                        Bắt đầu {{ formatDate(item.startedAtUtc) }}
                      </span>

                      <span v-else-if="!item.canAccess"> Hoàn thành bài trước để mở khóa </span>

                      <span v-else> Chưa có lịch sử bắt đầu </span>
                    </div>
                  </div>

                  <RouterLink
                    v-if="item.canAccess"
                    :to="`/student/learn/${item.id}`"
                    class="inline-flex h-9 shrink-0 items-center justify-center gap-1.5 self-start rounded-lg px-3.5 text-xs font-bold transition"
                    :class="
                      item.id === currentLesson?.id
                        ? 'bg-[#c9152b] text-white hover:bg-[#a0001c]'
                        : 'border border-[#ddd3d2] bg-[#faf8f7] text-[#665957] hover:border-[#d5a9ad] hover:bg-[#fff4f5] hover:text-[#a0001c]'
                    "
                  >
                    {{ actionLabel(item) }}
                    <svg
                      class="size-3.5"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="2"
                      aria-hidden="true"
                    >
                      <path d="M5 12h14M14 7l5 5-5 5" />
                    </svg>
                  </RouterLink>

                  <span
                    v-else
                    class="inline-flex h-9 shrink-0 items-center gap-1.5 self-start rounded-lg border border-[#ded6d4] bg-[#eeebe9] px-3 text-[10px] font-bold text-[#817572]"
                  >
                    <svg
                      class="size-3.5"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="1.9"
                      aria-hidden="true"
                    >
                      <rect x="6" y="10" width="12" height="9" rx="2" />
                      <path d="M8.5 10V7.5a3.5 3.5 0 0 1 7 0V10" />
                    </svg>
                    Đã khóa
                  </span>
                </div>
              </article>
            </li>
          </ol>
        </section>

        <!-- Footer link -->
        <div class="mt-10 flex justify-center border-t border-[#e4dcdb] pt-6">
          <RouterLink
            :to="`/student/courses/${course.id}`"
            class="inline-flex h-10 items-center gap-2 rounded-lg border border-[#d9cfce] bg-white px-4 text-sm font-bold text-[#665957] transition hover:border-[#d5a9ad] hover:bg-[#fff3f4] hover:text-[#a0001c]"
          >
            Xem tổng quan khóa học
            <svg
              class="size-4"
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
      </template>
    </template>
  </section>
</template>

<style scoped>
.academic-grid {
  background-image:
    linear-gradient(to right, rgb(127 16 32 / 0.03) 1px, transparent 1px),
    linear-gradient(to bottom, rgb(127 16 32 / 0.03) 1px, transparent 1px);
  background-size: 24px 24px;
}

.bookmark-tab {
  clip-path: polygon(0 0, 100% 0, 100% 100%, 50% 82%, 0 100%);
}
</style>
