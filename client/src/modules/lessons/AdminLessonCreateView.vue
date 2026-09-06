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
import type { AdminLessonListItem } from './models/AdminLessonListItem'

type CreateLessonStatus = Exclude<LessonStatus, 'ARCHIVED'>
type SavingAction = 'draft' | 'create' | null

interface LessonFormState {
  title: string
  description: string
  contentHtml: string
  videoUrl: string
  videoDurationSeconds: number | null
  sortOrder: number
  status: CreateLessonStatus
}

interface StructureItem {
  key: string
  title: string
  sortOrder: number
  kind: 'lesson' | 'creating'
  status?: LessonStatus
}

const route = useRoute()
const router = useRouter()

const courseId = computed(() => Number(route.params.courseId))

const course = ref<AdminCourseDetail | null>(null)
const lessons = ref<AdminLessonListItem[]>([])
const loading = ref(true)
const savingAction = ref<SavingAction>(null)
const problem = ref<ProblemDetails | null>(null)

const editor = ref<HTMLElement | null>(null)
const previewVideoId = ref<string | null>(null)
const previewProblem = ref('')
const allowLeave = ref(false)
const initialSnapshot = ref('')

const form = reactive<LessonFormState>({
  title: '',
  description: '',
  contentHtml: '',
  videoUrl: '',
  videoDurationSeconds: null,
  sortOrder: 1,
  status: 'DRAFT',
})

const nonArchivedLessons = computed(() =>
  lessons.value.filter((lesson) => lesson.status !== 'ARCHIVED'),
)

const nextSortOrder = computed(() => {
  if (nonArchivedLessons.value.length === 0) return 1
  return Math.max(...nonArchivedLessons.value.map((lesson) => lesson.sortOrder)) + 1
})

const courseArchived = computed(() => course.value?.status === 'ARCHIVED')

const titleValid = computed(() => {
  const value = form.title.trim()
  return value.length > 0 && value.length <= 200
})

const descriptionValid = computed(() => form.description.length <= 4000)

const sortOrderValid = computed(
  () => Number.isInteger(form.sortOrder) && form.sortOrder >= 0,
)

const orderConflict = computed(() =>
  nonArchivedLessons.value.some((lesson) => lesson.sortOrder === form.sortOrder),
)

const videoUrlValid = computed(() => {
  const value = form.videoUrl.trim()
  return value.length === 0 || parseYouTubeId(value) !== null
})

const videoDurationValid = computed(() => {
  if (!form.videoUrl.trim()) return form.videoDurationSeconds === null
  return Number.isInteger(form.videoDurationSeconds) && (form.videoDurationSeconds ?? 0) > 0 && (form.videoDurationSeconds ?? 0) <= 43200
})

const formValid = computed(
  () =>
    titleValid.value &&
    descriptionValid.value &&
    sortOrderValid.value &&
    !orderConflict.value &&
    videoUrlValid.value &&
    videoDurationValid.value &&
    !courseArchived.value,
)

const isSaving = computed(() => savingAction.value !== null)

const hasChanges = computed(() => {
  if (!initialSnapshot.value) return false
  return snapshotForm() !== initialSnapshot.value
})

const structureItems = computed<StructureItem[]>(() => {
  const items: StructureItem[] = nonArchivedLessons.value.map((lesson) => ({
    key: `lesson-${lesson.id}`,
    title: lesson.title,
    sortOrder: lesson.sortOrder,
    kind: 'lesson',
    status: lesson.status,
  }))

  items.push({
    key: 'creating',
    title: form.title.trim() || '[Đang tạo]',
    sortOrder: form.sortOrder,
    kind: 'creating',
  })

  return items.sort((a, b) => {
    if (a.sortOrder !== b.sortOrder) return a.sortOrder - b.sortOrder
    return a.kind === 'creating' ? 1 : -1
  })
})

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

function statusLabel(status: LessonStatus): string {
  if (status === 'PUBLISHED') return 'Đã xuất bản'
  if (status === 'DRAFT') return 'Bản nháp'
  return 'Đã lưu trữ'
}

