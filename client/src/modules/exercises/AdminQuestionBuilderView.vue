<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute } from 'vue-router'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'
import * as courseApi from '@/modules/courses/course.api'
import type { AdminCourseDetail } from '@/modules/courses/models/AdminCourseDetail'
import * as lessonApi from '@/modules/lessons/lesson.api'
import type { AdminLesson } from '@/modules/lessons/models/AdminLesson'
import type { QuestionPlacement } from '@/types/QuestionPlacement'
import type { QuestionType } from '@/types/QuestionType'

import type { AdminQuestion } from './models/AdminQuestion'

interface QuestionEditorForm {
  text: string
  type: QuestionType
  placement: QuestionPlacement
  videoTimestampSeconds: number | null
  explanation: string
  sortOrder: number
  options: string[]
}

const route = useRoute()

const lessonId = computed(() => Number(route.params.lessonId))

const lesson = ref<AdminLesson | null>(null)
const course = ref<AdminCourseDetail | null>(null)
const questions = ref<AdminQuestion[]>([])

const loading = ref(true)
const saving = ref(false)
const deletingId = ref<number | null>(null)
const problem = ref<ProblemDetails | null>(null)

const editingQuestionId = ref<number | null>(null)
const editingVersion = ref<number | null>(null)
const correctIndex = ref(0)
const historyLockedIds = ref<number[]>([])
const editorPanel = ref<HTMLElement | null>(null)

const form = reactive<QuestionEditorForm>({
  text: '',
  type: 'MULTIPLE_CHOICE',
  placement: 'REINFORCEMENT',
  videoTimestampSeconds: null,
  explanation: '',
  sortOrder: 0,
  options: ['', ''],
})

const isArchivedLesson = computed(() => lesson.value?.status === 'ARCHIVED')
const isEditing = computed(() => editingQuestionId.value !== null)

const formValid = computed(() => {
  if (!form.text.trim() || form.text.trim().length > 2000) return false
  if (!Number.isInteger(form.sortOrder) || form.sortOrder < 0) return false
  if (form.explanation.length > 4000) return false
  if (form.placement === 'VIDEO_CHECKPOINT') {
    if (!lesson.value?.video || !lesson.value.videoDurationSeconds) return false
    if (!Number.isInteger(form.videoTimestampSeconds)) return false
    if ((form.videoTimestampSeconds ?? 0) < 1 || (form.videoTimestampSeconds ?? 0) >= lesson.value.videoDurationSeconds) return false
  }

  if (form.type === 'TRUE_FALSE') {
    if (form.options.length !== 2) return false
  } else if (form.options.length < 2 || form.options.length > 4) {
    return false
  }

  if (correctIndex.value < 0 || correctIndex.value >= form.options.length) return false

  return form.options.every((option) => option.trim().length > 0 && option.trim().length <= 1000)
})

const nextSortOrder = computed(() => {
  if (questions.value.length === 0) return 0
  return Math.max(...questions.value.map((question) => question.sortOrder)) + 1
})

function typeLabel(type: QuestionType): string {
  return type === 'MULTIPLE_CHOICE' ? 'Nhiều lựa chọn' : 'Đúng / Sai'
}

function optionLetter(index: number): string {
  return String.fromCharCode(65 + index)
}

function placementLabel(question: AdminQuestion): string {
  return question.placement === 'VIDEO_CHECKPOINT'
    ? `Trong video · ${formatSeconds(question.videoTimestampSeconds ?? 0)}`
    : 'Củng cố cuối bài'
}

function formatSeconds(value: number): string {
  const seconds = Math.max(0, Math.floor(value))
  return `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, '0')}`
}

function onPlacementChange(): void {
  if (form.placement === 'REINFORCEMENT') {
    form.videoTimestampSeconds = null
    return
  }
  if (!form.videoTimestampSeconds) form.videoTimestampSeconds = 30
}

function isHistoryLocked(questionId: number): boolean {
  return historyLockedIds.value.includes(questionId)
}

function markHistoryLocked(questionId: number): void {
  if (!historyLockedIds.value.includes(questionId)) {
    historyLockedIds.value = [...historyLockedIds.value, questionId]
  }
}

function resetEditor(): void {
  editingQuestionId.value = null
  editingVersion.value = null
  correctIndex.value = 0
  form.text = ''
  form.type = 'MULTIPLE_CHOICE'
  form.placement = 'REINFORCEMENT'
  form.videoTimestampSeconds = null
  form.explanation = ''
  form.sortOrder = nextSortOrder.value
  form.options = ['', '']
}

function startCreate(): void {
  if (isArchivedLesson.value) return
  resetEditor()

  requestAnimationFrame(() => {
    editorPanel.value?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  })
}

