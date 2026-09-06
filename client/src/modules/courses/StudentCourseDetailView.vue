<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'

import * as courseApi from './course.api'
import type { StudentCourseDetail } from './models/StudentCourseDetail'
import type { StudentLessonSummary } from './models/StudentLessonSummary'

const route = useRoute()

const course = ref<StudentCourseDetail | null>(null)
const problem = ref<ProblemDetails | null>(null)
const loading = ref(true)

const courseId = computed(() => Number(route.params.courseId))

const continueLesson = computed<StudentLessonSummary | null>(() => {
  if (!course.value) return null

  return (
    course.value.lessons.find(
      (lesson) => lesson.canAccess && lesson.state !== 'COMPLETED',
    ) ?? null
  )
})

const remainingLessons = computed(() => {
  if (!course.value) return 0
  return Math.max(0, course.value.totalLessons - course.value.completedLessons)
})

const isCompleted = computed(
  () =>
    Boolean(course.value) &&
    course.value!.totalLessons > 0 &&
    course.value!.completedLessons >= course.value!.totalLessons,
)

const progressDashOffset = computed(() => {
  const circumference = 282.743
  const percentage = Math.max(0, Math.min(100, course.value?.percentage ?? 0))
  return circumference * (1 - percentage / 100)
})

function lessonLabel(index: number): string {
  return `Bài ${index + 1}`
}

function lessonStateLabel(lesson: StudentLessonSummary): string {
  if (lesson.state === 'COMPLETED') return 'Đã hoàn thành'
  if (lesson.canAccess) return 'Sẵn sàng để học'
  return 'Chưa mở khóa'
}