function parseYouTubeId(value: string): string | null {
  try {
    const url = new URL(value.trim())

    if (url.protocol.toLowerCase() !== 'https:') return null

    const host = url.hostname.toLowerCase()
    let id: string | null = null

    if (host === 'youtu.be') {
      id = url.pathname
        .split('/')
        .filter(Boolean)[0] ?? null
    } else if (host === 'youtube.com' || host === 'www.youtube.com' || host === 'm.youtube.com') {
      if (url.pathname.toLowerCase() === '/watch') {
        id = url.searchParams.get('v')
      } else {
        const segments = url.pathname.split('/').filter(Boolean)
        if (
          segments.length === 2 &&
          (segments[0] === 'embed' || segments[0] === 'shorts')
        ) {
          id = segments[1] ?? null
        }
      }
    }

    return id && /^[A-Za-z0-9_-]{11}$/.test(id) ? id : null
  } catch {
    return null
  }
}

function loadVideoPreview(): void {
  previewProblem.value = ''
  previewVideoId.value = null

  const value = form.videoUrl.trim()
  if (!value) return

  const id = parseYouTubeId(value)
  if (!id) {
    previewProblem.value =
      'Chỉ hỗ trợ URL YouTube HTTPS dạng watch, youtu.be, embed hoặc shorts.'
    return
  }

  previewVideoId.value = id
}

function syncEditor(): void {
  form.contentHtml = editor.value?.innerHTML.trim() ?? ''
}

function execEditorCommand(command: string, value?: string): void {
  editor.value?.focus()
  document.execCommand(command, false, value)
  syncEditor()
}

function setBlock(tag: 'h3' | 'p' | 'blockquote' | 'pre'): void {
  execEditorCommand('formatBlock', tag)
}

function insertLink(): void {
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
  event.preventDefault()
  const value = event.clipboardData?.getData('text/plain') ?? ''
  document.execCommand('insertText', false, value)
  syncEditor()
}

function cancel(): void {
  if (isSaving.value) return
  void router.push(`/admin/courses/${courseId.value}`)
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
    return
  }

  loading.value = true
  problem.value = null

  try {
    const requestedId = courseId.value
    const [courseData, lessonData] = await Promise.all([
      courseApi.getAdminCourse(requestedId),
      lessonApi.getAdminLessons(requestedId),
    ])

    if (courseId.value !== requestedId) return

    course.value = courseData
    lessons.value = lessonData

    form.title = ''
    form.description = ''
    form.contentHtml = ''
    form.videoUrl = ''
    form.videoDurationSeconds = null
    form.sortOrder =
      lessonData.filter((lesson) => lesson.status !== 'ARCHIVED').length === 0
        ? 1
        : Math.max(
            ...lessonData
              .filter((lesson) => lesson.status !== 'ARCHIVED')
              .map((lesson) => lesson.sortOrder),
          ) + 1
    form.status = 'DRAFT'

    await nextTick()
    if (editor.value) editor.value.innerHTML = ''

    initialSnapshot.value = snapshotForm()

    if (courseData.status === 'ARCHIVED') {
      problem.value = {
        status: 409,
        code: 'COURSE_ARCHIVED',
        title: 'Khóa học đã được lưu trữ',
        detail: 'Không thể thêm bài học mới vào một khóa học đã lưu trữ.',
      }
    }
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}

async function createLesson(forceDraft: boolean): Promise<void> {
  if (!course.value || isSaving.value || !formValid.value) return

  savingAction.value = forceDraft ? 'draft' : 'create'
  problem.value = null

  try {
    const created = await lessonApi.createLesson(course.value.id, {
      title: form.title.trim(),
      description: form.description.trim() || null,
      contentHtml: form.contentHtml.trim() || null,
      videoUrl: form.videoUrl.trim() || null,
      videoDurationSeconds: form.videoUrl.trim() ? form.videoDurationSeconds : null,
      sortOrder: form.sortOrder,
      status: forceDraft ? 'DRAFT' : form.status,
    })

    allowLeave.value = true
    await router.replace(`/admin/lessons/${created.id}/edit`)
  } catch (error) {
    problem.value = toProblem(error)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    savingAction.value = null
  }
}

watch(
  () => form.videoUrl,
  () => {
    previewVideoId.value = null
    previewProblem.value = ''
    if (!form.videoUrl.trim()) form.videoDurationSeconds = null
  },
)

watch(
  () => route.params.courseId,
  () => {
    void load()
  },
)

onBeforeRouteLeave(() => {
  if (allowLeave.value || !hasChanges.value || isSaving.value) return true
  return window.confirm('Bạn có thay đổi chưa lưu. Bạn có chắc muốn rời khỏi trang?')
})