function startEdit(question: AdminQuestion): void {
  if (isArchivedLesson.value || isHistoryLocked(question.id)) return

  editingQuestionId.value = question.id
  editingVersion.value = question.version
  form.text = question.text
  form.type = question.type
  form.placement = question.placement
  form.videoTimestampSeconds = question.videoTimestampSeconds
  form.explanation = question.explanation ?? ''
  form.sortOrder = question.sortOrder

  const sortedOptions = [...question.options].sort((a, b) => a.sortOrder - b.sortOrder)
  form.options = sortedOptions.map((option) => option.content)

  const currentCorrectIndex = sortedOptions.findIndex((option) => option.isCorrect)
  correctIndex.value = currentCorrectIndex >= 0 ? currentCorrectIndex : 0

  requestAnimationFrame(() => {
    editorPanel.value?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  })
}

function onTypeChange(): void {
  correctIndex.value = 0

  if (form.type === 'TRUE_FALSE') {
    form.options = ['Đúng', 'Sai']
    return
  }

  form.options = ['', '']
}

function addOption(): void {
  if (form.type !== 'MULTIPLE_CHOICE' || form.options.length >= 4) return
  form.options.push('')
}

function removeOption(index: number): void {
  if (form.type !== 'MULTIPLE_CHOICE' || form.options.length <= 2) return

  form.options.splice(index, 1)

  if (correctIndex.value === index) {
    correctIndex.value = 0
  } else if (index < correctIndex.value) {
    correctIndex.value -= 1
  }
}

function selectCorrect(index: number): void {
  correctIndex.value = index
}