async function load(): Promise<void> {
  if (!Number.isFinite(courseId.value) || courseId.value <= 0) {
    problem.value = {
      status: 400,
      code: 'INVALID_COURSE_ID',
      title: 'Khóa học không hợp lệ',
      detail: 'Mã khóa học không hợp lệ.',
    }
    loading.value = false
    course.value = null
    return
  }

  const requestedCourseId = courseId.value
  loading.value = true
  problem.value = null
  course.value = null

  try {
    const loadedCourse = await courseApi.getStudentCourse(requestedCourseId)

    if (courseId.value === requestedCourseId) {
      course.value = loadedCourse
    }
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
)

onMounted(load)
</script>

<template>
  <section class="-mx-4 -my-7 sm:-mx-6 sm:-my-10">
    <!-- Loading -->
    <div v-if="loading" class="mx-auto max-w-7xl px-4 py-8 sm:px-6 sm:py-10">
      <div class="h-4 w-52 animate-pulse rounded bg-[#e9e2e1]" />

      <div class="mt-5 h-[330px] animate-pulse rounded-2xl bg-[#7f1020]/15" />

      <div class="mt-8 grid gap-6 lg:grid-cols-[minmax(0,2fr)_340px]">
        <div class="space-y-5">
          <div class="h-20 animate-pulse rounded-2xl bg-[#eee8e7]" />
          <div class="h-20 animate-pulse rounded-2xl bg-[#eee8e7]" />
          <div class="h-20 animate-pulse rounded-2xl bg-[#eee8e7]" />
          <div class="h-20 animate-pulse rounded-2xl bg-[#eee8e7]" />
        </div>

        <div class="space-y-5">
          <div class="h-64 animate-pulse rounded-2xl bg-[#eee8e7]" />
          <div class="h-52 animate-pulse rounded-2xl bg-[#eee8e7]" />
        </div>
      </div>
    </div>

    <template v-else>
      <div v-if="problem" class="mx-auto max-w-7xl px-4 py-8 sm:px-6 sm:py-10">
        <ProblemAlert :problem="problem" />

        <RouterLink
          to="/student/courses"
          class="mt-5 inline-flex h-10 items-center gap-2 rounded-lg border border-[#ddd4d3] bg-white px-4 text-sm font-bold text-[#6b5d5c] transition hover:border-[#d5a9ad] hover:bg-[#fff3f4] hover:text-[#a0001c]"
        >
          <svg
            class="size-4"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            aria-hidden="true"
          >
            <path d="m15 18-6-6 6-6" />
          </svg>
          Quay lại khóa học của tôi
        </RouterLink>
      </div>

      <template v-if="course">
        <!-- Hero -->
        <section
          class="course-hero relative overflow-hidden bg-[#7f1020] text-white"
        >
          <div class="academic-grid absolute inset-0 opacity-35" />
          <div
            class="pointer-events-none absolute -right-28 -top-32 size-[430px] rounded-full border-[72px] border-white/[0.035]"
          />
          <div
            class="pointer-events-none absolute -bottom-40 right-[18%] size-[330px] rounded-full bg-[#c9152b]/25 blur-3xl"
          />

          <div class="relative mx-auto max-w-7xl px-4 pb-12 pt-8 sm:px-6 sm:pb-14 sm:pt-9">
            <RouterLink
              to="/student/courses"
              class="group inline-flex items-center gap-2 text-sm font-bold text-white/70 transition hover:text-white"
            >
              <svg
                class="size-4 transition-transform group-hover:-translate-x-0.5"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                aria-hidden="true"
              >
                <path d="m15 18-6-6 6-6" />
              </svg>
              Quay lại danh sách khóa học
            </RouterLink>

            <div
              class="mt-7 grid gap-8 lg:grid-cols-[minmax(0,1fr)_270px] lg:items-center lg:gap-12"
            >
              <div class="max-w-3xl">
                <span
                  class="inline-flex items-center gap-2 rounded-lg border border-white/15 bg-white/[0.08] px-3 py-1.5 text-[11px] font-bold uppercase tracking-[0.13em] text-white/75 backdrop-blur"
                >
                  <svg
                    class="size-4"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="1.8"
                    aria-hidden="true"
                  >
                    <path d="M4 5.5A2.5 2.5 0 0 1 6.5 3H20v16H6.5A2.5 2.5 0 0 0 4 21.5v-16Z" />
                    <path d="M4 18.5A2.5 2.5 0 0 1 6.5 16H20" />
                  </svg>
                  Khóa học của tôi
                </span>

                <h1
                  class="mt-4 text-[36px] font-black leading-[1.12] tracking-[-0.035em] text-white sm:text-[44px] lg:text-[50px]"
                >
                  {{ course.title }}
                </h1>

                <p class="mt-4 max-w-3xl text-[15px] leading-7 text-white/72 sm:text-base">
                  {{
                    course.description ||
                    'Hoàn thành từng bài học theo đúng thứ tự để mở khóa toàn bộ lộ trình.'
                  }}
                </p>

                <div class="mt-7 flex flex-wrap items-center gap-3">
                  <RouterLink
                    v-if="continueLesson"
                    :to="`/student/learn/${continueLesson.id}`"
                    class="inline-flex h-11 items-center justify-center gap-2 rounded-lg bg-white px-5 text-sm font-black text-[#7f1020] shadow-[0_8px_22px_rgba(35,8,13,0.18)] transition hover:bg-[#fff0f1]"
                  >
                    <svg
                      class="size-4.5"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="2"
                      aria-hidden="true"
                    >
                      <path d="m9 7 8 5-8 5Z" />
                    </svg>
                    {{ course.completedLessons > 0 ? 'Tiếp tục học' : 'Bắt đầu học' }}
                  </RouterLink>

                  <RouterLink
                    v-else-if="isCompleted"
                    :to="`/student/courses/${course.id}/progress`"
                    class="inline-flex h-11 items-center justify-center gap-2 rounded-lg bg-white px-5 text-sm font-black text-[#7f1020] shadow-[0_8px_22px_rgba(35,8,13,0.18)] transition hover:bg-[#fff0f1]"
                  >
                    <svg
                      class="size-4.5"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="2"
                      aria-hidden="true"
                    >
                      <path d="M4 19V9M10 19V5M16 19v-7M22 19H2" />
                    </svg>
                    Xem kết quả
                  </RouterLink>

                  <RouterLink
                    :to="`/student/courses/${course.id}/progress`"
                    class="inline-flex h-11 items-center justify-center gap-2 rounded-lg border border-white/20 bg-white/[0.07] px-4 text-sm font-bold text-white/85 backdrop-blur transition hover:bg-white/[0.12] hover:text-white"
                  >
                    Chi tiết tiến độ
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
              </div>

              <!-- Progress ring -->
              <div
                class="justify-self-start rounded-2xl border border-white/15 bg-white/[0.075] p-5 backdrop-blur-md lg:justify-self-end"
              >
                <div class="relative mx-auto size-36">
                  <svg
                    class="size-full -rotate-90"
                    viewBox="0 0 100 100"
                    role="img"
                    :aria-label="`Tiến độ khóa học ${course.percentage}%`"
                  >
                    <circle
                      cx="50"
                      cy="50"
                      r="45"
                      fill="none"
                      stroke="rgba(255,255,255,0.14)"
                      stroke-width="7"
                    />
                    <circle
                      cx="50"
                      cy="50"
                      r="45"
                      fill="none"
                      stroke="#92f7c2"
                      stroke-width="7"
                      stroke-linecap="round"
                      stroke-dasharray="282.743"
                      :stroke-dashoffset="progressDashOffset"
                      class="transition-[stroke-dashoffset] duration-700"
                    />
                  </svg>

                  <div class="absolute inset-0 grid place-items-center text-center">
                    <div>
                      <strong class="block text-[30px] font-black leading-none">
                        {{ course.percentage }}%
                      </strong>
                      <span class="mt-1.5 block text-[10px] font-bold uppercase tracking-[0.12em] text-white/55">
                        Hoàn thành
                      </span>
                    </div>
                  </div>
                </div>

                <p class="mt-4 text-center text-xs font-semibold text-white/65">
                  {{ course.completedLessons }}/{{ course.totalLessons }} bài học đã hoàn thành
                </p>
              </div>
            </div>
          </div>
        </section>

        <!-- Content -->
        <div
          class="mx-auto grid max-w-7xl gap-7 px-4 py-9 sm:px-6 sm:py-11 lg:grid-cols-[minmax(0,2fr)_340px] lg:items-start"
        >
          <!-- Learning path -->
          <section
            class="rounded-2xl border border-[#e6dedd] bg-white p-5 shadow-[0_2px_12px_rgba(50,25,25,0.025)] sm:p-7"
          >
            <div
              class="flex flex-col justify-between gap-4 border-b border-[#eee7e6] pb-5 sm:flex-row sm:items-end"
            >
              <div>
                <p class="text-[10px] font-black uppercase tracking-[0.16em] text-[#a0001c]">
                  Nội dung khóa học
                </p>
                <h2 class="mt-1.5 text-2xl font-black tracking-[-0.025em] text-[#302929]">
                  Lộ trình bài học
                </h2>
                <p class="mt-2 text-sm leading-6 text-[#81716f]">
                  Hoàn thành bài học đứng trước để mở khóa bài học kế tiếp.
                </p>
              </div>

              <span
                class="inline-flex h-8 items-center rounded-lg border border-[#e2d9d8] bg-[#faf7f6] px-3 text-xs font-bold text-[#756765]"
              >
                {{ course.totalLessons }} bài học
              </span>
            </div>

            <!-- Empty -->
            <div
              v-if="course.lessons.length === 0"
              class="flex min-h-[330px] flex-col items-center justify-center px-6 py-10 text-center"
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

              <h3 class="mt-4 text-lg font-black text-[#403735]">
                Khóa học chưa có bài học Published
              </h3>
              <p class="mt-2 max-w-md text-sm leading-6 text-[#81716f]">
                Khi nội dung được xuất bản, lộ trình học sẽ xuất hiện tại đây.
              </p>
            </div>

            <!-- Timeline -->
            <div v-else class="relative mt-7">
              <div
                class="absolute bottom-5 left-[13px] top-5 w-px bg-[#e1d9d8] sm:left-[15px]"
                aria-hidden="true"
              />

              <div
                class="absolute left-[13px] top-5 w-px bg-[#c9152b] transition-[height] duration-700 sm:left-[15px]"
                :style="{
                  height: `calc((100% - 2.5rem) * ${Math.max(
                    0,
                    Math.min(100, course.percentage),
                  ) / 100})`,
                }"
                aria-hidden="true"
              />

              <ol class="space-y-4">
                <li
                  v-for="(lesson, index) in course.lessons"
                  :key="lesson.id"
                  class="relative pl-11 sm:pl-12"
                >
                  <!-- Node -->
                  <span
                    class="absolute left-0 top-5 z-10 grid size-7 place-items-center rounded-full border-[3px] bg-white sm:size-8"
                    :class="
                      lesson.state === 'COMPLETED'
                        ? 'border-[#16875b] bg-[#16875b] text-white'
                        : lesson.canAccess
                          ? 'border-[#c9152b] text-[#c9152b] shadow-[0_0_0_5px_rgba(201,21,43,0.08)]'
                          : 'border-[#cfc4c2] bg-[#f5f2f1] text-[#9a8c89]'
                    "
                  >
                    <svg
                      v-if="lesson.state === 'COMPLETED'"
                      class="size-3.5"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="2.6"
                      aria-hidden="true"
                    >
                      <path d="m6 12 4 4 8-8" />
                    </svg>

                    <span
                      v-else-if="lesson.canAccess"
                      class="size-2 rounded-full bg-current"
                    />

                    <svg
                      v-else
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
                  </span>

                  <!-- Accessible -->
                  <RouterLink
                    v-if="lesson.canAccess"
                    :to="`/student/learn/${lesson.id}`"
                    class="group block rounded-xl border p-4 transition sm:p-5"
                    :class="
                      lesson.id === continueLesson?.id
                        ? 'border-[#e1aeb4] bg-[#fff7f7] shadow-[0_7px_24px_rgba(91,29,39,0.055)]'
                        : 'border-[#e6dfde] bg-white hover:border-[#d9afb3] hover:bg-[#fffafa]'
                    "
                  >
                    <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
                      <div class="min-w-0">
                        <div class="flex flex-wrap items-center gap-2">
                          <span
                            class="text-[10px] font-black uppercase tracking-[0.13em]"
                            :class="
                              lesson.id === continueLesson?.id
                                ? 'text-[#a0001c]'
                                : 'text-[#998784]'
                            "
                          >
                            {{ lessonLabel(index) }}
                          </span>

                          <span
                            v-if="lesson.id === continueLesson?.id"
                            class="rounded-md border border-[#e7b8bd] bg-[#fff0f1] px-2 py-0.5 text-[9px] font-black uppercase tracking-wide text-[#a0001c]"
                          >
                            Hiện tại
                          </span>

                          <span
                            v-else-if="lesson.state === 'COMPLETED'"
                            class="rounded-md border border-[#b9dfcc] bg-[#eef9f3] px-2 py-0.5 text-[9px] font-black uppercase tracking-wide text-[#16764f]"
                          >
                            Đã xong
                          </span>
                        </div>

                        <h3
                          class="mt-1.5 text-[15px] font-black leading-6 text-[#342d2d] transition group-hover:text-[#a0001c] sm:text-base"
                          :class="lesson.state === 'COMPLETED' ? 'text-[#645856]' : ''"
                        >
                          {{ lesson.title }}
                        </h3>

                        <div class="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-[11px] font-semibold text-[#958582]">
                          <span>{{ lessonStateLabel(lesson) }}</span>
                          <span>Thứ tự {{ lesson.sortOrder }}</span>
                        </div>
                      </div>

                      <span
                        class="inline-flex h-9 shrink-0 items-center gap-1.5 self-start rounded-lg px-3 text-xs font-bold transition"
                        :class="
                          lesson.id === continueLesson?.id
                            ? 'bg-[#c9152b] text-white group-hover:bg-[#a0001c]'
                            : 'border border-[#e0d7d6] bg-[#faf8f7] text-[#665957] group-hover:border-[#d7a8ad] group-hover:text-[#a0001c]'
                        "
                      >
                        {{
                          lesson.state === 'COMPLETED'
                            ? 'Học lại'
                            : course.completedLessons === 0
                              ? 'Bắt đầu'
                              : 'Học ngay'
                        }}

                        <svg
                          class="size-3.5 transition-transform group-hover:translate-x-0.5"
                          viewBox="0 0 24 24"
                          fill="none"
                          stroke="currentColor"
                          stroke-width="2"
                          aria-hidden="true"
                        >
                          <path d="M5 12h14M14 7l5 5-5 5" />
                        </svg>
                      </span>
                    </div>
                  </RouterLink>

                  <!-- Locked -->
                  <div
                    v-else
                    class="rounded-xl border border-[#e4dedc] bg-[#f7f4f3] p-4 text-[#837572] sm:p-5"
                    data-testid="locked-lesson"
                  >
                    <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
                      <div class="min-w-0">
                        <span class="text-[10px] font-black uppercase tracking-[0.13em] text-[#a0918f]">
                          {{ lessonLabel(index) }}
                        </span>

                        <h3 class="mt-1.5 text-[15px] font-bold leading-6 text-[#746764] sm:text-base">
                          {{ lesson.title }}
                        </h3>

                        <p class="mt-2 text-[11px] font-semibold leading-5 text-[#9a8c89]">
                          Hoàn thành bài học ngay trước để mở khóa nội dung này.
                        </p>
                      </div>

                      <span
                        class="inline-flex h-8 shrink-0 items-center gap-1.5 self-start rounded-lg border border-[#ded6d4] bg-[#eeebe9] px-2.5 text-[10px] font-bold text-[#817572]"
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
                  </div>
                </li>
              </ol>
            </div>
          </section>

          <!-- Right column -->
          <aside class="space-y-5 lg:sticky lg:top-24">
            <section
              class="rounded-2xl border border-[#e5dedd] bg-white p-5 shadow-[0_5px_22px_rgba(50,25,25,0.035)] sm:p-6"
            >
              <p class="text-[10px] font-black uppercase tracking-[0.15em] text-[#9b8583]">
                Tiến độ của bạn
              </p>

              <div class="mt-4 flex items-end justify-between gap-4">
                <div>
                  <strong class="text-[34px] font-black tracking-[-0.04em] text-[#302929]">
                    {{ course.percentage }}%
                  </strong>
                  <p class="mt-1 text-xs font-semibold text-[#8d7c7a]">
                    {{ isCompleted ? 'Đã hoàn thành khóa học' : 'Đang trong lộ trình' }}
                  </p>
                </div>

                <span
                  class="grid size-11 place-items-center rounded-xl"
                  :class="
                    isCompleted
                      ? 'bg-[#e8f7ef] text-[#16875b]'
                      : 'bg-[#fff0f1] text-[#c9152b]'
                  "
                >
                  <svg
                    class="size-5"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="1.9"
                    aria-hidden="true"
                  >
                    <path d="M4 19V9M10 19V5M16 19v-7M22 19H2" />
                  </svg>
                </span>
              </div>

              <div class="mt-5 h-2.5 overflow-hidden rounded-full bg-[#eee8e7]">
                <div
                  class="h-full rounded-full transition-[width] duration-700"
                  :class="isCompleted ? 'bg-[#16875b]' : 'bg-[#c9152b]'"
                  :style="{ width: `${Math.max(0, Math.min(100, course.percentage))}%` }"
                />
              </div>

              <dl class="mt-5 grid grid-cols-3 gap-2">
                <div class="rounded-xl bg-[#faf7f6] px-2 py-3 text-center">
                  <dt class="text-[9px] font-black uppercase tracking-wide text-[#9b8987]">
                    Tổng
                  </dt>
                  <dd class="mt-1 text-lg font-black text-[#403735]">
                    {{ course.totalLessons }}
                  </dd>
                </div>

                <div class="rounded-xl bg-[#f1faf5] px-2 py-3 text-center">
                  <dt class="text-[9px] font-black uppercase tracking-wide text-[#688374]">
                    Đã xong
                  </dt>
                  <dd class="mt-1 text-lg font-black text-[#16764f]">
                    {{ course.completedLessons }}
                  </dd>
                </div>

                <div class="rounded-xl bg-[#fff5f5] px-2 py-3 text-center">
                  <dt class="text-[9px] font-black uppercase tracking-wide text-[#a78387]">
                    Còn lại
                  </dt>
                  <dd class="mt-1 text-lg font-black text-[#a0001c]">
                    {{ remainingLessons }}
                  </dd>
                </div>
              </dl>

              <RouterLink
                :to="`/student/courses/${course.id}/progress`"
                class="mt-5 inline-flex h-10 w-full items-center justify-center gap-2 rounded-lg border border-[#d9cfce] bg-[#faf8f7] px-4 text-xs font-bold text-[#665957] transition hover:border-[#d5a9ad] hover:bg-[#fff3f4] hover:text-[#a0001c]"
              >
                Xem chi tiết tiến độ
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
            </section>

            <section
              class="rounded-2xl border border-[#e5dedd] bg-white p-5 sm:p-6"
            >
              <h2 class="text-sm font-black text-[#403735]">Cách mở khóa bài học</h2>

              <div class="mt-4 space-y-4">
                <div class="flex gap-3">
                  <span
                    class="grid size-8 shrink-0 place-items-center rounded-lg bg-[#fff0f1] text-xs font-black text-[#a0001c]"
                  >
                    1
                  </span>
                  <p class="pt-1 text-xs leading-5 text-[#756765]">
                    Bài Published đầu tiên luôn có thể truy cập.
                  </p>
                </div>

                <div class="flex gap-3">
                  <span
                    class="grid size-8 shrink-0 place-items-center rounded-lg bg-[#fff0f1] text-xs font-black text-[#a0001c]"
                  >
                    2
                  </span>
                  <p class="pt-1 text-xs leading-5 text-[#756765]">
                    Hoàn thành bài hiện tại để mở bài Published đứng ngay sau.
                  </p>
                </div>

                <div class="flex gap-3">
                  <span
                    class="grid size-8 shrink-0 place-items-center rounded-lg bg-[#f0f7f3] text-[#16764f]"
                  >
                    <svg
                      class="size-4"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="2"
                      aria-hidden="true"
                    >
                      <path d="m6 12 4 4 8-8" />
                    </svg>
                  </span>
                  <p class="pt-1 text-xs leading-5 text-[#756765]">
                    Tiến độ chỉ tính những bài học đang ở trạng thái Published.
                  </p>
                </div>
              </div>
            </section>

            <section
              class="academic-light-grid relative overflow-hidden rounded-2xl border border-[#e5dedd] bg-[#faf8f7] p-5 sm:p-6"
            >
              <div class="absolute inset-0 bg-gradient-to-b from-transparent to-[#faf8f7]/95" />
              <div class="relative">
                <span
                  class="grid size-10 place-items-center rounded-xl border border-[#ead9da] bg-white text-[#a0001c]"
                >
                  <svg
                    class="size-5"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="1.8"
                    aria-hidden="true"
                  >
                    <path d="M4 5.5A2.5 2.5 0 0 1 6.5 3H20v16H6.5A2.5 2.5 0 0 0 4 21.5v-16Z" />
                    <path d="M4 18.5A2.5 2.5 0 0 1 6.5 16H20" />
                  </svg>
                </span>

                <p class="mt-4 text-sm font-black text-[#403735]">
                  E Leaning MindX
                </p>
                <p class="mt-1.5 text-xs leading-5 text-[#81716f]">
                  Lộ trình trên được lấy trực tiếp từ dữ liệu enrollment, lesson và tiến độ của tài khoản đang đăng nhập.
                </p>
              </div>
            </section>
          </aside>
        </div>
      </template>
    </template>
  </section>
</template>

<style scoped>
.course-hero {
  box-shadow: inset 0 -1px 0 rgb(255 255 255 / 0.06);
}

.academic-grid {
  background-image:
    linear-gradient(to right, rgb(255 255 255 / 0.045) 1px, transparent 1px),
    linear-gradient(to bottom, rgb(255 255 255 / 0.045) 1px, transparent 1px);
  background-size: 32px 32px;
}

.academic-light-grid {
  background-image:
    linear-gradient(to right, rgb(127 16 32 / 0.045) 1px, transparent 1px),
    linear-gradient(to bottom, rgb(127 16 32 / 0.045) 1px, transparent 1px);
  background-size: 22px 22px;
}
</style>