onMounted(load)
</script>

<template>
  <section>
    <!-- Loading -->
    <div v-if="loading" class="space-y-6">
      <div class="h-4 w-72 animate-pulse rounded bg-[#e9e2e1]" />
      <div class="grid gap-6 xl:grid-cols-[minmax(0,1fr)_320px]">
        <div class="space-y-6">
          <div class="h-72 animate-pulse rounded-2xl bg-[#eee8e7]" />
          <div class="h-80 animate-pulse rounded-2xl bg-[#eee8e7]" />
          <div class="h-[420px] animate-pulse rounded-2xl bg-[#eee8e7]" />
        </div>
        <div class="h-[430px] animate-pulse rounded-2xl bg-[#eee8e7]" />
      </div>
    </div>

    <template v-else>
      <ProblemAlert class="mb-6" :problem="problem" />

      <template v-if="course">
        <!-- Header -->
        <header class="mb-7">
          <nav
            aria-label="Breadcrumb"
            class="mb-3 flex flex-wrap items-center gap-2 text-xs font-semibold text-[#8d7d7b]"
          >
            <RouterLink to="/admin/courses" class="transition hover:text-[#a0001c]">
              Khóa học
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

            <RouterLink
              :to="`/admin/courses/${course.id}`"
              class="max-w-[420px] truncate transition hover:text-[#a0001c]"
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

            <span class="text-[#403735]">Tạo bài học</span>
          </nav>

          <div>
            <p class="text-xs font-bold uppercase tracking-[0.16em] text-[#9b8583]">
              Xây dựng nội dung
            </p>
            <h1
              class="mt-1.5 text-[30px] font-black tracking-[-0.03em] text-[#1c1b1b] sm:text-[34px]"
            >
              Tạo bài học mới
            </h1>
            <p class="mt-2 max-w-3xl text-sm leading-6 text-[#6b5d5c] sm:text-[15px]">
              Thêm thông tin, video và nội dung chi tiết cho bài học thuộc
              <strong class="font-bold text-[#4b403f]">{{ course.title }}</strong>.
            </p>
          </div>
        </header>

        <section class="mb-5 rounded-2xl border border-[#eadfdf] bg-[#fffdfc] p-4 sm:p-5">
          <div class="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <p class="text-[10px] font-black uppercase tracking-[0.15em] text-[#a0001c]">
                Quy trình gợi ý
              </p>
              <h2 class="mt-1 text-sm font-black text-[#403735]">
                Tạo khung trước, hoàn thiện kiểm tra sau
              </h2>
              <p class="mt-1 text-xs leading-5 text-[#81716f]">
                1. Nhập nội dung → 2. Thêm video + thời lượng nếu có → 3. Lưu bài → 4. Sang Câu hỏi & kiểm tra để tạo checkpoint và bài củng cố.
              </p>
            </div>
            <div class="rounded-lg border border-[#efd1a7] bg-[#fff8ed] px-3 py-2 text-[10px] font-semibold leading-4 text-[#7b5521] lg:max-w-[310px]">
              Chưa chắc nội dung đã xong? Hãy lưu <strong>Bản nháp</strong>. Học viên chỉ thấy lesson khi bạn chủ động xuất bản.
            </div>
          </div>
        </section>

        <form @submit.prevent="createLesson(false)">
          <div class="grid gap-6 xl:grid-cols-[minmax(0,1fr)_320px] xl:items-start">
            <!-- Main editor -->
            <div class="space-y-6">
              <!-- Basic information -->
              <section
                class="rounded-2xl border border-[#e7dedd] bg-white p-5 shadow-[0_2px_12px_rgba(50,25,25,0.025)] sm:p-7"
              >
                <div class="mb-6 flex items-start gap-3 border-b border-[#eee7e6] pb-5">
                  <span
                    class="grid size-10 shrink-0 place-items-center rounded-xl border border-[#f0d9dc] bg-[#fff3f4] text-[#c9152b]"
                  >
                    <svg
                      class="size-5"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="1.8"
                      aria-hidden="true"
                    >
                      <circle cx="12" cy="12" r="9" />
                      <path d="M12 10.5V17M12 7h.01" />
                    </svg>
                  </span>

                  <div>
                    <h2 class="text-lg font-black tracking-[-0.02em] text-[#302929]">
                      Thông tin bài học
                    </h2>
                    <p class="mt-1 text-sm leading-5 text-[#81716f]">
                      Thiết lập tên, mô tả, vị trí và trạng thái ban đầu.
                    </p>
                  </div>
                </div>

                <div class="space-y-5">
                  <label class="block">
                    <span
                      class="mb-2 flex items-center justify-between gap-3 text-sm font-bold text-[#403735]"
                    >
                      <span>Tên bài học <span class="text-[#c9152b]">*</span></span>
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
                      placeholder="Ví dụ: Bài 1 - Giới thiệu về Python"
                      class="h-11 w-full rounded-xl border border-[#ddd4d3] bg-white px-3.5 text-sm text-[#302929] outline-none placeholder:text-[#aa9b99] focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                    />
                  </label>

                  <label class="block">
                    <span
                      class="mb-2 flex items-center justify-between gap-3 text-sm font-bold text-[#403735]"
                    >
                      <span>Mô tả ngắn</span>
                      <span
                        class="text-[11px] font-semibold"
                        :class="form.description.length > 4000 ? 'text-[#ba1a1a]' : 'text-[#a0918f]'"
                      >
                        {{ form.description.length }}/4000
                      </span>
                    </span>

                    <textarea
                      v-model="form.description"
                      maxlength="4000"
                      rows="3"
                      placeholder="Tóm tắt mục tiêu và nội dung chính của bài học…"
                      class="w-full resize-y rounded-xl border border-[#ddd4d3] bg-white px-3.5 py-3 text-sm leading-6 text-[#302929] outline-none placeholder:text-[#aa9b99] focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                    />
                  </label>

                  <div class="grid gap-5 sm:grid-cols-2">
                    <label class="block">
                      <span class="mb-2 block text-sm font-bold text-[#403735]">
                        Thứ tự bài học
                      </span>

                      <input
                        v-model.number="form.sortOrder"
                        required
                        type="number"
                        min="0"
                        step="1"
                        class="h-11 w-full rounded-xl border bg-white px-3.5 text-sm font-bold text-[#302929] outline-none focus:ring-4"
                        :class="
                          !sortOrderValid || orderConflict
                            ? 'border-[#d95b68] focus:border-[#ba1a1a] focus:ring-[#ba1a1a]/10'
                            : 'border-[#ddd4d3] focus:border-[#c9152b] focus:ring-[#c9152b]/10'
                        "
                      />

                      <p
                        class="mt-2 text-xs leading-5"
                        :class="orderConflict ? 'text-[#ba1a1a]' : 'text-[#8d7c7a]'"
                      >
                        {{
                          orderConflict
                            ? 'Thứ tự này đang được một bài học khác sử dụng.'
                            : `Gợi ý: ${nextSortOrder}. Thứ tự phải duy nhất trong các bài học chưa lưu trữ.`
                        }}
                      </p>
                    </label>

                    <fieldset>
                      <legend class="mb-2 text-sm font-bold text-[#403735]">
                        Trạng thái ban đầu
                      </legend>

                      <div class="grid grid-cols-2 gap-2.5">
                        <label class="cursor-pointer">
                          <input
                            v-model="form.status"
                            type="radio"
                            value="DRAFT"
                            class="peer sr-only"
                          />
                          <span
                            class="flex h-11 items-center justify-center gap-2 rounded-xl border border-[#e2d9d8] bg-white px-3 text-xs font-bold text-[#6c5f5d] transition peer-checked:border-[#d9a8ae] peer-checked:bg-[#fff3f4] peer-checked:text-[#a0001c]"
                          >
                            <span class="size-1.5 rounded-full bg-current opacity-70" />
                            Bản nháp
                          </span>
                        </label>

                        <label class="cursor-pointer">
                          <input
                            v-model="form.status"
                            type="radio"
                            value="PUBLISHED"
                            class="peer sr-only"
                          />
                          <span
                            class="flex h-11 items-center justify-center gap-2 rounded-xl border border-[#e2d9d8] bg-white px-3 text-xs font-bold text-[#6c5f5d] transition peer-checked:border-[#a9d4be] peer-checked:bg-[#f0faf5] peer-checked:text-[#16764f]"
                          >
                            <span class="size-1.5 rounded-full bg-current opacity-70" />
                            Xuất bản
                          </span>
                        </label>
                      </div>
                    </fieldset>
                  </div>
                </div>
              </section>

              <!-- Video -->
              <section
                class="rounded-2xl border border-[#e7dedd] bg-white p-5 shadow-[0_2px_12px_rgba(50,25,25,0.025)] sm:p-7"
              >
                <div class="mb-6 flex items-start gap-3 border-b border-[#eee7e6] pb-5">
                  <span
                    class="grid size-10 shrink-0 place-items-center rounded-xl border border-[#eadfdf] bg-[#f8f4f3] text-[#7f1020]"
                  >
                    <svg
                      class="size-5"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="1.8"
                      aria-hidden="true"
                    >
                      <rect x="3" y="5" width="18" height="14" rx="3" />
                      <path d="m10 9 5 3-5 3Z" />
                    </svg>
                  </span>

                  <div>
                    <h2 class="text-lg font-black tracking-[-0.02em] text-[#302929]">
                      Video bài học
                    </h2>
                    <p class="mt-1 text-sm leading-5 text-[#81716f]">
                      Video là tùy chọn và chỉ hỗ trợ YouTube qua HTTPS.
                    </p>
                  </div>
                </div>

                <label class="block">
                  <span class="mb-2 block text-sm font-bold text-[#403735]">YouTube URL</span>

                  <div class="flex flex-col gap-2 sm:flex-row">
                    <input
                      v-model="form.videoUrl"
                      type="url"
                      placeholder="https://www.youtube.com/watch?v=..."
                      class="h-11 min-w-0 flex-1 rounded-xl border bg-white px-3.5 text-sm text-[#302929] outline-none placeholder:text-[#aa9b99] focus:ring-4"
                      :class="
                        videoUrlValid
                          ? 'border-[#ddd4d3] focus:border-[#c9152b] focus:ring-[#c9152b]/10'
                          : 'border-[#d95b68] focus:border-[#ba1a1a] focus:ring-[#ba1a1a]/10'
                      "
                    />

                    <button
                      type="button"
                      class="h-11 shrink-0 rounded-xl border border-[#d9cfce] bg-[#f7f4f3] px-4 text-xs font-bold text-[#665957] transition hover:bg-[#eee9e8]"
                      @click="loadVideoPreview"
                    >
                      Xem trước
                    </button>
                  </div>

                  <p
                    v-if="previewProblem"
                    class="mt-2 text-xs leading-5 text-[#ba1a1a]"
                  >
                    {{ previewProblem }}
                  </p>
                  <p v-else class="mt-2 text-xs leading-5 text-[#8d7c7a]">
                    Hỗ trợ youtube.com/watch, youtu.be, /embed/ và /shorts/.
                  </p>
                </label>

                <div v-if="form.videoUrl.trim()" class="mt-4 block max-w-sm">
                  <span class="mb-2 block text-xs font-bold text-[#403735]">Thời lượng video tin cậy</span>
                  <DurationInput v-model="form.videoDurationSeconds" />
                  <p class="mt-1.5 text-[11px] leading-5 text-[#8d7c7a]">Dùng để chống tua và xác nhận đã xem hết video. Video Published bắt buộc có giá trị này.</p>
                </div>

                <div
                  class="mt-5 aspect-video overflow-hidden rounded-xl border border-dashed border-[#d9cecd] bg-[#f5f1f0]"
                >
                  <iframe
                    v-if="previewVideoId"
                    class="size-full border-0"
                    :src="`https://www.youtube-nocookie.com/embed/${encodeURIComponent(previewVideoId)}`"
                    title="Xem trước video bài học"
                    loading="lazy"
                    allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                    allowfullscreen
                  />

                  <div
                    v-else
                    class="lesson-grid flex size-full flex-col items-center justify-center px-6 text-center text-[#998784]"
                  >
                    <svg
                      class="size-9"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="1.6"
                      aria-hidden="true"
                    >
                      <rect x="3" y="5" width="18" height="14" rx="3" />
                      <path d="m10 9 5 3-5 3Z" />
                    </svg>
                    <p class="mt-3 text-xs font-semibold leading-5">
                      Nhập URL YouTube hợp lệ rồi bấm “Xem trước”.
                    </p>
                  </div>
                </div>
              </section>

              <!-- Rich content -->
              <section
                class="overflow-hidden rounded-2xl border border-[#e7dedd] bg-white shadow-[0_2px_12px_rgba(50,25,25,0.025)]"
              >
                <div class="flex items-start gap-3 border-b border-[#eee7e6] px-5 py-5 sm:px-7">
                  <span
                    class="grid size-10 shrink-0 place-items-center rounded-xl border border-[#eadfdf] bg-[#f8f4f3] text-[#7f1020]"
                  >
                    <svg
                      class="size-5"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="1.8"
                      aria-hidden="true"
                    >
                      <path d="M5 4h14v16H5z" />
                      <path d="M8 8h8M8 12h8M8 16h5" />
                    </svg>
                  </span>

                  <div>
                    <h2 class="text-lg font-black tracking-[-0.02em] text-[#302929]">
                      Nội dung chi tiết
                    </h2>
                    <p class="mt-1 text-sm leading-5 text-[#81716f]">
                      Nội dung sẽ được backend lọc HTML an toàn trước khi lưu.
                    </p>
                  </div>
                </div>

                <!-- Toolbar -->
                <div
                  class="flex flex-wrap items-center gap-1 border-b border-[#e8e0df] bg-[#faf8f7] px-3 py-2.5 sm:px-5"
                  aria-label="Công cụ định dạng"
                >
                  <button
                    type="button"
                    class="editor-tool"
                    title="Tiêu đề"
                    @click="setBlock('h3')"
                  >
                    H3
                  </button>
                  <button
                    type="button"
                    class="editor-tool"
                    title="Đoạn văn"
                    @click="setBlock('p')"
                  >
                    P
                  </button>
                  <span class="mx-1 h-5 w-px bg-[#ded5d4]" />

                  <button
                    type="button"
                    class="editor-tool font-black"
                    title="In đậm"
                    @click="execEditorCommand('bold')"
                  >
                    B
                  </button>
                  <button
                    type="button"
                    class="editor-tool italic"
                    title="In nghiêng"
                    @click="execEditorCommand('italic')"
                  >
                    I
                  </button>
                  <span class="mx-1 h-5 w-px bg-[#ded5d4]" />

                  <button
                    type="button"
                    class="editor-tool"
                    title="Danh sách dấu chấm"
                    @click="execEditorCommand('insertUnorderedList')"
                  >
                    • List
                  </button>
                  <button
                    type="button"
                    class="editor-tool"
                    title="Danh sách đánh số"
                    @click="execEditorCommand('insertOrderedList')"
                  >
                    1. List
                  </button>
                  <span class="mx-1 h-5 w-px bg-[#ded5d4]" />

                  <button
                    type="button"
                    class="editor-tool"
                    title="Liên kết"
                    @click="insertLink"
                  >
                    Link
                  </button>
                  <button
                    type="button"
                    class="editor-tool"
                    title="Trích dẫn"
                    @click="setBlock('blockquote')"
                  >
                    Quote
                  </button>
                  <button
                    type="button"
                    class="editor-tool"
                    title="Khối mã"
                    @click="setBlock('pre')"
                  >
                    Code
                  </button>
                </div>

                <!-- Editor -->
                <div class="p-4 sm:p-6">
                  <div
                    ref="editor"
                    class="lesson-editor min-h-[320px] rounded-xl border border-[#e1d8d7] bg-white p-5 text-sm leading-7 text-[#342d2d] outline-none transition focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                    contenteditable="true"
                    role="textbox"
                    aria-multiline="true"
                    data-placeholder="Bắt đầu soạn nội dung bài học tại đây…"
                    @input="syncEditor"
                    @blur="syncEditor"
                    @paste="pastePlainText"
                  />

                  <div class="mt-3 flex items-start gap-2 text-xs leading-5 text-[#8d7c7a]">
                    <svg
                      class="mt-0.5 size-4 shrink-0"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="1.8"
                      aria-hidden="true"
                    >
                      <circle cx="12" cy="12" r="9" />
                      <path d="M12 10.5V17M12 7h.01" />
                    </svg>
                    <p>
                      Khi dán nội dung, editor chỉ nhận văn bản thuần để tránh mang theo HTML không mong muốn.
                      Link chỉ cho phép HTTPS hoặc mailto.
                    </p>
                  </div>
                </div>
              </section>
            </div>

            <!-- Context sidebar -->
            <aside class="space-y-5 xl:sticky xl:top-24 xl:self-start">
              <section
                class="rounded-2xl border border-[#e5dcdb] bg-white p-5 shadow-[0_7px_24px_rgba(60,27,32,0.04)]"
              >
                <p class="text-[10px] font-black uppercase tracking-[0.16em] text-[#95827f]">
                  Thuộc khóa học
                </p>

                <RouterLink
                  :to="`/admin/courses/${course.id}`"
                  class="mt-3 flex items-start gap-3 rounded-xl border border-[#ede4e3] bg-[#faf7f6] p-3.5 transition hover:border-[#dcbdc0] hover:bg-[#fff7f7]"
                >
                  <span
                    class="grid size-10 shrink-0 place-items-center rounded-lg border border-[#eed7da] bg-[#fff1f2] text-[#c9152b]"
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

                  <span class="min-w-0">
                    <strong class="block line-clamp-2 text-sm font-black leading-5 text-[#3b3332]">
                      {{ course.title }}
                    </strong>
                    <span class="mt-1 block text-[11px] font-semibold text-[#958582]">
                      {{ nonArchivedLessons.length }} bài học hiện tại
                    </span>
                  </span>
                </RouterLink>

                <div class="mt-5 border-t border-[#eee7e6] pt-5">
                  <div class="flex items-center justify-between gap-3">
                    <p class="text-xs font-black uppercase tracking-[0.12em] text-[#7d6d6b]">
                      Cấu trúc hiện tại
                    </p>
                    <span class="text-[10px] font-bold text-[#a08f8d]">
                      Theo thứ tự
                    </span>
                  </div>

                  <div class="relative mt-4 space-y-4">
                    <div
                      class="absolute bottom-2 left-[7px] top-2 w-px bg-[#e1d8d7]"
                      aria-hidden="true"
                    />

                    <div
                      v-for="item in structureItems"
                      :key="item.key"
                      class="relative flex items-start gap-3 pl-0"
                    >
                      <span
                        class="relative z-10 mt-1 grid size-[15px] shrink-0 place-items-center rounded-full border-2 bg-white"
                        :class="
                          item.kind === 'creating'
                            ? 'border-[#c9152b]'
                            : item.status === 'PUBLISHED'
                              ? 'border-[#16875b]'
                              : 'border-[#b7a8a6]'
                        "
                      >
                        <span
                          v-if="item.kind === 'creating'"
                          class="size-1.5 rounded-full bg-[#c9152b]"
                        />
                      </span>

                      <div class="min-w-0 flex-1">
                        <p
                          class="line-clamp-2 text-xs leading-5"
                          :class="
                            item.kind === 'creating'
                              ? 'font-black text-[#a0001c]'
                              : 'font-semibold text-[#655856]'
                          "
                        >
                          <span class="mr-1 text-[10px] text-[#9b8987]">
                            #{{ item.sortOrder }}
                          </span>
                          {{ item.title }}
                        </p>

                        <span
                          v-if="item.kind === 'lesson' && item.status"
                          class="mt-0.5 block text-[10px] font-semibold text-[#a08f8d]"
                        >
                          {{ statusLabel(item.status) }}
                        </span>
                        <span
                          v-else
                          class="mt-0.5 block text-[10px] font-bold text-[#c9152b]"
                        >
                          Đang tạo
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
              </section>

              <section
                class="rounded-2xl border border-[#eadfdf] bg-[#f7f3f2] p-5"
              >
                <p class="text-xs font-black text-[#4d4240]">Trước khi tạo bài học</p>
                <ul class="mt-3 space-y-2.5 text-xs leading-5 text-[#756765]">
                  <li class="flex gap-2">
                    <span class="mt-2 size-1.5 shrink-0 rounded-full bg-[#c9152b]" />
                    Tên bài học là bắt buộc và tối đa 200 ký tự.
                  </li>
                  <li class="flex gap-2">
                    <span class="mt-2 size-1.5 shrink-0 rounded-full bg-[#c9152b]" />
                    Thứ tự không được âm và không trùng bài học đang hoạt động.
                  </li>
                  <li class="flex gap-2">
                    <span class="mt-2 size-1.5 shrink-0 rounded-full bg-[#c9152b]" />
                    Nếu xuất bản ngay, bài học sẽ tham gia learning path của học viên khi course hợp lệ.
                  </li>
                </ul>
              </section>
            </aside>
          </div>

          <!-- Sticky actions -->
          <div
            class="sticky bottom-0 z-20 -mx-4 -mb-4 mt-7 border-t border-[#e4dcdb] bg-[#f8f7f5]/95 px-4 py-4 backdrop-blur sm:-mx-5 sm:-mb-5 sm:px-5 lg:-mx-6 lg:px-6"
          >
            <div
              class="mx-auto flex max-w-[1360px] flex-col-reverse gap-3 sm:flex-row sm:items-center sm:justify-between"
            >
              <button
                type="button"
                :disabled="isSaving"
                class="h-10 self-start rounded-lg px-4 text-sm font-bold text-[#756765] transition hover:bg-[#eee9e8] disabled:cursor-not-allowed disabled:opacity-50"
                @click="cancel"
              >
                Hủy
              </button>

              <div class="flex flex-col-reverse gap-3 sm:flex-row">
                <button
                  type="button"
                  :disabled="isSaving || !formValid"
                  class="inline-flex h-10 items-center justify-center gap-2 rounded-lg border border-[#d79aa2] bg-white px-5 text-sm font-bold text-[#a0001c] transition hover:bg-[#fff3f4] disabled:cursor-not-allowed disabled:opacity-50"
                  @click="createLesson(true)"
                >
                  <svg
                    v-if="savingAction === 'draft'"
                    class="size-4 animate-spin"
                    viewBox="0 0 24 24"
                    fill="none"
                    aria-hidden="true"
                  >
                    <circle class="opacity-30" cx="12" cy="12" r="9" stroke="currentColor" stroke-width="3" />
                    <path d="M21 12a9 9 0 0 0-9-9" stroke="currentColor" stroke-width="3" stroke-linecap="round" />
                  </svg>
                  {{ savingAction === 'draft' ? 'Đang lưu…' : 'Lưu bản nháp' }}
                </button>

                <button
                  type="submit"
                  :disabled="isSaving || !formValid"
                  class="inline-flex h-10 items-center justify-center gap-2 rounded-lg bg-[#c9152b] px-5 text-sm font-bold text-white shadow-[0_6px_16px_rgba(160,0,28,0.13)] transition hover:bg-[#a0001c] disabled:cursor-not-allowed disabled:opacity-50"
                >
                  <svg
                    v-if="savingAction === 'create'"
                    class="size-4 animate-spin"
                    viewBox="0 0 24 24"
                    fill="none"
                    aria-hidden="true"
                  >
                    <circle class="opacity-30" cx="12" cy="12" r="9" stroke="currentColor" stroke-width="3" />
                    <path d="M21 12a9 9 0 0 0-9-9" stroke="currentColor" stroke-width="3" stroke-linecap="round" />
                  </svg>

                  <svg
                    v-else
                    class="size-4"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                    aria-hidden="true"
                  >
                    <path d="M12 5v14M5 12h14" />
                  </svg>

                  {{ savingAction === 'create' ? 'Đang tạo…' : 'Tạo bài học' }}
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
.lesson-grid {
  background-image:
    linear-gradient(to right, rgb(232 227 227 / 0.68) 1px, transparent 1px),
    linear-gradient(to bottom, rgb(232 227 227 / 0.68) 1px, transparent 1px);
  background-size: 22px 22px;
}

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
    color 150ms ease;
}

.editor-tool:hover {
  background: #f0e9e8;
  color: #a0001c;
}

.lesson-editor:empty::before {
  content: attr(data-placeholder);
  color: #a2918f;
  font-style: italic;
  pointer-events: none;
}

.lesson-editor :deep(h1),
.lesson-editor :deep(h2),
.lesson-editor :deep(h3) {
  margin: 1.1rem 0 0.55rem;
  font-weight: 800;
  line-height: 1.35;
  color: #302929;
}

.lesson-editor :deep(h3) {
  font-size: 1.15rem;
}

.lesson-editor :deep(p) {
  margin: 0.6rem 0;
}

.lesson-editor :deep(ul) {
  margin: 0.7rem 0;
  list-style: disc;
  padding-left: 1.4rem;
}

.lesson-editor :deep(ol) {
  margin: 0.7rem 0;
  list-style: decimal;
  padding-left: 1.4rem;
}

.lesson-editor :deep(blockquote) {
  margin: 0.9rem 0;
  border-left: 3px solid #dec5c8;
  background: #faf6f6;
  padding: 0.75rem 1rem;
  color: #655856;
}

.lesson-editor :deep(pre) {
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

.lesson-editor :deep(a) {
  color: #a0001c;
  text-decoration: underline;
}
</style>