async function load(): Promise<void> {
  if (!Number.isFinite(lessonId.value) || lessonId.value <= 0) {
    problem.value = {
      status: 400,
      code: 'INVALID_LESSON_ID',
      title: 'Bài học không hợp lệ',
      detail: 'Mã bài học không hợp lệ.',
    }
    loading.value = false
    return
  }

  loading.value = true
  problem.value = null

  try {
    const requestedLessonId = lessonId.value
    const [lessonData, questionData] = await Promise.all([
      lessonApi.getAdminLesson(requestedLessonId),
      lessonApi.getQuestions(requestedLessonId),
    ])

    if (lessonId.value !== requestedLessonId) return

    const courseData = await courseApi.getAdminCourse(lessonData.courseId)
    if (lessonId.value !== requestedLessonId) return

    lesson.value = lessonData
    course.value = courseData
    questions.value = questionData
    resetEditor()
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}

async function reloadQuestions(): Promise<void> {
  const data = await lessonApi.getQuestions(lessonId.value)
  questions.value = data
}

async function submitQuestion(): Promise<void> {
  if (
    saving.value ||
    !formValid.value ||
    isArchivedLesson.value ||
    !lesson.value
  ) {
    return
  }

  saving.value = true
  problem.value = null

  const request = {
    text: form.text.trim(),
    type: form.type,
    placement: form.placement,
    videoTimestampSeconds: form.placement === 'VIDEO_CHECKPOINT' ? form.videoTimestampSeconds : null,
    explanation: form.explanation.trim() || null,
    sortOrder: form.sortOrder,
    options: form.options.map((content, index) => ({
      content: content.trim(),
      isCorrect: index === correctIndex.value,
      sortOrder: index + 1,
    })),
  }

  try {
    if (editingQuestionId.value !== null) {
      if (editingVersion.value === null) return

      await lessonApi.updateQuestion(editingQuestionId.value, {
        ...request,
        version: editingVersion.value,
      })
    } else {
      await lessonApi.createQuestion(lesson.value.id, request)
    }

    await reloadQuestions()
    resetEditor()
  } catch (error) {
    const nextProblem = toProblem(error)
    problem.value = nextProblem

    if (
      editingQuestionId.value !== null &&
      nextProblem.status === 409 &&
      (nextProblem.code === 'QUESTION_HAS_HISTORY' ||
        nextProblem.detail?.toLowerCase().includes('history'))
    ) {
      markHistoryLocked(editingQuestionId.value)
    }

    if (nextProblem.status === 409 && nextProblem.code === 'CONCURRENCY_CONFLICT') {
      await reloadQuestions()
    }

    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    saving.value = false
  }
}

async function deleteQuestion(question: AdminQuestion): Promise<void> {
  if (
    deletingId.value !== null ||
    isArchivedLesson.value ||
    isHistoryLocked(question.id)
  ) {
    return
  }

  const confirmed = window.confirm(
    `Xóa câu hỏi này?\n\n"${question.text}"\n\nChỉ câu hỏi chưa có lịch sử trả lời mới có thể bị xóa.`,
  )
  if (!confirmed) return

  deletingId.value = question.id
  problem.value = null

  try {
    await lessonApi.deleteQuestion(question.id)

    if (editingQuestionId.value === question.id) {
      resetEditor()
    }

    await reloadQuestions()
    resetEditor()
  } catch (error) {
    const nextProblem = toProblem(error)
    problem.value = nextProblem

    if (
      nextProblem.status === 409 &&
      (nextProblem.code === 'QUESTION_HAS_HISTORY' ||
        nextProblem.detail?.toLowerCase().includes('history'))
    ) {
      markHistoryLocked(question.id)
    }

    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    deletingId.value = null
  }
}

watch(
  () => route.params.lessonId,
  () => {
    void load()
  },
)

onMounted(load)
</script>

<template>
  <section>
    <!-- Loading -->
    <div v-if="loading" class="space-y-6">
      <div class="h-4 w-72 animate-pulse rounded bg-[#eae3e2]" />
      <div class="flex justify-between gap-6">
        <div>
          <div class="h-9 w-56 animate-pulse rounded bg-[#e9e2e1]" />
          <div class="mt-3 h-4 w-80 animate-pulse rounded bg-[#f0ebea]" />
        </div>
        <div class="h-10 w-36 animate-pulse rounded-lg bg-[#eadfdf]" />
      </div>

      <div class="grid gap-6 xl:grid-cols-[minmax(0,2fr)_380px]">
        <div class="space-y-5">
          <div v-for="index in 3" :key="index" class="h-72 animate-pulse rounded-2xl bg-[#eee8e7]" />
        </div>
        <div class="h-[620px] animate-pulse rounded-2xl bg-[#eee8e7]" />
      </div>
    </div>

    <template v-else>
      <ProblemAlert class="mb-5" :problem="problem" />

      <template v-if="lesson && course">
        <!-- Archived notice -->
        <div
          v-if="isArchivedLesson"
          class="mb-5 flex items-start gap-3 rounded-xl border border-[#ddd5d4] bg-[#f3f0ef] px-4 py-3.5"
        >
          <svg
            class="mt-0.5 size-5 shrink-0 text-[#7c6f6d]"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="1.8"
            aria-hidden="true"
          >
            <path d="M4 7h16M6 7v13h12V7M8 4h8" />
          </svg>
          <div>
            <p class="text-sm font-black text-[#554a48]">Bài học đã được lưu trữ</p>
            <p class="mt-1 text-xs leading-5 text-[#786967]">
              Bộ câu hỏi được giữ để phục vụ lịch sử dữ liệu. Trang này đang ở chế độ chỉ đọc.
            </p>
          </div>
        </div>

        <!-- Header -->
        <header class="mb-7 border-b border-[#e3dad9] pb-5">
          <div class="flex flex-col justify-between gap-5 lg:flex-row lg:items-end">
            <div class="min-w-0">
              <nav
                aria-label="Breadcrumb"
                class="mb-3 flex flex-wrap items-center gap-2 text-xs font-semibold text-[#8d7d7b]"
              >
                <RouterLink to="/admin/courses" class="hover:text-[#a0001c]">
                  Khóa học
                </RouterLink>
                <svg class="size-3.5 text-[#b8aaa8]" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="m9 18 6-6-6-6" />
                </svg>
                <RouterLink
                  :to="`/admin/courses/${course.id}`"
                  class="max-w-[300px] truncate hover:text-[#a0001c]"
                >
                  {{ course.title }}
                </RouterLink>
                <svg class="size-3.5 text-[#b8aaa8]" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="m9 18 6-6-6-6" />
                </svg>
                <RouterLink
                  :to="`/admin/lessons/${lesson.id}/edit`"
                  class="max-w-[300px] truncate hover:text-[#a0001c]"
                >
                  {{ lesson.title }}
                </RouterLink>
                <svg class="size-3.5 text-[#b8aaa8]" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="m9 18 6-6-6-6" />
                </svg>
                <span class="text-[#403735]">Câu hỏi & kiểm tra</span>
              </nav>

              <p class="text-xs font-bold uppercase tracking-[0.16em] text-[#9b8583]">
                {{ lesson.title }}
              </p>
              <h1 class="mt-1.5 text-[27px] font-black tracking-[-0.03em] text-[#1c1b1b] sm:text-[30px]">
                Câu hỏi & kiểm tra
              </h1>
              <p class="mt-2 text-sm leading-6 text-[#6b5d5c] sm:text-[15px]">
                Tạo checkpoint trong video hoặc bài củng cố cuối bài. Hệ thống dùng các câu hỏi này để quyết định học viên có đủ điều kiện hoàn thành lesson hay chưa.
              </p>
            </div>

            <button
              v-if="!isArchivedLesson"
              type="button"
              class="inline-flex h-10 shrink-0 items-center justify-center gap-2 self-start rounded-lg bg-[#c9152b] px-4 text-sm font-bold text-white shadow-[0_6px_15px_rgba(160,0,28,0.12)] hover:bg-[#a0001c] lg:self-auto"
              @click="startCreate"
            >
              <svg class="size-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 5v14M5 12h14" />
              </svg>
              Thêm câu hỏi
            </button>
          </div>
        </header>

        <section
          class="mb-4 grid gap-3 rounded-xl border border-[#eadfdf] bg-[#fffdfc] p-3 sm:grid-cols-2"
          aria-label="Giải thích hai loại câu hỏi"
        >
          <div class="rounded-lg border border-[#e6dedd] bg-white p-3">
            <div class="flex items-center gap-2">
              <span class="grid size-7 place-items-center rounded-lg bg-[#fff0f1] text-xs font-black text-[#a0001c]">1</span>
              <strong class="text-xs text-[#403735]">Checkpoint trong video</strong>
            </div>
            <p class="mt-2 text-[11px] leading-5 text-[#81716f]">
              Dùng khi muốn học viên dừng đúng một mốc trong video và trả lời đúng mới được xem tiếp.
            </p>
          </div>

          <div class="rounded-lg border border-[#e6dedd] bg-white p-3">
            <div class="flex items-center gap-2">
              <span class="grid size-7 place-items-center rounded-lg bg-[#f0f7f3] text-xs font-black text-[#16764f]">2</span>
              <strong class="text-xs text-[#403735]">Củng cố cuối bài</strong>
            </div>
            <p class="mt-2 text-[11px] leading-5 text-[#81716f]">
              Chỉ mở sau khi xem hết video. Học viên cần đạt trên 80% để hoàn thành lesson.
            </p>
          </div>
        </section>

        <!-- Main 8/4 layout -->
        <div class="grid gap-4 xl:h-[calc(100vh-190px)] xl:grid-cols-[minmax(0,2fr)_350px] xl:items-start">
          <!-- Question list -->
          <div class="min-w-0 space-y-4 xl:max-h-[calc(100vh-190px)] xl:overflow-y-auto xl:pr-2">
            <div class="flex items-center justify-between gap-4">
              <div>
                <h2 class="text-lg font-black tracking-[-0.02em] text-[#302929]">
                  Danh sách câu hỏi
                </h2>
                <p class="mt-1 text-xs font-medium text-[#8c7c7a]">
                  {{ questions.length }} câu hỏi · sắp xếp theo thứ tự hiển thị
                </p>
              </div>
            </div>

            <!-- Empty -->
            <div
              v-if="questions.length === 0"
              class="flex min-h-[340px] flex-col items-center justify-center rounded-2xl border border-dashed border-[#d9cecd] bg-white/60 px-6 py-12 text-center"
            >
              <div class="grid size-16 place-items-center rounded-2xl border border-[#eadfdf] bg-white text-[#a18d8b]">
                <svg class="size-8" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
                  <rect x="4" y="3" width="16" height="18" rx="2" />
                  <path d="M8 8h8M8 12h5M8 16h3" />
                </svg>
              </div>
              <h3 class="mt-5 text-lg font-black text-[#342d2d]">Chưa có câu hỏi</h3>
              <p class="mt-2 max-w-md text-sm leading-6 text-[#81716f]">
                Thêm câu hỏi đầu tiên để học viên có thể tự luyện tập sau khi học xong nội dung.
              </p>
              <button
                v-if="!isArchivedLesson"
                type="button"
                class="mt-5 inline-flex h-10 items-center gap-2 rounded-lg bg-[#c9152b] px-4 text-sm font-bold text-white hover:bg-[#a0001c]"
                @click="startCreate"
              >
                <svg class="size-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M12 5v14M5 12h14" />
                </svg>
                Thêm câu hỏi đầu tiên
              </button>
            </div>

            <!-- Cards -->
            <article
              v-for="(question, questionIndex) in questions"
              v-else
              :key="question.id"
              class="rounded-xl border bg-white p-4 transition sm:p-5"
              :class="
                editingQuestionId === question.id
                  ? 'border-[#dba7ad] shadow-[0_8px_28px_rgba(75,29,37,0.06)]'
                  : 'border-[#e5dedd] hover:border-[#dcb9bd]'
              "
            >
              <div class="flex items-start justify-between gap-4">
                <div class="flex min-w-0 flex-wrap items-center gap-2">
                  <span class="rounded-lg bg-[#f1edec] px-2.5 py-1 text-[11px] font-black text-[#5f5351]">
                    Câu {{ questionIndex + 1 }}
                  </span>

                  <span
                    class="rounded-lg border px-2.5 py-1 text-[10px] font-bold uppercase tracking-wide"
                    :class="
                      question.type === 'MULTIPLE_CHOICE'
                        ? 'border-[#cbe1d5] bg-[#eef9f3] text-[#16764f]'
                        : 'border-[#edc9cd] bg-[#fff3f4] text-[#a22435]'
                    "
                  >
                    {{ typeLabel(question.type) }}
                  </span>

                  <span class="rounded-md border border-[#e4dcdb] bg-[#faf7f6] px-2 py-1 text-[9px] font-bold text-[#756765]">
                    {{ placementLabel(question) }}
                  </span>

                  <span class="text-[11px] font-bold text-[#9a8987]">
                    Thứ tự {{ question.sortOrder }}
                  </span>
                </div>

                <div v-if="!isArchivedLesson" class="flex shrink-0 items-center gap-1">
                  <button
                    type="button"
                    :disabled="isHistoryLocked(question.id)"
                    class="grid size-8 place-items-center rounded-lg text-[#7d6e6c] transition hover:bg-[#f5f0ef] hover:text-[#a0001c] disabled:cursor-not-allowed disabled:opacity-35"
                    title="Chỉnh sửa câu hỏi"
                    @click="startEdit(question)"
                  >
                    <svg class="size-4.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                      <path d="M12 20h9" />
                      <path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L8 18l-4 1 1-4Z" />
                    </svg>
                  </button>

                  <button
                    type="button"
                    :disabled="deletingId === question.id || isHistoryLocked(question.id)"
                    class="grid size-8 place-items-center rounded-lg text-[#8c7775] transition hover:bg-[#fff1f2] hover:text-[#ba1a1a] disabled:cursor-not-allowed disabled:opacity-35"
                    title="Xóa câu hỏi"
                    @click="deleteQuestion(question)"
                  >
                    <svg class="size-4.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                      <path d="M4 7h16M6 7v13h12V7M9 11v5M15 11v5M8 4h8" />
                    </svg>
                  </button>
                </div>
              </div>

              <div
                v-if="isHistoryLocked(question.id)"
                class="mt-4 flex items-start gap-2.5 rounded-xl border border-[#edd5a9] bg-[#fff8ec] px-3.5 py-3"
              >
                <svg class="mt-0.5 size-4 shrink-0 text-[#b56a12]" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9">
                  <path d="M12 9v4M12 17h.01" />
                  <circle cx="12" cy="12" r="9" />
                </svg>
                <p class="text-xs leading-5 text-[#7a592c]">
                  Câu hỏi đã có lịch sử trả lời nên không thể sửa hoặc xóa. Hãy tạo câu hỏi thay thế nếu cần thay đổi nội dung.
                </p>
              </div>

              <p class="mt-5 text-[16px] font-bold leading-7 text-[#302929] sm:text-[17px]">
                {{ question.text }}
              </p>

              <div class="mt-5 space-y-2.5">
                <div
                  v-for="(option, optionIndex) in [...question.options].sort((a, b) => a.sortOrder - b.sortOrder)"
                  :key="option.id"
                  class="flex items-center gap-3 rounded-xl border px-3.5 py-3"
                  :class="
                    option.isCorrect
                      ? 'border-[#a9d8bf] bg-[#f0faf5]'
                      : 'border-[#e4dcdb] bg-[#fffefe]'
                  "
                >
                  <span
                    class="grid size-7 shrink-0 place-items-center rounded-lg text-xs font-black"
                    :class="
                      option.isCorrect
                        ? 'bg-[#dff3e8] text-[#16764f]'
                        : 'bg-[#f2eeed] text-[#756765]'
                    "
                  >
                    {{ optionLetter(optionIndex) }}
                  </span>

                  <span class="min-w-0 flex-1 text-sm font-medium leading-5 text-[#4a4140]">
                    {{ option.content }}
                  </span>

                  <span
                    v-if="option.isCorrect"
                    class="inline-flex shrink-0 items-center gap-1 text-[11px] font-black text-[#16764f]"
                  >
                    <svg class="size-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4">
                      <circle cx="12" cy="12" r="9" />
                      <path d="m8 12 2.5 2.5L16.5 9" />
                    </svg>
                    Đáp án đúng
                  </span>
                </div>
              </div>

              <div
                v-if="question.explanation"
                class="mt-5 rounded-xl border border-[#e6dfde] bg-[#f7f4f3] px-4 py-3.5"
              >
                <div class="flex items-center gap-2 text-xs font-black text-[#6d5f5d]">
                  <svg class="size-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                    <path d="M9 18h6M10 22h4" />
                    <path d="M8.5 14.5A6 6 0 1 1 15.5 14.5c-.9.7-1.5 1.5-1.5 2.5h-4c0-1-.6-1.8-1.5-2.5Z" />
                  </svg>
                  Giải thích
                </div>
                <p class="mt-2 text-sm leading-6 text-[#625654]">
                  {{ question.explanation }}
                </p>
              </div>
            </article>
          </div>

          <!-- Editor -->
          <aside
            ref="editorPanel"
            class="scroll-mt-20 xl:max-h-[calc(100vh-190px)] xl:overflow-y-auto xl:self-start xl:pr-1"
          >
            <section
              class="overflow-hidden rounded-2xl border border-[#e4dbda] bg-white shadow-[0_8px_28px_rgba(60,27,32,0.045)]"
            >
              <div class="border-b border-[#ece5e4] px-5 py-4 sm:px-6">
                <div class="flex items-start justify-between gap-3">
                  <div>
                    <p class="text-[10px] font-bold uppercase tracking-[0.16em] text-[#9b8583]">
                      Trình tạo câu hỏi
                    </p>
                    <h2 class="mt-1 text-lg font-black tracking-[-0.02em] text-[#302929]">
                      {{ isEditing ? 'Chỉnh sửa câu hỏi' : 'Câu hỏi mới' }}
                    </h2>
                  </div>

                  <button
                    v-if="isEditing && !isArchivedLesson"
                    type="button"
                    class="rounded-lg px-2.5 py-1.5 text-xs font-bold text-[#7d6e6c] hover:bg-[#f5f1f0]"
                    @click="resetEditor"
                  >
                    Tạo mới
                  </button>
                </div>
              </div>

              <div v-if="isArchivedLesson" class="p-6 text-center">
                <div class="mx-auto grid size-12 place-items-center rounded-xl bg-[#f1eeed] text-[#8a7b79]">
                  <svg class="size-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7">
                    <path d="M4 7h16M6 7v13h12V7M8 4h8" />
                  </svg>
                </div>
                <p class="mt-4 text-sm font-black text-[#504644]">Chế độ chỉ đọc</p>
                <p class="mt-1 text-xs leading-5 text-[#81716f]">
                  Không thể thay đổi câu hỏi của bài học đã lưu trữ.
                </p>
              </div>

              <form v-else class="space-y-5 p-5 sm:p-6" @submit.prevent="submitQuestion">
                <!-- Learning placement -->
                <fieldset class="rounded-xl border border-[#eadfdf] bg-[#faf7f6] p-3">
                  <legend class="px-1 text-xs font-black text-[#4a403f]">
                    Câu hỏi này dùng ở đâu?
                  </legend>

                  <div class="mt-2 grid gap-2">
                    <label class="cursor-pointer">
                      <input
                        v-model="form.placement"
                        type="radio"
                        value="REINFORCEMENT"
                        class="peer sr-only"
                        @change="onPlacementChange"
                      />
                      <span
                        class="flex items-start gap-3 rounded-lg border border-[#e2d9d8] bg-white p-3 transition peer-checked:border-[#d99da5] peer-checked:bg-[#fff5f6] peer-focus-visible:ring-3 peer-focus-visible:ring-[#c9152b]/10"
                      >
                        <span class="mt-0.5 grid size-7 shrink-0 place-items-center rounded-lg bg-[#f2eeed] text-xs font-black text-[#756765] peer-checked:bg-[#c9152b]">
                          A
                        </span>
                        <span>
                          <strong class="block text-xs text-[#403735]">Củng cố cuối bài</strong>
                          <span class="mt-1 block text-[10px] leading-4 text-[#81716f]">
                            Học viên làm sau khi xem hết video. Điểm nhóm này phải trên 80%.
                          </span>
                        </span>
                      </span>
                    </label>

                    <label
                      class="cursor-pointer"
                      :class="!lesson.video || !lesson.videoDurationSeconds ? 'cursor-not-allowed opacity-55' : ''"
                    >
                      <input
                        v-model="form.placement"
                        type="radio"
                        value="VIDEO_CHECKPOINT"
                        class="peer sr-only"
                        :disabled="!lesson.video || !lesson.videoDurationSeconds"
                        @change="onPlacementChange"
                      />
                      <span
                        class="flex items-start gap-3 rounded-lg border border-[#e2d9d8] bg-white p-3 transition peer-checked:border-[#d99da5] peer-checked:bg-[#fff5f6] peer-focus-visible:ring-3 peer-focus-visible:ring-[#c9152b]/10"
                      >
                        <span class="mt-0.5 grid size-7 shrink-0 place-items-center rounded-lg bg-[#fff0f1] text-xs font-black text-[#a0001c]">
                          B
                        </span>
                        <span>
                          <strong class="block text-xs text-[#403735]">Checkpoint trong video</strong>
                          <span class="mt-1 block text-[10px] leading-4 text-[#81716f]">
                            Video tự dừng tại mốc bạn chọn. Học viên phải trả lời đúng mới xem tiếp.
                          </span>
                        </span>
                      </span>
                    </label>
                  </div>

                  <label v-if="form.placement === 'VIDEO_CHECKPOINT'" class="mt-3 block">
                    <span class="mb-2 flex justify-between text-xs font-bold text-[#4a403f]">
                      <span>Xuất hiện tại giây thứ</span>
                      <span class="text-[#a0001c]">{{ formatSeconds(form.videoTimestampSeconds ?? 0) }}</span>
                    </span>
                    <input
                      v-model.number="form.videoTimestampSeconds"
                      type="number"
                      min="1"
                      :max="Math.max(1, (lesson.videoDurationSeconds ?? 1) - 1)"
                      step="1"
                      class="h-10 w-full rounded-lg border border-[#ddd4d3] bg-white px-3 text-xs font-bold outline-none focus:border-[#c9152b] focus:ring-3 focus:ring-[#c9152b]/10"
                    />
                    <p class="mt-1.5 text-[10px] leading-4 text-[#8d7c7a]">
                      Ví dụ 90 = 01:30. Mốc phải nằm bên trong thời lượng video.
                    </p>
                  </label>

                  <div
                    v-if="!lesson.video || !lesson.videoDurationSeconds"
                    class="mt-3 rounded-lg border border-[#efd1a7] bg-[#fff8ed] px-3 py-2 text-[10px] leading-4 text-[#7b5521]"
                  >
                    Muốn dùng checkpoint? Quay lại <RouterLink :to="`/admin/lessons/${lesson.id}/edit`" class="font-black underline">Chỉnh sửa bài học</RouterLink>, thêm video và nhập thời lượng tin cậy trước.
                  </div>
                </fieldset>

                <!-- Type + order -->
                <div class="grid grid-cols-2 gap-3">
                  <label class="block">
                    <span class="mb-2 block text-xs font-bold text-[#4a403f]">Loại câu hỏi</span>
                    <div class="relative">
                      <select
                        v-model="form.type"
                        class="h-10 w-full appearance-none rounded-lg border border-[#ddd4d3] bg-white pl-3 pr-8 text-xs font-bold text-[#403735] outline-none focus:border-[#c9152b] focus:ring-3 focus:ring-[#c9152b]/10"
                        @change="onTypeChange"
                      >
                        <option value="MULTIPLE_CHOICE">Nhiều lựa chọn</option>
                        <option value="TRUE_FALSE">Đúng / Sai</option>
                      </select>
                      <svg class="pointer-events-none absolute right-2.5 top-1/2 size-3.5 -translate-y-1/2 text-[#8d7c7a]" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <path d="m7 10 5 5 5-5" />
                      </svg>
                    </div>
                  </label>

                  <label class="block">
                    <span class="mb-2 block text-xs font-bold text-[#4a403f]">Thứ tự</span>
                    <input
                      v-model.number="form.sortOrder"
                      required
                      min="0"
                      step="1"
                      type="number"
                      class="h-10 w-full rounded-lg border border-[#ddd4d3] bg-white px-3 text-xs font-bold text-[#403735] outline-none focus:border-[#c9152b] focus:ring-3 focus:ring-[#c9152b]/10"
                    />
                  </label>
                </div>

                <!-- Question -->
                <label class="block">
                  <span class="mb-2 flex items-center justify-between gap-2 text-xs font-bold text-[#4a403f]">
                    <span>Nội dung câu hỏi <span class="text-[#c9152b]">*</span></span>
                    <span
                      class="text-[10px]"
                      :class="form.text.length > 2000 ? 'text-[#ba1a1a]' : 'text-[#a0918f]'"
                    >
                      {{ form.text.length }}/2000
                    </span>
                  </span>
                  <textarea
                    v-model="form.text"
                    required
                    maxlength="2000"
                    rows="4"
                    placeholder="Nhập nội dung câu hỏi…"
                    class="w-full resize-y rounded-lg border border-[#ddd4d3] bg-white px-3 py-2.5 text-sm leading-6 text-[#342d2d] outline-none placeholder:text-[#aa9b99] focus:border-[#c9152b] focus:ring-3 focus:ring-[#c9152b]/10"
                  />
                </label>

                <!-- Options -->
                <fieldset>
                  <div class="mb-2.5 flex items-end justify-between gap-3">
                    <legend class="text-xs font-bold text-[#4a403f]">Các lựa chọn</legend>
                    <span class="text-[10px] font-semibold text-[#958582]">
                      Chọn 1 đáp án đúng
                    </span>
                  </div>

                  <div class="space-y-2.5">
                    <div
                      v-for="(_, index) in form.options"
                      :key="`${form.type}-${index}`"
                      class="flex items-center gap-2"
                    >
                      <button
                        type="button"
                        class="grid size-5 shrink-0 place-items-center rounded-full border transition"
                        :class="
                          correctIndex === index
                            ? 'border-[#c9152b] bg-[#fff1f2]'
                            : 'border-[#cfc2c0] bg-white hover:border-[#c9152b]'
                        "
                        :aria-label="`Chọn lựa chọn ${optionLetter(index)} là đáp án đúng`"
                        @click="selectCorrect(index)"
                      >
                        <span
                          v-if="correctIndex === index"
                          class="size-2.5 rounded-full bg-[#c9152b]"
                        />
                      </button>

                      <div
                        class="flex min-w-0 flex-1 overflow-hidden rounded-lg border border-[#ddd4d3] bg-white focus-within:border-[#c9152b] focus-within:ring-3 focus-within:ring-[#c9152b]/10"
                      >
                        <span class="grid w-9 shrink-0 place-items-center border-r border-[#e4dcdb] bg-[#f3efee] text-xs font-black text-[#756765]">
                          {{ optionLetter(index) }}
                        </span>
                        <input
                          v-model="form.options[index]"
                          required
                          maxlength="1000"
                          :readonly="form.type === 'TRUE_FALSE'"
                          :placeholder="`Nội dung lựa chọn ${optionLetter(index)}…`"
                          class="h-10 min-w-0 flex-1 border-0 bg-white px-3 text-xs text-[#403735] outline-none focus:ring-0 read-only:bg-[#faf8f7]"
                        />
                      </div>

                      <button
                        v-if="form.type === 'MULTIPLE_CHOICE' && form.options.length > 2"
                        type="button"
                        class="grid size-8 shrink-0 place-items-center rounded-lg text-[#9a8583] hover:bg-[#fff1f2] hover:text-[#ba1a1a]"
                        :aria-label="`Xóa lựa chọn ${optionLetter(index)}`"
                        @click="removeOption(index)"
                      >
                        <svg class="size-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                          <path d="M6 6l12 12M18 6 6 18" />
                        </svg>
                      </button>
                    </div>
                  </div>

                  <button
                    v-if="form.type === 'MULTIPLE_CHOICE' && form.options.length < 4"
                    type="button"
                    class="mt-3 inline-flex items-center gap-1.5 text-xs font-bold text-[#a0001c] hover:underline"
                    @click="addOption"
                  >
                    <svg class="size-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M12 5v14M5 12h14" />
                    </svg>
                    Thêm lựa chọn
                  </button>

                  <p class="mt-2 text-[10px] leading-4 text-[#9a8987]">
                    Nhiều lựa chọn hỗ trợ 2–4 đáp án. Đúng/Sai luôn có đúng 2 đáp án.
                  </p>
                </fieldset>

                <!-- Explanation -->
                <label class="block">
                  <span class="mb-2 flex items-center justify-between gap-2 text-xs font-bold text-[#4a403f]">
                    <span>Giải thích <span class="font-medium text-[#9a8987]">(tùy chọn)</span></span>
                    <span
                      class="text-[10px]"
                      :class="form.explanation.length > 4000 ? 'text-[#ba1a1a]' : 'text-[#a0918f]'"
                    >
                      {{ form.explanation.length }}/4000
                    </span>
                  </span>
                  <textarea
                    v-model="form.explanation"
                    maxlength="4000"
                    rows="3"
                    placeholder="Giải thích vì sao đáp án trên là đúng…"
                    class="w-full resize-y rounded-lg border border-[#ddd4d3] bg-white px-3 py-2.5 text-xs leading-5 text-[#403735] outline-none placeholder:text-[#aa9b99] focus:border-[#c9152b] focus:ring-3 focus:ring-[#c9152b]/10"
                  />
                </label>

                <div
                  v-if="isEditing"
                  class="rounded-lg border border-[#eadfdf] bg-[#faf7f6] px-3 py-2.5 text-[10px] leading-4 text-[#786967]"
                >
                  Đang sửa câu hỏi hiện có. Hệ thống dùng version để chống ghi đè thay đổi đồng thời.
                </div>

                <div class="flex items-center justify-end gap-2 border-t border-[#ece5e4] pt-4">
                  <button
                    type="button"
                    :disabled="saving"
                    class="h-9 rounded-lg px-3.5 text-xs font-bold text-[#756765] hover:bg-[#f3efee] disabled:opacity-50"
                    @click="resetEditor"
                  >
                    Hủy
                  </button>

                  <button
                    type="submit"
                    :disabled="saving || !formValid"
                    class="inline-flex h-9 items-center gap-2 rounded-lg bg-[#c9152b] px-4 text-xs font-bold text-white hover:bg-[#a0001c] disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    <svg
                      v-if="saving"
                      class="size-3.5 animate-spin"
                      viewBox="0 0 24 24"
                      fill="none"
                      aria-hidden="true"
                    >
                      <circle class="opacity-30" cx="12" cy="12" r="9" stroke="currentColor" stroke-width="3" />
                      <path d="M21 12a9 9 0 0 0-9-9" stroke="currentColor" stroke-width="3" stroke-linecap="round" />
                    </svg>
                    {{ saving ? 'Đang lưu…' : isEditing ? 'Lưu thay đổi' : 'Lưu câu hỏi' }}
                  </button>
                </div>
              </form>
            </section>
          </aside>
        </div>
      </template>
    </template>
  </section>
</template>
<style scoped>
@media (min-width: 1280px) {
  .xl\:overflow-y-auto { scrollbar-width: thin; scrollbar-color: #d5c8c6 transparent; }
}
</style>