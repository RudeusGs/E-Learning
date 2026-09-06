<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'
import * as lessonApi from '@/modules/lessons/lesson.api'
import type { AdminLessonListItem } from '@/modules/lessons/models/AdminLessonListItem'
import type { LessonStatus } from '@/types/LessonStatus'
import type { CourseStatus } from '@/types/CourseStatus'

import * as courseApi from './course.api'
import type { AdminCourseDetail } from './models/AdminCourseDetail'

type EditableCourseStatus = Exclude<CourseStatus, 'ARCHIVED'>
type EditableLessonStatus = Exclude<LessonStatus, 'ARCHIVED'>

const route = useRoute()
const router = useRouter()

const courseId = computed(() => Number(route.params.courseId))

const course = ref<AdminCourseDetail | null>(null)
const lessons = ref<AdminLessonListItem[]>([])
const loading = ref(true)
const problem = ref<ProblemDetails | null>(null)

const archiving = ref(false)
const savingCourse = ref(false)
const creatingLesson = ref(false)
const thumbnailFailed = ref(false)

const editCourseOpen = ref(false)
const createLessonOpen = ref(false)

const courseForm = reactive({
  title: '',
  description: '',
  thumbnailUrl: '',
  status: 'DRAFT' as EditableCourseStatus,
  sortOrder: 0,
})

const lessonForm = reactive({
  title: '',
  description: '',
  videoUrl: '',
  status: 'DRAFT' as EditableLessonStatus,
  sortOrder: 0,
})

const nonArchivedLessons = computed(() =>
  lessons.value.filter((lesson) => lesson.status !== 'ARCHIVED'),
)

const archivedLessonCount = computed(
  () => lessons.value.filter((lesson) => lesson.status === 'ARCHIVED').length,
)

const publishedLessonCount = computed(
  () => lessons.value.filter((lesson) => lesson.status === 'PUBLISHED').length,
)

const isArchived = computed(() => course.value?.status === 'ARCHIVED')

const firstDraftLesson = computed(
  () => nonArchivedLessons.value.find((lesson) => lesson.status === 'DRAFT') ?? null,
)

const nextSetupAction = computed(() => {
  if (!course.value || isArchived.value) return null

  if (nonArchivedLessons.value.length === 0) {
    return {
      to: `/admin/courses/${course.value.id}/lessons/create`,
      label: 'Thêm bài học đầu tiên',
      hint: 'Khóa học chưa có nội dung. Hãy tạo lesson đầu tiên trước.',
    }
  }

  if (firstDraftLesson.value) {
    return {
      to: `/admin/lessons/${firstDraftLesson.value.id}/edit`,
      label: 'Hoàn thiện bài nháp',
      hint: `Còn bài “${firstDraftLesson.value.title}” đang ở trạng thái Bản nháp.`,
    }
  }

  if (course.value.status !== 'PUBLISHED') {
    return {
      to: `/admin/courses/${course.value.id}/edit`,
      label: 'Xuất bản khóa học',
      hint: 'Các lesson đã sẵn sàng. Kiểm tra lại course và chuyển sang Đã xuất bản.',
    }
  }

  return {
    to: '/admin/students',
    label: 'Gán khóa học cho học viên',
    hint: 'Nội dung đã xuất bản. Bước vận hành tiếp theo là gán course cho học viên.',
  }
})

const setupSteps = computed(() => [
  { label: 'Thông tin khóa học', done: Boolean(course.value) },
  { label: 'Có bài học', done: nonArchivedLessons.value.length > 0 },
  { label: 'Có bài đã xuất bản', done: publishedLessonCount.value > 0 },
  { label: 'Khóa học đã xuất bản', done: course.value?.status === 'PUBLISHED' },
])

function courseStatusLabel(status: CourseStatus): string {
  if (status === 'PUBLISHED') return 'Đã xuất bản'
  if (status === 'DRAFT') return 'Bản nháp'
  return 'Đã lưu trữ'
}

function courseStatusClass(status: CourseStatus): string {
  if (status === 'PUBLISHED') return 'border-[#b9dfcc] bg-[#eef9f3] text-[#16764f]'
  if (status === 'DRAFT') return 'border-[#efcfaa] bg-[#fff7ed] text-[#b76500]'
  return 'border-[#ddd5d4] bg-[#f1eeed] text-[#766967]'
}

function lessonStatusLabel(status: LessonStatus): string {
  if (status === 'PUBLISHED') return 'Đã xuất bản'
  if (status === 'DRAFT') return 'Bản nháp'
  return 'Đã lưu trữ'
}

