<script setup lang="ts">
import { computed, nextTick, onMounted, reactive, ref, watch } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'
import DurationInput from '@/components/DurationInput.vue'
import * as courseApi from '@/modules/courses/course.api'
import type { AdminCourseDetail } from '@/modules/courses/models/AdminCourseDetail'
import type { LessonStatus } from '@/types/LessonStatus'

import * as lessonApi from './lesson.api'
import type { AdminLesson } from './models/AdminLesson'
import type { AdminLessonListItem } from './models/AdminLessonListItem'

type EditableLessonStatus = Exclude<LessonStatus, 'ARCHIVED'>
type EditTab = 'content' | 'preview'

interface LessonFormState {
  title: string
  description: string
  contentHtml: string
  videoUrl: string
  videoDurationSeconds: number | null
  sortOrder: number
  status: EditableLessonStatus
}

const route = useRoute()
const router = useRouter()

const lessonId = computed(() => Number(route.params.lessonId))

const lesson = ref<AdminLesson | null>(null)
const course = ref<AdminCourseDetail | null>(null)
const courseLessons = ref<AdminLessonListItem[]>([])
const questionCount = ref(0)

const loading = ref(true)
const saving = ref(false)
const archiving = ref(false)
const problem = ref<ProblemDetails | null>(null)
const savedMessage = ref('')

const activeTab = ref<EditTab>('content')
const editor = ref<HTMLElement | null>(null)
const videoPreviewId = ref<string | null>(null)
const videoPreviewProblem = ref('')

const initialSnapshot = ref('')
const allowLeave = ref(false)

const form = reactive<LessonFormState>({
  title: '',
  description: '',
  contentHtml: '',
  videoUrl: '',
  videoDurationSeconds: null,
  sortOrder: 0,
  status: 'DRAFT',
})

const isArchived = computed(() => lesson.value?.status === 'ARCHIVED')

const titleValid = computed(() => {
  const value = form.title.trim()
  return value.length > 0 && value.length <= 200
})

const descriptionValid = computed(() => form.description.length <= 4000)

const sortOrderValid = computed(() => Number.isInteger(form.sortOrder) && form.sortOrder >= 0)

const orderConflict = computed(() =>
  courseLessons.value.some(
    (item) =>
      item.id !== lesson.value?.id &&
      item.status !== 'ARCHIVED' &&
      item.sortOrder === form.sortOrder,
  ),
)

const videoUrlValid = computed(() => {
  const value = form.videoUrl.trim()
  return value.length === 0 || parseYouTubeId(value) !== null
})

const videoDurationValid = computed(() => {
  if (!form.videoUrl.trim()) return form.videoDurationSeconds === null
  return (
    Number.isInteger(form.videoDurationSeconds) &&
    (form.videoDurationSeconds ?? 0) > 0 &&
    (form.videoDurationSeconds ?? 0) <= 43200
  )
})

const formValid = computed(
  () =>
    !isArchived.value &&
    titleValid.value &&
    descriptionValid.value &&
    sortOrderValid.value &&
    !orderConflict.value &&
    videoUrlValid.value &&
    videoDurationValid.value,
)

const lessonSetupSteps = computed(() => [
  {
    label: 'Nội dung',
    detail: titleValid.value ? 'Đã có tiêu đề' : 'Thiếu tiêu đề',
    done: titleValid.value,
    optional: false,
  },
  {
    label: 'Video',
    detail: form.videoUrl.trim()
      ? videoDurationValid.value
        ? 'Đã có thời lượng'
        : 'Thiếu thời lượng'
      : 'Không dùng video',
    done: !form.videoUrl.trim() || videoDurationValid.value,
    optional: true,
  },
  {
    label: 'Câu hỏi',
    detail: questionCount.value > 0 ? `${questionCount.value} câu đã tạo` : 'Nên thêm để kiểm tra',
    done: questionCount.value > 0,
    optional: true,
  },
  {
    label: 'Xuất bản',
    detail: form.status === 'PUBLISHED' ? 'Học viên có thể thấy' : 'Đang là bản nháp',
    done: form.status === 'PUBLISHED',
    optional: false,
  },
])

const hasChanges = computed(() => {
  if (!initialSnapshot.value || isArchived.value) return false
  return snapshotForm() !== initialSnapshot.value
})

const statusLabel = computed(() => {
  if (lesson.value?.status === 'ARCHIVED') return 'Đã lưu trữ'
  return form.status === 'PUBLISHED' ? 'Đã xuất bản' : 'Bản nháp'
})

const statusClass = computed(() => {
  if (lesson.value?.status === 'ARCHIVED') {
    return 'border-[#ddd5d4] bg-[#f2efee] text-[#766967]'
  }

  return form.status === 'PUBLISHED'
    ? 'border-[#b9dfcc] bg-[#eef9f3] text-[#16764f]'
    : 'border-[#efcfaa] bg-[#fff7ed] text-[#b76500]'
})

const canonicalVideoId = computed(() => parseYouTubeId(form.videoUrl.trim()))

const previewVideoId = computed(() => videoPreviewId.value ?? canonicalVideoId.value)

const safePreviewHtml = computed(() => sanitizePreviewHtml(form.contentHtml))

function snapshotForm(): string {
  return JSON.stringify({
    title: form.title,
    description: form.description,
    contentHtml: form.contentHtml,
    videoUrl: form.videoUrl,
    videoDurationSeconds: form.videoDurationSeconds,
    sortOrder: form.sortOrder,
    status: form.status,
  })
}

function parseYouTubeId(value: string): string | null {
  if (!value) return null

  try {
    const url = new URL(value.trim())
    if (url.protocol.toLowerCase() !== 'https:') return null

    const host = url.hostname.toLowerCase()
    let id: string | null = null

    if (host === 'youtu.be') {
      id = url.pathname.split('/').filter(Boolean)[0] ?? null
    } else if (host === 'youtube.com' || host === 'www.youtube.com' || host === 'm.youtube.com') {
      if (url.pathname.toLowerCase() === '/watch') {
        id = url.searchParams.get('v')
      } else {
        const segments = url.pathname.split('/').filter(Boolean)
        if (segments.length === 2 && (segments[0] === 'embed' || segments[0] === 'shorts')) {
          id = segments[1] ?? null
        }
      }
    }

    return id && /^[A-Za-z0-9_-]{11}$/.test(id) ? id : null
  } catch {
    return null
  }
}

function sanitizePreviewHtml(value: string): string {
  if (!value.trim()) return ''

  const parser = new DOMParser()
  const documentValue = parser.parseFromString(`<body>${value}</body>`, 'text/html')
  const allowedTags = new Set([
    'P',
    'BR',
    'STRONG',
    'EM',
    'UL',
    'OL',
    'LI',
    'H1',
    'H2',
    'H3',
    'H4',
    'H5',
    'H6',
    'PRE',
    'CODE',
    'BLOCKQUOTE',
    'A',
  ])

  const elements = Array.from(documentValue.body.querySelectorAll('*'))

  for (const element of elements) {
    if (!allowedTags.has(element.tagName)) {
      element.replaceWith(documentValue.createTextNode(element.textContent ?? ''))
      continue
    }

    for (const attribute of Array.from(element.attributes)) {
      const keep =
        element.tagName === 'A' &&
        (attribute.name.toLowerCase() === 'href' || attribute.name.toLowerCase() === 'title')

      if (!keep) {
        element.removeAttribute(attribute.name)
      }
    }

    if (element.tagName === 'A') {
      const href = element.getAttribute('href')?.trim() ?? ''
      if (!href.startsWith('https://') && !href.startsWith('mailto:')) {
        element.removeAttribute('href')
      } else {
        element.setAttribute('rel', 'noopener noreferrer')
      }
    }
  }

  return documentValue.body.innerHTML
}

function syncEditor(): void {
  form.contentHtml = editor.value?.innerHTML.trim() ?? ''
}

function execEditorCommand(command: string, value?: string): void {
  if (isArchived.value) return

  editor.value?.focus()
  document.execCommand(command, false, value)
  syncEditor()
}

function setBlock(tag: 'h3' | 'p' | 'blockquote' | 'pre'): void {
  execEditorCommand('formatBlock', tag)
}

function insertLink(): void {
  if (isArchived.value) return

  const value = window.prompt('Nhập liên kết HTTPS hoặc mailto:')
  if (!value) return

  const url = value.trim()
  if (!url.startsWith('https://') && !url.startsWith('mailto:')) {
    problem.value = {
      status: 400,
      code: 'INVALID_EDITOR_LINK',
      title: 'Liên kết không hợp lệ',
      detail: 'Nội dung bài học chỉ cho phép liên kết HTTPS hoặc mailto.',
    }
    return
  }

  execEditorCommand('createLink', url)
}

function pastePlainText(event: ClipboardEvent): void {
  if (isArchived.value) return

  event.preventDefault()
  const value = event.clipboardData?.getData('text/plain') ?? ''
  document.execCommand('insertText', false, value)
  syncEditor()
}

function loadVideoPreview(): void {
  videoPreviewProblem.value = ''
  videoPreviewId.value = null

  const value = form.videoUrl.trim()
  if (!value) return

  const id = parseYouTubeId(value)
  if (!id) {
    videoPreviewProblem.value =
      'Chỉ hỗ trợ URL YouTube HTTPS dạng watch, youtu.be, embed hoặc shorts.'
    return
  }

  videoPreviewId.value = id
}

function fillForm(data: AdminLesson): void {
  form.title = data.title
  form.description = data.description ?? ''
  form.contentHtml = data.contentHtml ?? ''
  form.videoUrl =
    data.video?.provider === 'YOUTUBE'
      ? `https://www.youtube.com/watch?v=${data.video.externalId}`
      : ''
  form.videoDurationSeconds = data.videoDurationSeconds
  form.sortOrder = data.sortOrder
  form.status = data.status === 'PUBLISHED' ? 'PUBLISHED' : 'DRAFT'
}