function lessonStatusClass(status: LessonStatus): string {
  if (status === 'PUBLISHED') return 'border-[#b9dfcc] bg-[#eef9f3] text-[#16764f]'
  if (status === 'DRAFT') return 'border-[#efcfaa] bg-[#fff7ed] text-[#b76500]'
  return 'border-[#ddd5d4] bg-[#f1eeed] text-[#766967]'
}

function resetCourseForm(data: AdminCourseDetail): void {
  courseForm.title = data.title
  courseForm.description = data.description ?? ''
  courseForm.thumbnailUrl = data.thumbnailUrl ?? ''
  courseForm.sortOrder = data.sortOrder
  courseForm.status = data.status === 'PUBLISHED' ? 'PUBLISHED' : 'DRAFT'
}

function resetLessonForm(): void {
  const effectiveLessons = lessons.value.filter((lesson) => lesson.status !== 'ARCHIVED')
  lessonForm.title = ''
  lessonForm.description = ''
  lessonForm.videoUrl = ''
  lessonForm.status = 'DRAFT'
  lessonForm.sortOrder =
    effectiveLessons.length > 0
      ? Math.max(...effectiveLessons.map((lesson) => lesson.sortOrder)) + 1
      : 0
}

async function load(): Promise<void> {
  if (!Number.isFinite(courseId.value) || courseId.value <= 0) return

  loading.value = true
  problem.value = null
  thumbnailFailed.value = false

  try {
    const requestedId = courseId.value
    const [courseData, lessonData] = await Promise.all([
      courseApi.getAdminCourse(requestedId),
      lessonApi.getAdminLessons(requestedId),
    ])

    if (courseId.value !== requestedId) return

    course.value = courseData
    lessons.value = lessonData
    resetCourseForm(courseData)
    resetLessonForm()
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}

function openEditCourse(): void {
  if (!course.value || isArchived.value) return
  void router.push(`/admin/courses/${course.value.id}/edit`)
}

function openCreateLesson(): void {
  if (!course.value || isArchived.value) return
  void router.push(`/admin/courses/${course.value.id}/lessons/create`)
}

function closeModal(): void {
  if (savingCourse.value || creatingLesson.value) return

  editCourseOpen.value = false
  createLessonOpen.value = false

  if (route.hash) {
    void router.replace(`/admin/courses/${courseId.value}`)
  }
}

async function saveCourse(): Promise<void> {
  if (!course.value || savingCourse.value || isArchived.value) return

  savingCourse.value = true
  problem.value = null

  try {
    const updated = await courseApi.updateCourse(course.value.id, {
      title: courseForm.title.trim(),
      description: courseForm.description.trim() || null,
      thumbnailUrl: courseForm.thumbnailUrl.trim() || null,
      status: courseForm.status,
      sortOrder: courseForm.sortOrder,
      version: course.value.version,
    })

    course.value = updated
    resetCourseForm(updated)
    editCourseOpen.value = false
    await router.replace(`/admin/courses/${updated.id}`)
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    savingCourse.value = false
  }
}

async function createLesson(): Promise<void> {
  if (!course.value || creatingLesson.value || isArchived.value) return

  creatingLesson.value = true
  problem.value = null

  try {
    const created = await lessonApi.createLesson(course.value.id, {
      title: lessonForm.title.trim(),
      description: lessonForm.description.trim() || null,
      contentHtml: null,
      videoUrl: lessonForm.videoUrl.trim() || null,
      sortOrder: lessonForm.sortOrder,
      status: lessonForm.status,
    })

    createLessonOpen.value = false
    await router.replace(`/admin/lessons/${created.id}/edit`)
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    creatingLesson.value = false
  }
}

async function archiveCourse(): Promise<void> {
  if (!course.value || isArchived.value || archiving.value) return

  const confirmed = window.confirm(
    `Lưu trữ khóa học "${course.value.title}"?\n\nHọc viên sẽ không còn truy cập khóa học này. Lịch sử học tập vẫn được giữ lại.`,
  )
  if (!confirmed) return

  archiving.value = true
  problem.value = null

  try {
    await courseApi.archiveCourse(course.value.id)
    await load()
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    archiving.value = false
  }
}

watch(
  () => route.hash,
  (hash) => {
    if (hash === '#edit-course' && course.value && !isArchived.value) {
      resetCourseForm(course.value)
      editCourseOpen.value = true
      createLessonOpen.value = false
      return
    }

    if (hash === '#create-lesson' && course.value && !isArchived.value) {
      resetLessonForm()
      createLessonOpen.value = true
      editCourseOpen.value = false
      return
    }

    editCourseOpen.value = false
    createLessonOpen.value = false
  },
)

watch(
  () => route.params.courseId,
  () => {
    void load()
  },
)

watch(course, (value) => {
  if (!value) return

  if (route.hash === '#edit-course' && !isArchived.value) {
    resetCourseForm(value)
    editCourseOpen.value = true
  }

  if (route.hash === '#create-lesson' && !isArchived.value) {
    resetLessonForm()
    createLessonOpen.value = true
  }
})

onMounted(load)
</script>

<template>
  <section>
    <!-- Loading -->
    <div v-if="loading" class="space-y-6">
      <div class="h-4 w-52 animate-pulse rounded bg-[#eae3e2]" />
      <div class="rounded-2xl border border-[#e7dedd] bg-white p-6 sm:p-7">
        <div class="grid gap-6 lg:grid-cols-[250px_minmax(0,1fr)]">
          <div class="aspect-[4/3] animate-pulse rounded-xl bg-[#eee8e7]" />
          <div class="py-2">
            <div class="h-8 w-3/5 animate-pulse rounded bg-[#eae3e2]" />
            <div class="mt-4 h-4 w-full animate-pulse rounded bg-[#f0ebea]" />
            <div class="mt-2 h-4 w-4/5 animate-pulse rounded bg-[#f0ebea]" />
            <div class="mt-6 h-9 w-72 animate-pulse rounded bg-[#eee8e7]" />
          </div>
        </div>
      </div>

      <div v-for="index in 4" :key="index" class="h-24 animate-pulse rounded-xl bg-[#eee8e7]" />
    </div>

    <template v-else>
      <ProblemAlert class="mb-6" :problem="problem" />

      <template v-if="course">
        <!-- Breadcrumb -->
        <nav
          aria-label="Breadcrumb"
          class="mb-5 flex flex-wrap items-center gap-2 text-xs font-semibold text-[#8d7d7b]"
        >
          <RouterLink to="/admin/courses" class="transition hover:text-[#a0001c]">
            Khóa học
          </RouterLink>
          <svg
            class="size-3.5 text-[#b9aaa8]"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            aria-hidden="true"
          >
            <path d="m9 18 6-6-6-6" />
          </svg>
          <span class="max-w-[520px] truncate text-[#403735]">{{ course.title }}</span>
        </nav>

        <!-- Course hero -->
        <article
          class="relative overflow-hidden rounded-2xl border border-[#e7dedd] bg-white p-5 shadow-[0_2px_14px_rgba(50,25,25,0.025)] sm:p-7"
        >
          <div
            class="pointer-events-none absolute right-7 top-0 hidden h-12 w-8 rounded-b-md bg-[#c9152b]/[0.08] lg:block"
          />

          <div class="grid gap-6 lg:grid-cols-[250px_minmax(0,1fr)] lg:items-start">
            <!-- Thumbnail -->
            <div
              class="course-grid relative aspect-video overflow-hidden rounded-xl border border-[#e4dcdb] bg-[#f5f1f0] lg:aspect-[4/3]"
            >
              <img
                v-if="course.thumbnailUrl && !thumbnailFailed"
                :src="course.thumbnailUrl"
                :alt="`Ảnh đại diện ${course.title}`"
                class="absolute inset-0 size-full object-cover"
                @error="thumbnailFailed = true"
              />

              <div
                v-else
                class="absolute inset-0 flex flex-col items-center justify-center text-[#a28f8d]"
              >
                <div
                  class="grid size-12 place-items-center rounded-xl border border-[#ead8da] bg-white/75 text-[#c9152b]"
                >
                  <svg
                    class="size-6"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="1.7"
                    aria-hidden="true"
                  >
                    <path d="M4 5.5A2.5 2.5 0 0 1 6.5 3H20v16H6.5A2.5 2.5 0 0 0 4 21.5v-16Z" />
                    <path d="M4 18.5A2.5 2.5 0 0 1 6.5 16H20" />
                  </svg>
                </div>
                <span class="mt-3 text-xs font-semibold">
                  {{ thumbnailFailed ? 'Không tải được ảnh' : 'Chưa có ảnh đại diện' }}
                </span>
              </div>
            </div>

            <!-- Information -->
            <div class="min-w-0">
              <div class="flex flex-col justify-between gap-5 xl:flex-row xl:items-start">
                <div class="min-w-0">
                  <div class="flex flex-wrap items-center gap-3">
                    <h1
                      class="min-w-0 text-[28px] font-black leading-tight tracking-[-0.03em] text-[#1c1b1b] sm:text-[34px]"
                    >
                      {{ course.title }}
                    </h1>

                    <span
                      class="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-bold"
                      :class="courseStatusClass(course.status)"
                    >
                      <span class="size-1.5 rounded-full bg-current opacity-75" />
                      {{ courseStatusLabel(course.status) }}
                    </span>
                  </div>

                  <p class="mt-3 max-w-3xl text-sm leading-6 text-[#6f615f] sm:text-[15px]">
                    {{
                      course.description ||
                      'Khóa học chưa có mô tả. Hãy bổ sung mô tả để quản trị nội dung rõ ràng hơn.'
                    }}
                  </p>

                  <div class="mt-5 flex flex-wrap gap-x-6 gap-y-3 text-sm font-semibold text-[#786967]">
                    <span class="inline-flex items-center gap-2">
                      <svg
                        class="size-4.5 text-[#9d8987]"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="1.8"
                        aria-hidden="true"
                      >
                        <path d="M4 5.5A2.5 2.5 0 0 1 6.5 3H20v16H6.5A2.5 2.5 0 0 0 4 21.5v-16Z" />
                        <path d="M4 18.5A2.5 2.5 0 0 1 6.5 16H20" />
                      </svg>
                      {{ nonArchivedLessons.length }} bài học
                    </span>

                    <span class="inline-flex items-center gap-2">
                      <svg
                        class="size-4.5 text-[#9d8987]"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="1.8"
                        aria-hidden="true"
                      >
                        <path d="M8 6h11M8 12h8M8 18h5M4 5v14" />
                      </svg>
                      Thứ tự {{ course.sortOrder }}
                    </span>

                    <span v-if="archivedLessonCount > 0" class="inline-flex items-center gap-2">
                      <svg
                        class="size-4.5 text-[#9d8987]"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="1.8"
                        aria-hidden="true"
                      >
                        <path d="M4 7h16M6 7v13h12V7M8 4h8" />
                      </svg>
                      {{ archivedLessonCount }} bài đã lưu trữ
                    </span>
                  </div>
                </div>

                <!-- Actions -->
                <div class="flex shrink-0 flex-wrap gap-2">
                  <button
                    v-if="!isArchived"
                    type="button"
                    class="inline-flex h-10 items-center gap-2 rounded-lg bg-[#c9152b] px-4 text-sm font-bold text-white shadow-[0_6px_15px_rgba(160,0,28,0.12)] transition hover:bg-[#a0001c]"
                    @click="openCreateLesson"
                  >
                    <svg
                      class="size-4"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="2"
                      aria-hidden="true"
                    >
                      <path d="M12 5v14M5 12h14" />
                    </svg>
                    Thêm bài học
                  </button>

                  <button
                    v-if="!isArchived"
                    type="button"
                    class="inline-flex h-10 items-center gap-2 rounded-lg border border-[#d7a8ad] bg-white px-4 text-sm font-bold text-[#a0001c] transition hover:bg-[#fff4f5]"
                    @click="openEditCourse"
                  >
                    <svg
                      class="size-4"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="1.8"
                      aria-hidden="true"
                    >
                      <path d="M12 20h9" />
                      <path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L8 18l-4 1 1-4Z" />
                    </svg>
                    Chỉnh sửa
                  </button>

                  <button
                    v-if="!isArchived"
                    type="button"
                    :disabled="archiving"
                    class="h-10 rounded-lg px-3.5 text-sm font-bold text-[#a82c38] transition hover:bg-[#fff1f2] disabled:cursor-not-allowed disabled:opacity-50"
                    @click="archiveCourse"
                  >
                    {{ archiving ? 'Đang lưu trữ…' : 'Lưu trữ' }}
                  </button>

                  <span
                    v-else
                    class="inline-flex h-10 items-center rounded-lg border border-[#ded6d5] bg-[#f5f2f1] px-4 text-sm font-bold text-[#7c6f6d]"
                  >
                    Khóa học đã lưu trữ
                  </span>
                </div>
              </div>
            </div>
          </div>
        </article>

        <section
          v-if="!isArchived && nextSetupAction"
          class="mt-5 rounded-2xl border border-[#eadfdf] bg-[#fffdfc] p-4 shadow-[0_2px_10px_rgba(50,25,25,0.02)] sm:p-5"
          aria-label="Quy trình thiết lập khóa học"
        >
          <div class="flex flex-col justify-between gap-4 lg:flex-row lg:items-center">
            <div class="min-w-0">
              <div class="flex flex-wrap items-center gap-2">
                <p class="text-[10px] font-black uppercase tracking-[0.15em] text-[#a0001c]">
                  Quy trình gợi ý
                </p>
                <span class="text-[10px] font-semibold text-[#9a8987]">
                  Không cần nhớ tài liệu — cứ đi từ trái sang phải
                </span>
              </div>

              <div class="mt-3 grid gap-2 sm:grid-cols-2 xl:grid-cols-4">
                <div
                  v-for="(step, index) in setupSteps"
                  :key="step.label"
                  class="flex items-center gap-2 rounded-lg border px-3 py-2.5"
                  :class="
                    step.done
                      ? 'border-[#cfe4d8] bg-[#f4faf7] text-[#245b43]'
                      : 'border-[#e5dedd] bg-white text-[#756765]'
                  "
                >
                  <span
                    class="grid size-5 shrink-0 place-items-center rounded-full text-[10px] font-black"
                    :class="step.done ? 'bg-[#16875b] text-white' : 'bg-[#f0eceb] text-[#8d7c7a]'"
                  >
                    {{ step.done ? '✓' : index + 1 }}
                  </span>
                  <span class="text-[11px] font-bold leading-4">{{ step.label }}</span>
                </div>
              </div>
            </div>

            <div class="shrink-0 rounded-xl border border-[#edcfd2] bg-[#fff4f5] p-3 lg:w-[285px]">
              <p class="text-[10px] font-black uppercase tracking-[0.12em] text-[#a0001c]">
                Bước tiếp theo
              </p>
              <p class="mt-1 text-xs leading-5 text-[#756065]">{{ nextSetupAction.hint }}</p>
              <RouterLink
                :to="nextSetupAction.to"
                class="mt-2.5 inline-flex h-9 items-center gap-2 rounded-lg bg-[#c9152b] px-3.5 text-xs font-bold text-white transition hover:bg-[#a0001c]"
              >
                {{ nextSetupAction.label }}
                <svg class="size-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M5 12h14M14 7l5 5-5 5" />
                </svg>
              </RouterLink>
            </div>
          </div>
        </section>

        <!-- Curriculum -->
        <section class="mt-8">
          <div
            class="mb-5 flex flex-col justify-between gap-3 border-b border-[#e4dcdb] pb-4 sm:flex-row sm:items-end"
          >
            <div>
              <p class="text-xs font-bold uppercase tracking-[0.16em] text-[#9b8583]">
                Lộ trình nội dung
              </p>
              <h2 class="mt-1 text-xl font-black tracking-[-0.02em] text-[#302929]">
                Nội dung khóa học
              </h2>
            </div>

            <div class="flex flex-wrap gap-2 text-xs font-bold">
              <span class="rounded-lg bg-[#eef9f3] px-2.5 py-1.5 text-[#16764f]">
                {{ publishedLessonCount }} đã xuất bản
              </span>
              <span class="rounded-lg bg-[#f5f1f0] px-2.5 py-1.5 text-[#786967]">
                {{ lessons.length }} tổng cộng
              </span>
            </div>
          </div>

          <!-- Empty -->
          <div
            v-if="lessons.length === 0"
            class="flex min-h-[300px] flex-col items-center justify-center rounded-2xl border border-dashed border-[#d9cecd] bg-white/55 px-6 py-12 text-center"
          >
            <div
              class="grid size-16 place-items-center rounded-2xl border border-[#eadfdf] bg-white text-[#aa9694]"
            >
              <svg
                class="size-8"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="1.6"
                aria-hidden="true"
              >
                <path d="M5 4h14v16H5z" />
                <path d="M9 8h6M9 12h6M9 16h4" />
              </svg>
            </div>
            <h3 class="mt-5 text-lg font-black text-[#342d2d]">Chưa có bài học</h3>
            <p class="mt-2 max-w-md text-sm leading-6 text-[#81716f]">
              Bắt đầu xây dựng lộ trình bằng bài học đầu tiên của khóa học.
            </p>

            <button
              v-if="!isArchived"
              type="button"
              class="mt-5 inline-flex h-10 items-center gap-2 rounded-lg bg-[#c9152b] px-4 text-sm font-bold text-white hover:bg-[#a0001c]"
              @click="openCreateLesson"
            >
              <svg
                class="size-4"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                aria-hidden="true"
              >
                <path d="M12 5v14M5 12h14" />
              </svg>
              Thêm bài học đầu tiên
            </button>
          </div>

          <!-- Learning path -->
          <ol v-else class="relative space-y-4">
            <li
              v-for="(lesson, index) in lessons"
              :key="lesson.id"
              class="relative grid grid-cols-[34px_minmax(0,1fr)] gap-3 sm:grid-cols-[42px_minmax(0,1fr)] sm:gap-4"
            >
              <!-- Timeline -->
              <div class="relative flex justify-center pt-5">
                <div
                  v-if="index < lessons.length - 1"
                  class="absolute bottom-[-20px] top-7 w-px bg-[#dfd6d5]"
                />

                <span
                  class="relative z-10 grid size-5 place-items-center rounded-full border-2 bg-[#f8f7f5]"
                  :class="
                    lesson.status === 'PUBLISHED'
                      ? 'border-[#16875b] text-[#16875b]'
                      : lesson.status === 'DRAFT'
                        ? 'border-[#c9152b] text-[#c9152b]'
                        : 'border-[#b8aaa8] text-[#8e807e]'
                  "
                >
                  <svg
                    v-if="lesson.status === 'PUBLISHED'"
                    class="size-3"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="3"
                    aria-hidden="true"
                  >
                    <path d="m5 12 4 4L19 6" />
                  </svg>
                  <span
                    v-else-if="lesson.status === 'DRAFT'"
                    class="size-1.5 rounded-full bg-current"
                  />
                  <span v-else class="h-px w-2 bg-current" />
                </span>
              </div>

              <!-- Lesson card -->
              <article
                class="group rounded-xl border p-4 transition sm:p-5"
                :class="
                  lesson.status === 'ARCHIVED'
                    ? 'border-[#e2dcdb] bg-[#f2efee]/70 opacity-75'
                    : 'border-[#e5dedd] bg-white hover:border-[#ddb9bd] hover:shadow-[0_8px_24px_rgba(66,28,34,0.045)]'
                "
              >
                <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
                  <div class="min-w-0 flex-1">
                    <div class="flex flex-wrap items-center gap-2">
                      <span
                        class="rounded-md bg-[#f2eeed] px-2 py-1 text-[10px] font-black uppercase tracking-wide text-[#7b6d6b]"
                      >
                        Thứ tự {{ lesson.sortOrder }}
                      </span>

                      <span
                        class="inline-flex items-center rounded-full border px-2 py-0.5 text-[10px] font-bold"
                        :class="lessonStatusClass(lesson.status)"
                      >
                        {{ lessonStatusLabel(lesson.status) }}
                      </span>
                    </div>

                    <RouterLink
                      :to="`/admin/lessons/${lesson.id}/edit`"
                      class="mt-2 block w-fit max-w-full"
                    >
                      <h3
                        class="truncate text-base font-black tracking-[-0.015em] transition sm:text-[17px]"
                        :class="
                          lesson.status === 'ARCHIVED'
                            ? 'text-[#716563]'
                            : 'text-[#312a29] group-hover:text-[#a0001c]'
                        "
                      >
                        {{ lesson.title }}
                      </h3>
                    </RouterLink>

                    <p
                      v-if="lesson.description"
                      class="mt-1.5 line-clamp-2 text-sm leading-5 text-[#81716f]"
                    >
                      {{ lesson.description }}
                    </p>
                    <p v-else class="mt-1.5 text-sm italic text-[#a0918f]">
                      Chưa có mô tả bài học.
                    </p>
                  </div>

                  <div class="flex shrink-0 items-center gap-2">
                    <RouterLink
                      v-if="lesson.status !== 'ARCHIVED'"
                      :to="`/admin/lessons/${lesson.id}/exercises`"
                      class="inline-flex h-9 items-center gap-1.5 rounded-lg px-3 text-xs font-bold text-[#756765] hover:bg-[#f5f1f0] hover:text-[#7f1020]"
                      title="Quản lý câu hỏi & kiểm tra"
                    >
                      <svg
                        class="size-4"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="1.8"
                        aria-hidden="true"
                      >
                        <rect x="4" y="3" width="16" height="18" rx="2" />
                        <path d="M8 8h8M8 12h5M8 16h3" />
                      </svg>
                      Câu hỏi
                    </RouterLink>

                    <RouterLink
                      :to="`/admin/lessons/${lesson.id}/edit`"
                      class="inline-flex h-9 items-center rounded-lg border border-[#ddd3d2] bg-white px-3 text-xs font-bold text-[#665957] transition hover:border-[#d1a8ac] hover:bg-[#fff6f6] hover:text-[#a0001c]"
                    >
                      {{ lesson.status === 'ARCHIVED' ? 'Xem' : 'Chỉnh sửa' }}
                    </RouterLink>
                  </div>
                </div>
              </article>
            </li>
          </ol>
        </section>
      </template>
    </template>

    <!-- Edit course modal -->
    <Teleport to="body">
      <div
        v-if="editCourseOpen && course"
        class="fixed inset-0 z-[100] flex items-center justify-center p-4 sm:p-6"
        role="dialog"
        aria-modal="true"
        aria-labelledby="edit-course-title"
      >
        <button
          type="button"
          class="absolute inset-0 bg-[#251719]/45 backdrop-blur-[2px]"
          aria-label="Đóng cửa sổ chỉnh sửa khóa học"
          @click="closeModal"
        />

        <div
          class="relative z-10 max-h-[92vh] w-full max-w-[640px] overflow-y-auto rounded-2xl border border-[#e5dada] bg-white shadow-[0_24px_80px_rgba(40,20,23,0.2)]"
        >
          <div class="flex items-start justify-between gap-4 border-b border-[#eee6e5] px-6 py-5">
            <div>
              <p class="text-[11px] font-bold uppercase tracking-[0.16em] text-[#a0001c]">
                Thông tin khóa học
              </p>
              <h2 id="edit-course-title" class="mt-1 text-xl font-black text-[#2c2525]">
                Chỉnh sửa khóa học
              </h2>
            </div>

            <button
              type="button"
              :disabled="savingCourse"
              aria-label="Đóng"
              class="grid size-9 place-items-center rounded-lg text-[#8d7d7b] hover:bg-[#f5f0ef]"
              @click="closeModal"
            >
              <svg class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M6 6l12 12M18 6 6 18" />
              </svg>
            </button>
          </div>

          <form class="space-y-5 p-6" @submit.prevent="saveCourse">
            <label class="block">
              <span class="mb-2 block text-sm font-bold text-[#403635]">
                Tên khóa học <span class="text-[#c9152b]">*</span>
              </span>
              <input
                v-model.trim="courseForm.title"
                required
                maxlength="200"
                class="h-11 w-full rounded-xl border border-[#ddd4d3] px-3.5 text-sm outline-none focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
              />
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-bold text-[#403635]">Mô tả</span>
              <textarea
                v-model.trim="courseForm.description"
                maxlength="4000"
                rows="4"
                class="w-full resize-y rounded-xl border border-[#ddd4d3] px-3.5 py-3 text-sm leading-6 outline-none focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
              />
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-bold text-[#403635]">URL ảnh đại diện</span>
              <input
                v-model.trim="courseForm.thumbnailUrl"
                type="url"
                pattern="https://.*"
                placeholder="https://..."
                class="h-11 w-full rounded-xl border border-[#ddd4d3] px-3.5 text-sm outline-none focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
              />
            </label>

            <div class="grid gap-4 sm:grid-cols-2">
              <label class="block">
                <span class="mb-2 block text-sm font-bold text-[#403635]">Trạng thái</span>
                <select
                  v-model="courseForm.status"
                  class="h-11 w-full rounded-xl border border-[#ddd4d3] bg-white px-3.5 text-sm font-semibold outline-none focus:border-[#c9152b]"
                >
                  <option value="DRAFT">Bản nháp</option>
                  <option value="PUBLISHED">Đã xuất bản</option>
                </select>
              </label>

              <label class="block">
                <span class="mb-2 block text-sm font-bold text-[#403635]">Thứ tự</span>
                <input
                  v-model.number="courseForm.sortOrder"
                  required
                  min="0"
                  step="1"
                  type="number"
                  class="h-11 w-full rounded-xl border border-[#ddd4d3] px-3.5 text-sm outline-none focus:border-[#c9152b]"
                />
              </label>
            </div>

            <div class="flex flex-col-reverse gap-3 border-t border-[#eee6e5] pt-5 sm:flex-row sm:justify-end">
              <button
                type="button"
                :disabled="savingCourse"
                class="h-10 rounded-lg border border-[#ddd3d2] bg-white px-4 text-sm font-bold text-[#6d5f5d] hover:bg-[#f8f5f4]"
                @click="closeModal"
              >
                Hủy
              </button>
              <button
                type="submit"
                :disabled="savingCourse"
                class="h-10 rounded-lg bg-[#c9152b] px-5 text-sm font-bold text-white hover:bg-[#a0001c] disabled:opacity-50"
              >
                {{ savingCourse ? 'Đang lưu…' : 'Lưu thay đổi' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- Create lesson modal -->
    <Teleport to="body">
      <div
        v-if="createLessonOpen && course"
        class="fixed inset-0 z-[100] flex items-center justify-center p-4 sm:p-6"
        role="dialog"
        aria-modal="true"
        aria-labelledby="create-lesson-title"
      >
        <button
          type="button"
          class="absolute inset-0 bg-[#251719]/45 backdrop-blur-[2px]"
          aria-label="Đóng cửa sổ tạo bài học"
          @click="closeModal"
        />

        <div
          class="relative z-10 max-h-[92vh] w-full max-w-[620px] overflow-y-auto rounded-2xl border border-[#e5dada] bg-white shadow-[0_24px_80px_rgba(40,20,23,0.2)]"
        >
          <div class="flex items-start justify-between gap-4 border-b border-[#eee6e5] px-6 py-5">
            <div>
              <p class="text-[11px] font-bold uppercase tracking-[0.16em] text-[#a0001c]">
                {{ course.title }}
              </p>
              <h2 id="create-lesson-title" class="mt-1 text-xl font-black text-[#2c2525]">
                Thêm bài học
              </h2>
              <p class="mt-1 text-sm text-[#81716f]">
                Tạo khung bài học trước, sau đó hoàn thiện nội dung trong trang chỉnh sửa.
              </p>
            </div>

            <button
              type="button"
              :disabled="creatingLesson"
              aria-label="Đóng"
              class="grid size-9 place-items-center rounded-lg text-[#8d7d7b] hover:bg-[#f5f0ef]"
              @click="closeModal"
            >
              <svg class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M6 6l12 12M18 6 6 18" />
              </svg>
            </button>
          </div>

          <form class="space-y-5 p-6" @submit.prevent="createLesson">
            <label class="block">
              <span class="mb-2 block text-sm font-bold text-[#403635]">
                Tên bài học <span class="text-[#c9152b]">*</span>
              </span>
              <input
                v-model.trim="lessonForm.title"
                required
                maxlength="200"
                placeholder="Ví dụ: Biến và kiểu dữ liệu"
                class="h-11 w-full rounded-xl border border-[#ddd4d3] px-3.5 text-sm outline-none focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
              />
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-bold text-[#403635]">Mô tả ngắn</span>
              <textarea
                v-model.trim="lessonForm.description"
                rows="3"
                placeholder="Nội dung chính của bài học…"
                class="w-full resize-y rounded-xl border border-[#ddd4d3] px-3.5 py-3 text-sm leading-6 outline-none focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
              />
            </label>

            <label class="block">
              <span class="mb-2 block text-sm font-bold text-[#403635]">YouTube URL</span>
              <input
                v-model.trim="lessonForm.videoUrl"
                type="url"
                placeholder="https://www.youtube.com/watch?v=..."
                class="h-11 w-full rounded-xl border border-[#ddd4d3] px-3.5 text-sm outline-none focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
              />
              <span class="mt-1.5 block text-xs leading-5 text-[#8d7c7a]">
                Video là tùy chọn và sẽ được backend kiểm tra/chuẩn hóa.
              </span>
            </label>

            <div class="grid gap-4 sm:grid-cols-2">
              <label class="block">
                <span class="mb-2 block text-sm font-bold text-[#403635]">Trạng thái</span>
                <select
                  v-model="lessonForm.status"
                  class="h-11 w-full rounded-xl border border-[#ddd4d3] bg-white px-3.5 text-sm font-semibold outline-none focus:border-[#c9152b]"
                >
                  <option value="DRAFT">Bản nháp</option>
                  <option value="PUBLISHED">Đã xuất bản</option>
                </select>
              </label>

              <label class="block">
                <span class="mb-2 block text-sm font-bold text-[#403635]">Thứ tự</span>
                <input
                  v-model.number="lessonForm.sortOrder"
                  required
                  min="0"
                  step="1"
                  type="number"
                  class="h-11 w-full rounded-xl border border-[#ddd4d3] px-3.5 text-sm outline-none focus:border-[#c9152b]"
                />
              </label>
            </div>

            <div
              class="rounded-xl border border-[#eadfdf] bg-[#faf7f6] px-4 py-3 text-xs leading-5 text-[#786967]"
            >
              Sau khi tạo, hệ thống sẽ mở trang chỉnh sửa bài học để bạn thêm nội dung chi tiết và bài tập.
            </div>

            <div class="flex flex-col-reverse gap-3 border-t border-[#eee6e5] pt-5 sm:flex-row sm:justify-end">
              <button
                type="button"
                :disabled="creatingLesson"
                class="h-10 rounded-lg border border-[#ddd3d2] bg-white px-4 text-sm font-bold text-[#6d5f5d] hover:bg-[#f8f5f4]"
                @click="closeModal"
              >
                Hủy
              </button>
              <button
                type="submit"
                :disabled="creatingLesson"
                class="h-10 rounded-lg bg-[#c9152b] px-5 text-sm font-bold text-white hover:bg-[#a0001c] disabled:opacity-50"
              >
                {{ creatingLesson ? 'Đang tạo…' : 'Tạo bài học' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>
  </section>
</template>

<style scoped>
.course-grid {
  background-image:
    linear-gradient(to right, rgb(232 227 227 / 0.72) 1px, transparent 1px),
    linear-gradient(to bottom, rgb(232 227 227 / 0.72) 1px, transparent 1px);
  background-size: 22px 22px;
}
</style>