async function syncEditorFromForm(): Promise<void> {
  await nextTick()
  if (editor.value) {
    editor.value.innerHTML = form.contentHtml
  }
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
  savedMessage.value = ''
  videoPreviewProblem.value = ''
  videoPreviewId.value = null

  try {
    const requestedId = lessonId.value
    const lessonData = await lessonApi.getAdminLesson(requestedId)

    if (lessonId.value !== requestedId) return

    const [courseData, listData, questionData] = await Promise.all([
      courseApi.getAdminCourse(lessonData.courseId),
      lessonApi.getAdminLessons(lessonData.courseId),
      lessonApi.getQuestions(requestedId),
    ])

    if (lessonId.value !== requestedId) return

    lesson.value = lessonData
    course.value = courseData
    courseLessons.value = listData
    questionCount.value = questionData.length

    fillForm(lessonData)
    await syncEditorFromForm()
    initialSnapshot.value = snapshotForm()
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}

async function reloadLatest(): Promise<void> {
  initialSnapshot.value = ''
  await load()
}

async function save(): Promise<void> {
  if (!lesson.value || saving.value || archiving.value || !formValid.value) return

  syncEditor()
  saving.value = true
  problem.value = null
  savedMessage.value = ''

  try {
    const updated = await lessonApi.updateLesson(lesson.value.id, {
      title: form.title.trim(),
      description: form.description.trim() || null,
      contentHtml: form.contentHtml.trim() || null,
      videoUrl: form.videoUrl.trim() || null,
      videoDurationSeconds: form.videoUrl.trim() ? form.videoDurationSeconds : null,
      sortOrder: form.sortOrder,
      status: form.status,
      version: lesson.value.version,
    })

    lesson.value = updated
    fillForm(updated)
    await syncEditorFromForm()
    initialSnapshot.value = snapshotForm()
    savedMessage.value = 'Đã lưu thay đổi bài học.'
  } catch (error) {
    problem.value = toProblem(error)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    saving.value = false
  }
}

async function archiveLesson(): Promise<void> {
  if (!lesson.value || isArchived.value || saving.value || archiving.value) return

  const confirmed = window.confirm(
    `Lưu trữ bài học "${lesson.value.title}"?\n\nBài học sẽ bị ẩn khỏi học viên. MVP hiện chưa có chức năng khôi phục lesson đã lưu trữ.`,
  )

  if (!confirmed) return

  archiving.value = true
  problem.value = null

  try {
    const courseId = lesson.value.courseId
    await lessonApi.archiveLesson(lesson.value.id)
    allowLeave.value = true
    await router.replace(`/admin/courses/${courseId}`)
  } catch (error) {
    problem.value = toProblem(error)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    archiving.value = false
  }
}

function cancel(): void {
  if (!lesson.value || saving.value || archiving.value) return
  void router.push(`/admin/courses/${lesson.value.courseId}`)
}

watch(
  () => form.videoUrl,
  () => {
    videoPreviewId.value = null
    videoPreviewProblem.value = ''
    if (!form.videoUrl.trim()) form.videoDurationSeconds = null
  },
)

watch(
  () => route.params.lessonId,
  () => {
    void load()
  },
)

onBeforeRouteLeave(() => {
  if (allowLeave.value || !hasChanges.value || saving.value || archiving.value) return true
  return window.confirm('Bạn có thay đổi chưa lưu. Bạn có chắc muốn rời khỏi trang?')
})

onMounted(load)
</script>

<template>
  <section>
    <!-- Loading -->
    <div v-if="loading" class="space-y-6">
      <div class="flex justify-between gap-6">
        <div>
          <div class="h-4 w-28 animate-pulse rounded bg-[#eae3e2]" />
          <div class="mt-2 h-8 w-80 animate-pulse rounded bg-[#e7dfde]" />
        </div>
        <div class="h-10 w-32 animate-pulse rounded-lg bg-[#eee8e7]" />
      </div>

      <div class="grid gap-6 xl:grid-cols-[minmax(0,2fr)_360px]">
        <div class="h-[650px] animate-pulse rounded-2xl bg-[#eee8e7]" />
        <div class="space-y-6">
          <div class="h-80 animate-pulse rounded-2xl bg-[#eee8e7]" />
          <div class="h-52 animate-pulse rounded-2xl bg-[#eee8e7]" />
        </div>
      </div>
    </div>

    <template v-else>
      <ProblemAlert class="mb-5" :problem="problem" />

      <div
        v-if="problem?.status === 409 && problem?.code === 'CONCURRENCY_CONFLICT'"
        class="mb-5 flex flex-col justify-between gap-3 rounded-xl border border-[#efd1a7] bg-[#fff8ed] px-4 py-3 sm:flex-row sm:items-center"
      >
        <p class="text-sm leading-6 text-[#7b5521]">
          Bài học đã được cập nhật ở nơi khác. Hãy tải lại phiên bản mới nhất trước khi tiếp tục
          chỉnh sửa.
        </p>

        <button
          type="button"
          class="h-9 shrink-0 rounded-lg border border-[#d8b57f] bg-white px-3 text-xs font-bold text-[#8c5b16] hover:bg-[#fffaf3]"
          @click="reloadLatest"
        >
          Tải lại dữ liệu
        </button>
      </div>

      <div
        v-if="savedMessage"
        class="mb-5 flex items-center gap-3 rounded-xl border border-[#b9dfcc] bg-[#f0faf5] px-4 py-3 text-sm font-bold text-[#245b43]"
        role="status"
      >
        <svg
          class="size-5 shrink-0 text-[#16875f]"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2.2"
          aria-hidden="true"
        >
          <path d="m6 12 4 4 8-8" />
        </svg>
        {{ savedMessage }}
      </div>

      <template v-if="lesson && course">
        <!-- Archived notice -->
        <div
          v-if="isArchived"
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
              Nội dung được giữ để bảo toàn lịch sử học tập. Backend hiện không hỗ trợ kích hoạt lại
              lesson đã lưu trữ, vì vậy trang đang ở chế độ chỉ đọc.
            </p>
          </div>
        </div>

        <!-- Heading -->
        <header
          class="mb-6 flex flex-col justify-between gap-5 border-b border-[#e4dcdb] pb-5 lg:flex-row lg:items-end"
        >
          <div class="min-w-0">
            <nav
              aria-label="Breadcrumb"
              class="mb-3 flex flex-wrap items-center gap-2 text-xs font-semibold text-[#8d7d7b]"
            >
              <RouterLink to="/admin/courses" class="hover:text-[#a0001c]"> Khóa học </RouterLink>

              <svg
                class="size-3.5 text-[#b8aaa8]"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                aria-hidden="true"
              >
                <path d="m9 18 6-6-6-6" />
              </svg>

              <RouterLink
                :to="`/admin/courses/${course.id}`"
                class="max-w-[340px] truncate hover:text-[#a0001c]"
              >
                {{ course.title }}
              </RouterLink>

              <svg
                class="size-3.5 text-[#b8aaa8]"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                aria-hidden="true"
              >
                <path d="m9 18 6-6-6-6" />
              </svg>

              <span class="text-[#403735]">Chỉnh sửa bài học</span>
            </nav>

            <div class="flex flex-wrap items-center gap-3">
              <span class="text-xs font-bold uppercase tracking-[0.14em] text-[#95827f]">
                Bài {{ form.sortOrder }}
              </span>

              <span
                class="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-bold"
                :class="statusClass"
              >
                <span class="size-1.5 rounded-full bg-current opacity-75" />
                {{ statusLabel }}
              </span>
            </div>

            <h1
              class="mt-2 max-w-4xl text-[29px] font-black leading-tight tracking-[-0.03em] text-[#1c1b1b] sm:text-[34px]"
            >
              {{ lesson.title }}
            </h1>
          </div>

          <RouterLink
            :to="`/admin/lessons/${lesson.id}/exercises`"
            class="inline-flex h-10 shrink-0 items-center justify-center gap-2 self-start rounded-lg border border-[#d6a4aa] bg-white px-4 text-sm font-bold text-[#a0001c] transition hover:bg-[#fff3f4] lg:self-auto"
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
            Câu hỏi & kiểm tra
            <span
              class="rounded-md bg-[#fff0f1] px-1.5 py-0.5 text-[10px] font-black text-[#a0001c]"
            >
              {{ questionCount }}
            </span>
          </RouterLink>
        </header>

        <section
          v-if="!isArchived"
          class="mb-5 rounded-2xl border border-[#eadfdf] bg-[#fffdfc] p-4 sm:p-5"
          aria-label="Quy trình thiết lập bài học"
        >
          <div class="flex flex-col justify-between gap-4 lg:flex-row lg:items-center">
            <div class="min-w-0 flex-1">
              <div class="flex flex-wrap items-center gap-2">
                <p class="text-[10px] font-black uppercase tracking-[0.15em] text-[#a0001c]">
                  Thiết lập bài học
                </p>
                <span class="text-[10px] font-semibold text-[#9a8987]">
                  Làm từ trái sang phải, không cần nhớ quy trình
                </span>
              </div>

              <div class="mt-3 grid gap-2 sm:grid-cols-2 xl:grid-cols-4">
                <div
                  v-for="(step, index) in lessonSetupSteps"
                  :key="step.label"
                  class="rounded-lg border px-3 py-2.5"
                  :class="
                    step.done
                      ? 'border-[#cfe4d8] bg-[#f4faf7]'
                      : step.optional
                        ? 'border-[#e5dedd] bg-white'
                        : 'border-[#efc7cb] bg-[#fff7f7]'
                  "
                >
                  <div class="flex items-center gap-2">
                    <span
                      class="grid size-5 shrink-0 place-items-center rounded-full text-[10px] font-black"
                      :class="
                        step.done
                          ? 'bg-[#16875b] text-white'
                          : step.optional
                            ? 'bg-[#f0eceb] text-[#8d7c7a]'
                            : 'bg-[#fff0f1] text-[#a0001c]'
                      "
                    >
                      {{ step.done ? '✓' : index + 1 }}
                    </span>
                    <strong class="text-[11px] text-[#4f4442]">{{ step.label }}</strong>
                    <span
                      v-if="step.optional"
                      class="ml-auto text-[9px] font-bold uppercase tracking-wide text-[#a0918f]"
                    >
                      tùy chọn
                    </span>
                  </div>
                  <p class="mt-1.5 pl-7 text-[10px] leading-4 text-[#81716f]">{{ step.detail }}</p>
                </div>
              </div>
            </div>

            <RouterLink
              :to="`/admin/lessons/${lesson.id}/exercises`"
              class="inline-flex h-10 shrink-0 items-center justify-center gap-2 rounded-lg border border-[#d7a8ad] bg-white px-4 text-xs font-bold text-[#a0001c] transition hover:bg-[#fff4f5]"
            >
              Thiết lập câu hỏi
              <svg
                class="size-3.5"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
              >
                <path d="M5 12h14M14 7l5 5-5 5" />
              </svg>
            </RouterLink>
          </div>
        </section>

        <form @submit.prevent="save">
          <div class="grid gap-6 xl:grid-cols-[minmax(0,2fr)_360px] xl:items-start">
            <!-- Main -->
            <div class="min-w-0">
              <!-- Tabs -->
              <div class="mb-4 flex border-b border-[#e3dad9]">
                <button
                  type="button"
                  class="-mb-px h-11 border-b-2 px-5 text-sm font-bold transition"
                  :class="
                    activeTab === 'content'
                      ? 'border-[#a0001c] text-[#a0001c]'
                      : 'border-transparent text-[#81716f] hover:text-[#5f5351]'
                  "
                  @click="activeTab = 'content'"
                >
                  Nội dung
                </button>

                <button
                  type="button"
                  class="-mb-px h-11 border-b-2 px-5 text-sm font-bold transition"
                  :class="
                    activeTab === 'preview'
                      ? 'border-[#a0001c] text-[#a0001c]'
                      : 'border-transparent text-[#81716f] hover:text-[#5f5351]'
                  "
                  @click="activeTab = 'preview'"
                >
                  Xem trước
                </button>
              </div>

              <!-- Content editor -->
              <section
                v-show="activeTab === 'content'"
                class="overflow-hidden rounded-2xl border border-[#e5dedd] bg-white shadow-[0_2px_12px_rgba(50,25,25,0.025)]"
              >
                <div class="space-y-6 p-5 sm:p-7">
                  <label class="block">
                    <span
                      class="mb-2 flex items-center justify-between gap-3 text-sm font-bold text-[#403735]"
                    >
                      <span>Tiêu đề bài học <span class="text-[#c9152b]">*</span></span>
                      <span
                        class="text-[11px] font-semibold"
                        :class="form.title.length > 200 ? 'text-[#ba1a1a]' : 'text-[#a0918f]'"
                      >
                        {{ form.title.length }}/200
                      </span>
                    </span>

                    <input
                      v-model="form.title"
                      required
                      maxlength="200"
                      autocomplete="off"
                      :disabled="isArchived"
                      class="h-11 w-full rounded-xl border border-[#ddd4d3] bg-white px-3.5 text-sm text-[#302929] outline-none focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10 disabled:cursor-not-allowed disabled:bg-[#f4f1f0] disabled:text-[#877976]"
                    />
                  </label>

                  <label class="block">
                    <span
                      class="mb-2 flex items-center justify-between gap-3 text-sm font-bold text-[#403735]"
                    >
                      <span>Mô tả ngắn</span>
                      <span
                        class="text-[11px] font-semibold"
                        :class="
                          form.description.length > 4000 ? 'text-[#ba1a1a]' : 'text-[#a0918f]'
                        "
                      >
                        {{ form.description.length }}/4000
                      </span>
                    </span>

                    <textarea
                      v-model="form.description"
                      maxlength="4000"
                      rows="3"
                      :disabled="isArchived"
                      class="w-full resize-y rounded-xl border border-[#ddd4d3] bg-white px-3.5 py-3 text-sm leading-6 text-[#302929] outline-none focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10 disabled:cursor-not-allowed disabled:bg-[#f4f1f0] disabled:text-[#877976]"
                    />
                  </label>

                  <!-- Video -->
                  <div>
                    <label
                      class="mb-2 block text-sm font-bold text-[#403735]"
                      for="lesson-video-url"
                    >
                      Video URL
                    </label>

                    <div class="flex flex-col gap-2 sm:flex-row">
                      <input
                        id="lesson-video-url"
                        v-model="form.videoUrl"
                        type="url"
                        :disabled="isArchived"
                        placeholder="https://www.youtube.com/watch?v=..."
                        class="h-11 min-w-0 flex-1 rounded-xl border bg-white px-3.5 text-sm text-[#302929] outline-none placeholder:text-[#aa9b99] focus:ring-4 disabled:cursor-not-allowed disabled:bg-[#f4f1f0]"
                        :class="
                          videoUrlValid
                            ? 'border-[#ddd4d3] focus:border-[#c9152b] focus:ring-[#c9152b]/10'
                            : 'border-[#d95b68] focus:border-[#ba1a1a] focus:ring-[#ba1a1a]/10'
                        "
                      />

                      <button
                        type="button"
                        :disabled="isArchived || !form.videoUrl.trim()"
                        class="h-11 shrink-0 rounded-xl border border-[#d9cfce] bg-[#f7f4f3] px-4 text-xs font-bold text-[#665957] transition hover:bg-[#eee9e8] disabled:cursor-not-allowed disabled:opacity-50"
                        @click="loadVideoPreview"
                      >
                        Kiểm tra
                      </button>
                    </div>

                    <p v-if="videoPreviewProblem" class="mt-2 text-xs leading-5 text-[#ba1a1a]">
                      {{ videoPreviewProblem }}
                    </p>
                    <p v-else class="mt-2 text-xs leading-5 text-[#8d7c7a]">
                      Chỉ hỗ trợ YouTube HTTPS. Backend sẽ chuẩn hóa video trước khi lưu.
                    </p>

                    <div v-if="form.videoUrl.trim()" class="mt-4 block max-w-sm">
                      <span class="mb-2 block text-xs font-bold text-[#403735]"
                        >Thời lượng video tin cậy</span
                      >
                      <DurationInput v-model="form.videoDurationSeconds" :disabled="isArchived" />
                      <p class="mt-1.5 text-[11px] leading-5 text-[#8d7c7a]">
                        Giá trị này là mốc server dùng để chống tua và khóa bài tập củng cố cho tới
                        khi video kết thúc.
                      </p>
                    </div>

                    <div
                      v-if="previewVideoId"
                      class="mt-4 aspect-video overflow-hidden rounded-xl border border-[#e0d7d6] bg-[#f3efee]"
                    >
                      <iframe
                        class="size-full border-0"
                        :src="`https://www.youtube-nocookie.com/embed/${encodeURIComponent(previewVideoId)}`"
                        title="Xem trước video bài học"
                        loading="lazy"
                        allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                        allowfullscreen
                      />
                    </div>
                  </div>

                  <!-- Rich text -->
                  <div>
                    <div class="mb-2 flex items-end justify-between gap-3">
                      <div>
                        <p class="text-sm font-bold text-[#403735]">Nội dung chi tiết</p>
                        <p class="mt-1 text-xs leading-5 text-[#8d7c7a]">
                          Nội dung HTML được backend sanitize trước khi lưu.
                        </p>
                      </div>
                    </div>

                    <div class="overflow-hidden rounded-xl border border-[#ddd4d3]">
                      <div
                        class="flex flex-wrap items-center gap-1 border-b border-[#e8e0df] bg-[#faf8f7] px-3 py-2"
                        aria-label="Công cụ định dạng"
                      >
                        <button
                          type="button"
                          class="editor-tool"
                          :disabled="isArchived"
                          @click="setBlock('h3')"
                        >
                          H3
                        </button>
                        <button
                          type="button"
                          class="editor-tool"
                          :disabled="isArchived"
                          @click="setBlock('p')"
                        >
                          P
                        </button>
                        <span class="mx-1 h-5 w-px bg-[#ded5d4]" />

                        <button
                          type="button"
                          class="editor-tool font-black"
                          :disabled="isArchived"
                          @click="execEditorCommand('bold')"
                        >
                          B
                        </button>
                        <button
                          type="button"
                          class="editor-tool italic"
                          :disabled="isArchived"
                          @click="execEditorCommand('italic')"
                        >
                          I
                        </button>

                        <span class="mx-1 h-5 w-px bg-[#ded5d4]" />

                        <button
                          type="button"
                          class="editor-tool"
                          :disabled="isArchived"
                          @click="execEditorCommand('insertUnorderedList')"
                        >
                          • List
                        </button>
                        <button
                          type="button"
                          class="editor-tool"
                          :disabled="isArchived"
                          @click="execEditorCommand('insertOrderedList')"
                        >
                          1. List
                        </button>

                        <span class="mx-1 h-5 w-px bg-[#ded5d4]" />

                        <button
                          type="button"
                          class="editor-tool"
                          :disabled="isArchived"
                          @click="insertLink"
                        >
                          Link
                        </button>
                        <button
                          type="button"
                          class="editor-tool"
                          :disabled="isArchived"
                          @click="setBlock('blockquote')"
                        >
                          Quote
                        </button>
                        <button
                          type="button"
                          class="editor-tool"
                          :disabled="isArchived"
                          @click="setBlock('pre')"
                        >
                          Code
                        </button>
                      </div>

                      <div
                        ref="editor"
                        class="lesson-editor min-h-[330px] bg-white p-5 text-sm leading-7 text-[#342d2d] outline-none"
                        :class="isArchived ? 'pointer-events-none bg-[#f7f4f3] text-[#7d706e]' : ''"
                        :contenteditable="isArchived ? 'false' : 'true'"
                        role="textbox"
                        aria-multiline="true"
                        data-placeholder="Bắt đầu soạn nội dung bài học…"
                        @input="syncEditor"
                        @blur="syncEditor"
                        @paste="pastePlainText"
                      />
                    </div>
                  </div>
                </div>
              </section>

              <!-- Preview -->
              <section
                v-show="activeTab === 'preview'"
                class="overflow-hidden rounded-2xl border border-[#e5dedd] bg-white shadow-[0_2px_12px_rgba(50,25,25,0.025)]"
              >
                <div class="border-b border-[#eee7e6] px-5 py-5 sm:px-7">
                  <p class="text-[10px] font-black uppercase tracking-[0.16em] text-[#9b8583]">
                    Chế độ xem trước
                  </p>
                  <h2 class="mt-1 text-xl font-black tracking-[-0.02em] text-[#302929]">
                    {{ form.title.trim() || 'Bài học chưa có tiêu đề' }}
                  </h2>

                  <p v-if="form.description.trim()" class="mt-2 text-sm leading-6 text-[#756765]">
                    {{ form.description }}
                  </p>
                </div>

                <div class="space-y-7 p-5 sm:p-7">
                  <div
                    v-if="previewVideoId"
                    class="aspect-video overflow-hidden rounded-xl border border-[#e1d8d7] bg-[#20191a]"
                  >
                    <iframe
                      class="size-full border-0"
                      :src="`https://www.youtube-nocookie.com/embed/${encodeURIComponent(previewVideoId)}`"
                      title="Video bài học"
                      loading="lazy"
                      allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                      allowfullscreen
                    />
                  </div>

                  <!-- eslint-disable vue/no-v-html -->
                  <div
                    v-if="safePreviewHtml"
                    class="lesson-preview text-sm leading-7 text-[#342d2d]"
                    v-html="safePreviewHtml"
                  />
                  <!-- eslint-enable vue/no-v-html -->

                  <div
                    v-else
                    class="flex min-h-[240px] flex-col items-center justify-center rounded-xl border border-dashed border-[#d9cecd] bg-[#faf8f7] px-6 text-center"
                  >
                    <svg
                      class="size-8 text-[#a3918f]"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="1.6"
                      aria-hidden="true"
                    >
                      <path d="M5 4h14v16H5z" />
                      <path d="M8 8h8M8 12h8M8 16h5" />
                    </svg>
                    <p class="mt-3 text-sm font-bold text-[#665957]">Chưa có nội dung chi tiết</p>
                    <p class="mt-1 text-xs leading-5 text-[#93827f]">
                      Quay lại tab Nội dung để bắt đầu soạn bài học.
                    </p>
                  </div>
                </div>
              </section>
            </div>

            <!-- Metadata sidebar -->
            <aside class="space-y-6 xl:sticky xl:top-24 xl:self-start">
              <section
                class="rounded-2xl border border-[#e5dedd] bg-white p-5 shadow-[0_6px_24px_rgba(53,27,31,0.04)] sm:p-6"
              >
                <h2 class="text-lg font-black tracking-[-0.02em] text-[#302929]">
                  Thông tin bài học
                </h2>

                <div class="mt-5 divide-y divide-[#eee7e6]">
                  <div class="py-4 first:pt-0">
                    <p class="text-[10px] font-bold uppercase tracking-[0.14em] text-[#9b8987]">
                      Khóa học
                    </p>

                    <RouterLink
                      :to="`/admin/courses/${course.id}`"
                      class="mt-1.5 block text-sm font-black leading-5 text-[#4a403f] transition hover:text-[#a0001c]"
                    >
                      {{ course.title }}
                    </RouterLink>
                  </div>

                  <label class="flex items-center justify-between gap-4 py-4">
                    <span class="text-sm font-bold text-[#625654]">Thứ tự</span>

                    <input
                      v-model.number="form.sortOrder"
                      type="number"
                      min="0"
                      step="1"
                      required
                      :disabled="isArchived"
                      class="h-9 w-24 rounded-lg border bg-white px-2 text-center text-sm font-black text-[#403735] outline-none focus:ring-3 disabled:cursor-not-allowed disabled:bg-[#f3efee]"
                      :class="
                        !sortOrderValid || orderConflict
                          ? 'border-[#d95b68] focus:border-[#ba1a1a] focus:ring-[#ba1a1a]/10'
                          : 'border-[#ddd4d3] focus:border-[#c9152b] focus:ring-[#c9152b]/10'
                      "
                    />
                  </label>

                  <p
                    v-if="orderConflict"
                    class="-mt-2 pb-3 text-right text-[10px] font-semibold leading-4 text-[#ba1a1a]"
                  >
                    Thứ tự này đang được lesson khác sử dụng.
                  </p>

                  <label class="flex items-center justify-between gap-4 py-4">
                    <span class="text-sm font-bold text-[#625654]">Trạng thái</span>

                    <select
                      v-if="!isArchived"
                      v-model="form.status"
                      class="h-9 rounded-lg border border-[#ddd4d3] bg-white px-2.5 text-xs font-bold text-[#403735] outline-none focus:border-[#c9152b] focus:ring-3 focus:ring-[#c9152b]/10"
                    >
                      <option value="PUBLISHED">Đã xuất bản</option>
                      <option value="DRAFT">Bản nháp</option>
                    </select>

                    <span
                      v-else
                      class="rounded-lg border border-[#ddd5d4] bg-[#f2efee] px-2.5 py-1.5 text-xs font-bold text-[#766967]"
                    >
                      Đã lưu trữ
                    </span>
                  </label>

                  <div class="flex items-center justify-between gap-4 py-4">
                    <span class="text-sm font-bold text-[#625654]">Version</span>
                    <span class="text-xs font-black text-[#827270]">{{ lesson.version }}</span>
                  </div>
                </div>

                <RouterLink
                  :to="`/admin/lessons/${lesson.id}/exercises`"
                  class="mt-2 inline-flex h-10 w-full items-center justify-center gap-2 rounded-lg border border-[#ddd4d3] bg-[#faf8f7] px-3 text-xs font-bold text-[#665957] transition hover:border-[#d7a7ac] hover:bg-[#fff4f5] hover:text-[#a0001c]"
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
                  Quản lý câu hỏi
                  <span class="rounded-md bg-[#f1edec] px-1.5 py-0.5 text-[10px] font-black">
                    {{ questionCount }}
                  </span>
                </RouterLink>
              </section>

              <!-- Danger zone -->
              <section
                v-if="!isArchived"
                class="rounded-2xl border border-[#efc7cb] bg-[#fff7f7] p-5 sm:p-6"
              >
                <h2 class="text-lg font-black tracking-[-0.02em] text-[#a22435]">Vùng nguy hiểm</h2>
                <p class="mt-2 text-xs leading-5 text-[#786967]">
                  Lưu trữ sẽ ẩn bài học khỏi học viên và loại nó khỏi learning path. MVP hiện chưa
                  có API khôi phục lesson đã lưu trữ.
                </p>

                <button
                  type="button"
                  :disabled="saving || archiving"
                  class="mt-5 inline-flex h-10 w-full items-center justify-center gap-2 rounded-lg border border-[#d9858f] bg-white px-4 text-sm font-bold text-[#a22435] transition hover:bg-[#fff0f1] disabled:cursor-not-allowed disabled:opacity-50"
                  @click="archiveLesson"
                >
                  <svg
                    class="size-4"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="1.8"
                    aria-hidden="true"
                  >
                    <path d="M4 7h16M6 7v13h12V7M8 4h8" />
                  </svg>
                  {{ archiving ? 'Đang lưu trữ…' : 'Lưu trữ bài học' }}
                </button>
              </section>
            </aside>
          </div>

          <!-- Sticky actions -->
          <div
            v-if="!isArchived"
            class="sticky bottom-0 z-20 -mx-4 -mb-4 mt-7 border-t border-[#e4dcdb] bg-[#f8f7f5]/95 px-4 py-4 backdrop-blur sm:-mx-5 sm:-mb-5 sm:px-5 lg:-mx-6 lg:px-6"
          >
            <div
              class="mx-auto flex max-w-[1360px] flex-col-reverse gap-3 sm:flex-row sm:items-center sm:justify-between"
            >
              <p
                class="text-xs font-semibold"
                :class="hasChanges ? 'text-[#a0001c]' : 'text-[#988784]'"
              >
                {{ hasChanges ? 'Bạn có thay đổi chưa lưu' : 'Không có thay đổi mới' }}
              </p>

              <div class="flex flex-col-reverse gap-3 sm:flex-row">
                <button
                  type="button"
                  :disabled="saving || archiving"
                  class="h-10 rounded-lg px-5 text-sm font-bold text-[#6e605e] transition hover:bg-[#eee9e8] disabled:cursor-not-allowed disabled:opacity-50"
                  @click="cancel"
                >
                  Hủy thay đổi
                </button>

                <button
                  type="submit"
                  :disabled="saving || archiving || !formValid || !hasChanges"
                  class="inline-flex h-10 items-center justify-center gap-2 rounded-lg bg-[#c9152b] px-5 text-sm font-bold text-white shadow-[0_6px_16px_rgba(160,0,28,0.14)] transition hover:bg-[#a0001c] disabled:cursor-not-allowed disabled:opacity-50"
                >
                  <svg
                    v-if="saving"
                    class="size-4 animate-spin"
                    viewBox="0 0 24 24"
                    fill="none"
                    aria-hidden="true"
                  >
                    <circle
                      class="opacity-30"
                      cx="12"
                      cy="12"
                      r="9"
                      stroke="currentColor"
                      stroke-width="3"
                    />
                    <path
                      d="M21 12a9 9 0 0 0-9-9"
                      stroke="currentColor"
                      stroke-width="3"
                      stroke-linecap="round"
                    />
                  </svg>

                  <svg
                    v-else
                    class="size-4"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="1.9"
                    aria-hidden="true"
                  >
                    <path d="M5 4h12l2 2v14H5z" />
                    <path d="M8 4v6h8V4M8 20v-6h8v6" />
                  </svg>

                  {{ saving ? 'Đang lưu…' : 'Lưu bài học' }}
                </button>
              </div>
            </div>
          </div>
        </form>
      </template>
    </template>
  </section>
</template>

<style scoped>
.editor-tool {
  min-width: 2rem;
  height: 2rem;
  border-radius: 0.5rem;
  padding: 0 0.55rem;
  font-size: 0.72rem;
  font-weight: 700;
  color: #665957;
  transition:
    background-color 150ms ease,
    color 150ms ease,
    opacity 150ms ease;
}

.editor-tool:hover:not(:disabled) {
  background: #f0e9e8;
  color: #a0001c;
}

.editor-tool:disabled {
  cursor: not-allowed;
  opacity: 0.4;
}

.lesson-editor:empty::before {
  content: attr(data-placeholder);
  color: #a2918f;
  font-style: italic;
  pointer-events: none;
}

.lesson-editor :deep(h1),
.lesson-editor :deep(h2),
.lesson-editor :deep(h3),
.lesson-editor :deep(h4),
.lesson-editor :deep(h5),
.lesson-editor :deep(h6),
.lesson-preview :deep(h1),
.lesson-preview :deep(h2),
.lesson-preview :deep(h3),
.lesson-preview :deep(h4),
.lesson-preview :deep(h5),
.lesson-preview :deep(h6) {
  margin: 1.1rem 0 0.55rem;
  font-weight: 800;
  line-height: 1.35;
  color: #302929;
}

.lesson-editor :deep(h3),
.lesson-preview :deep(h3) {
  font-size: 1.15rem;
}

.lesson-editor :deep(p),
.lesson-preview :deep(p) {
  margin: 0.6rem 0;
}

.lesson-editor :deep(ul),
.lesson-preview :deep(ul) {
  margin: 0.7rem 0;
  list-style: disc;
  padding-left: 1.4rem;
}

.lesson-editor :deep(ol),
.lesson-preview :deep(ol) {
  margin: 0.7rem 0;
  list-style: decimal;
  padding-left: 1.4rem;
}

.lesson-editor :deep(blockquote),
.lesson-preview :deep(blockquote) {
  margin: 0.9rem 0;
  border-left: 3px solid #dec5c8;
  background: #faf6f6;
  padding: 0.75rem 1rem;
  color: #655856;
}

.lesson-editor :deep(pre),
.lesson-preview :deep(pre) {
  margin: 0.9rem 0;
  overflow-x: auto;
  border: 1px solid #e3d9d8;
  border-radius: 0.65rem;
  background: #f4f1f0;
  padding: 0.9rem 1rem;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 0.8rem;
  line-height: 1.6;
}

.lesson-editor :deep(a),
.lesson-preview :deep(a) {
  color: #a0001c;
  text-decoration: underline;
}
</style>
