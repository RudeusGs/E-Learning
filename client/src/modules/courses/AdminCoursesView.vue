<script setup lang="ts">
import { onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'
import type { CourseStatus } from '@/types/CourseStatus'

import * as courseApi from './course.api'
import type { AdminCourseListItem } from './models/AdminCourseListItem'

type StatusFilter = 'ALL' | CourseStatus
type CourseSort = 'sortOrder' | '-sortOrder'

const route = useRoute()
const router = useRouter()

const courses = ref<AdminCourseListItem[]>([])
const nextCursor = ref<string | null>(null)
const hasMore = ref(false)

const loading = ref(false)
const saving = ref(false)
const archivingId = ref<number | null>(null)
const problem = ref<ProblemDetails | null>(null)

const search = ref('')
const statusFilter = ref<StatusFilter>('ALL')
const sort = ref<CourseSort>('sortOrder')

const createOpen = ref(false)
const openActionsId = ref<number | null>(null)

let searchTimeout: ReturnType<typeof setTimeout> | null = null
let abortController: AbortController | null = null

const form = reactive({
  title: '',
  description: '',
  thumbnailUrl: '',
  status: 'DRAFT' as Exclude<CourseStatus, 'ARCHIVED'>,
  sortOrder: 1,
})

const statusOptions: Array<{ value: StatusFilter; label: string }> = [
  { value: 'ALL', label: 'Tất cả' },
  { value: 'PUBLISHED', label: 'Đã xuất bản' },
  { value: 'DRAFT', label: 'Bản nháp' },
  { value: 'ARCHIVED', label: 'Đã lưu trữ' },
]

function isCanceled(error: unknown): boolean {
  return (
    (error as Error)?.name === 'CanceledError' ||
    (error as { code?: string })?.code === 'ERR_CANCELED'
  )
}

function statusLabel(status: CourseStatus): string {
  if (status === 'PUBLISHED') return 'Đã xuất bản'
  if (status === 'DRAFT') return 'Bản nháp'
  return 'Đã lưu trữ'
}

function statusClass(status: CourseStatus): string {
  if (status === 'PUBLISHED') {
    return 'border-[#b9dfcc] bg-[#eef9f3] text-[#16764f]'
  }

  if (status === 'DRAFT') {
    return 'border-[#efc7cb] bg-[#fff3f4] text-[#a22435]'
  }

  return 'border-[#ddd5d4] bg-[#f2efee] text-[#766967]'
}

async function load(reset = false): Promise<void> {
  if (loading.value && !reset) return

  abortController?.abort()
  abortController = new AbortController()
  const signal = abortController.signal

  loading.value = true
  if (reset) problem.value = null

  try {
    const page = await courseApi.getAdminCourses(
      reset ? null : nextCursor.value,
      search.value.trim(),
      signal,
      20,
      statusFilter.value === 'ALL' ? null : statusFilter.value,
      sort.value,
    )

    if (signal.aborted) return

    courses.value = reset ? page.items : [...courses.value, ...page.items]
    nextCursor.value = page.nextCursor
    hasMore.value = page.hasMore
  } catch (error) {
    if (isCanceled(error)) return
    problem.value = toProblem(error)
  } finally {
    if (!signal.aborted) loading.value = false
  }
}

function onSearchInput(): void {
  if (searchTimeout) window.clearTimeout(searchTimeout)

  searchTimeout = window.setTimeout(() => {
    void load(true)
  }, 350)
}

function onSearchSubmit(): void {
  if (searchTimeout) {
    window.clearTimeout(searchTimeout)
    searchTimeout = null
  }

  void load(true)
}

function clearSearch(): void {
  if (!search.value) return
  search.value = ''
  void load(true)
}

function setStatusFilter(value: StatusFilter): void {
  if (statusFilter.value === value) return
  statusFilter.value = value
  void load(true)
}

function onSortChange(): void {
  void load(true)
}

function openCreate(): void {
  void router.push('/admin/courses/create')
}

function closeCreate(): void {
  if (saving.value) return
  createOpen.value = false

  if (route.hash === '#create-course') {
    void router.replace({ path: '/admin/courses', query: route.query })
  }
}

function resetForm(): void {
  form.title = ''
  form.description = ''
  form.thumbnailUrl = ''
  form.status = 'DRAFT'
  form.sortOrder =
    courses.value.length > 0 ? Math.max(...courses.value.map((course) => course.sortOrder)) + 1 : 1
}

async function createCourse(): Promise<void> {
  if (saving.value) return

  saving.value = true
  problem.value = null

  try {
    const created = await courseApi.createCourse({
      title: form.title.trim(),
      description: form.description.trim() || null,
      thumbnailUrl: form.thumbnailUrl.trim() || null,
      status: form.status,
      sortOrder: form.sortOrder,
    })

    resetForm()
    createOpen.value = false
    await router.replace(`/admin/courses/${created.id}`)
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    saving.value = false
  }
}

function toggleActions(courseId: number): void {
  openActionsId.value = openActionsId.value === courseId ? null : courseId
}

async function archiveCourse(course: AdminCourseListItem): Promise<void> {
  openActionsId.value = null

  if (course.status === 'ARCHIVED') return

  const confirmed = window.confirm(
    `Lưu trữ khóa học "${course.title}"?\n\nKhóa học sẽ không còn hiển thị cho học viên, nhưng lịch sử học tập vẫn được giữ lại.`,
  )
  if (!confirmed) return

  archivingId.value = course.id
  problem.value = null

  try {
    await courseApi.archiveCourse(course.id)
    await load(true)
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    archivingId.value = null
  }
}

function onWindowClick(): void {
  openActionsId.value = null
}

watch(
  () => route.hash,
  (hash) => {
    if (hash === '#create-course') {
      resetForm()
      createOpen.value = true
    }
  },
  { immediate: true },
)

onMounted(() => {
  window.addEventListener('click', onWindowClick)
  void load(true)
})

onBeforeUnmount(() => {
  abortController?.abort()
  if (searchTimeout) window.clearTimeout(searchTimeout)
  window.removeEventListener('click', onWindowClick)
})
</script>

<template>
  <section class="space-y-6">
    <!-- Page heading -->
    <header class="flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
      <div>
        <p class="text-xs font-bold uppercase tracking-[0.16em] text-[#9b8583]">Quản lý nội dung</p>
        <h1 class="mt-1.5 text-[30px] font-black tracking-[-0.03em] text-[#1c1b1b] sm:text-[34px]">
          Khóa học
        </h1>
        <p class="mt-2 max-w-2xl text-sm leading-6 text-[#6b5d5c] sm:text-[15px]">
          Quản lý nội dung, trạng thái và thứ tự hiển thị các khóa học trong hệ thống.
        </p>
      </div>

      <button
        type="button"
        class="inline-flex h-11 shrink-0 items-center justify-center gap-2 self-start rounded-xl bg-[#c9152b] px-4.5 text-sm font-bold text-white shadow-[0_8px_18px_rgba(160,0,28,0.14)] transition hover:bg-[#a0001c] focus:outline-none focus-visible:ring-4 focus-visible:ring-[#c9152b]/20 sm:self-auto"
        @click="openCreate"
      >
        <svg
          class="size-4.5"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          aria-hidden="true"
        >
          <path d="M12 5v14M5 12h14" />
        </svg>
        Tạo khóa học
      </button>
    </header>

    <ProblemAlert :problem="problem" />

    <!-- List container -->
    <div
      class="overflow-hidden rounded-2xl border border-[#e5dedd] bg-white shadow-[0_1px_3px_rgba(44,24,24,0.025)]"
    >
      <!-- Toolbar -->
      <div class="border-b border-[#ece5e4] p-4 sm:p-5">
        <div class="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
          <form class="w-full xl:max-w-[420px]" @submit.prevent="onSearchSubmit">
            <label class="sr-only" for="course-search">Tìm khóa học</label>
            <div class="relative">
              <span
                class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3.5 text-[#8d7c7a]"
              >
                <svg
                  class="size-5"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="1.8"
                  aria-hidden="true"
                >
                  <circle cx="11" cy="11" r="7" />
                  <path d="m20 20-4-4" />
                </svg>
              </span>

              <input
                id="course-search"
                v-model="search"
                type="search"
                maxlength="100"
                autocomplete="off"
                placeholder="Tìm theo tên khóa học…"
                class="h-11 w-full rounded-xl border border-[#ddd4d3] bg-[#fffefe] pl-11 pr-10 text-sm text-[#2c2626] outline-none placeholder:text-[#aa9b99] focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                @input="onSearchInput"
              />

              <button
                v-if="search"
                type="button"
                aria-label="Xóa nội dung tìm kiếm"
                class="absolute inset-y-0 right-0 grid w-10 place-items-center text-[#9a8886] hover:text-[#7f1020]"
                @click="clearSearch"
              >
                <svg
                  class="size-4"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                  aria-hidden="true"
                >
                  <path d="M6 6l12 12M18 6 6 18" />
                </svg>
              </button>
            </div>
          </form>

          <div class="flex flex-col gap-3 sm:flex-row sm:items-center">
            <div class="flex max-w-full gap-1.5 overflow-x-auto pb-1 sm:pb-0">
              <button
                v-for="option in statusOptions"
                :key="option.value"
                type="button"
                class="h-9 shrink-0 rounded-lg border px-3 text-xs font-bold transition"
                :class="
                  statusFilter === option.value
                    ? 'border-[#d9a8ae] bg-[#fff1f2] text-[#a0001c]'
                    : 'border-[#e4dcdb] bg-white text-[#756765] hover:border-[#d5c7c5] hover:bg-[#faf7f7]'
                "
                @click="setStatusFilter(option.value)"
              >
                {{ option.label }}
              </button>
            </div>

            <label class="relative shrink-0">
              <span class="sr-only">Sắp xếp khóa học</span>
              <select
                v-model="sort"
                class="h-9 appearance-none rounded-lg border border-[#e4dcdb] bg-white py-0 pl-3 pr-9 text-xs font-bold text-[#665957] outline-none focus:border-[#c9152b] focus:ring-3 focus:ring-[#c9152b]/10"
                @change="onSortChange"
              >
                <option value="sortOrder">Thứ tự tăng dần</option>
                <option value="-sortOrder">Thứ tự giảm dần</option>
              </select>
              <svg
                class="pointer-events-none absolute right-2.5 top-1/2 size-4 -translate-y-1/2 text-[#8e7d7b]"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                aria-hidden="true"
              >
                <path d="m7 10 5 5 5-5" />
              </svg>
            </label>
          </div>
        </div>
      </div>

      <!-- Loading skeleton -->
      <div v-if="loading && courses.length === 0" class="overflow-x-auto">
        <table class="w-full min-w-[820px] border-collapse text-left">
          <thead>
            <tr class="border-b border-[#ece5e4] bg-[#faf8f7]">
              <th class="px-5 py-3.5 text-xs font-bold text-[#786967]">Tên khóa học</th>
              <th class="px-5 py-3.5 text-xs font-bold text-[#786967]">Trạng thái</th>
              <th class="px-5 py-3.5 text-center text-xs font-bold text-[#786967]">Bài học</th>
              <th class="px-5 py-3.5 text-center text-xs font-bold text-[#786967]">Học viên</th>
              <th class="px-5 py-3.5 text-center text-xs font-bold text-[#786967]">Thứ tự</th>
              <th class="px-5 py-3.5 text-right text-xs font-bold text-[#786967]">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="index in 6" :key="index" class="border-b border-[#f0eae9] last:border-0">
              <td class="px-5 py-4">
                <div class="flex items-center gap-3">
                  <div class="size-10 animate-pulse rounded-lg bg-[#efe9e8]" />
                  <div>
                    <div class="h-4 w-48 animate-pulse rounded bg-[#ebe4e3]" />
                    <div class="mt-2 h-3 w-24 animate-pulse rounded bg-[#f2edec]" />
                  </div>
                </div>
              </td>
              <td class="px-5 py-4">
                <div class="h-7 w-24 animate-pulse rounded-full bg-[#f0eae9]" />
              </td>
              <td class="px-5 py-4">
                <div class="mx-auto h-4 w-8 animate-pulse rounded bg-[#eee8e7]" />
              </td>
              <td class="px-5 py-4">
                <div class="mx-auto h-4 w-8 animate-pulse rounded bg-[#eee8e7]" />
              </td>
              <td class="px-5 py-4">
                <div class="mx-auto h-4 w-6 animate-pulse rounded bg-[#eee8e7]" />
              </td>
              <td class="px-5 py-4">
                <div class="ml-auto h-8 w-8 animate-pulse rounded-lg bg-[#eee8e7]" />
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Empty state -->
      <div
        v-else-if="courses.length === 0"
        class="flex min-h-[360px] flex-col items-center justify-center px-6 py-12 text-center"
      >
        <div
          class="grid size-16 place-items-center rounded-2xl border border-[#eadfdf] bg-[#faf6f6] text-[#a98f8d]"
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

        <h2 class="mt-5 text-lg font-black text-[#342d2d]">
          {{
            search || statusFilter !== 'ALL'
              ? 'Không tìm thấy khóa học phù hợp'
              : 'Chưa có khóa học'
          }}
        </h2>
        <p class="mt-2 max-w-md text-sm leading-6 text-[#81716f]">
          {{
            search || statusFilter !== 'ALL'
              ? 'Thử thay đổi từ khóa hoặc bộ lọc trạng thái để xem thêm kết quả.'
              : 'Tạo khóa học đầu tiên để bắt đầu xây dựng nội dung học tập.'
          }}
        </p>

        <button
          v-if="!search && statusFilter === 'ALL'"
          type="button"
          class="mt-5 inline-flex h-10 items-center gap-2 rounded-lg bg-[#c9152b] px-4 text-sm font-bold text-white hover:bg-[#a0001c]"
          @click="openCreate"
        >
          <svg
            class="size-4"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
          >
            <path d="M12 5v14M5 12h14" />
          </svg>
          Tạo khóa học
        </button>
      </div>

      <!-- Data table -->
      <div v-else class="overflow-x-auto">
        <table class="w-full min-w-[820px] border-collapse text-left">
          <thead>
            <tr class="border-b border-[#ece5e4] bg-[#faf8f7]">
              <th class="w-[40%] px-5 py-3.5 text-xs font-bold text-[#786967]">Tên khóa học</th>
              <th class="px-5 py-3.5 text-xs font-bold text-[#786967]">Trạng thái</th>
              <th class="px-5 py-3.5 text-center text-xs font-bold text-[#786967]">Bài học</th>
              <th class="px-5 py-3.5 text-center text-xs font-bold text-[#786967]">Học viên</th>
              <th class="px-5 py-3.5 text-center text-xs font-bold text-[#786967]">Thứ tự</th>
              <th class="px-5 py-3.5 text-right text-xs font-bold text-[#786967]">Thao tác</th>
            </tr>
          </thead>

          <tbody>
            <tr
              v-for="course in courses"
              :key="course.id"
              class="group border-b border-[#f0eae9] transition last:border-0 hover:bg-[#fcfaf9]"
              :class="course.status === 'ARCHIVED' ? 'bg-[#faf9f8]/70' : ''"
            >
              <td class="px-5 py-4">
                <RouterLink
                  :to="`/admin/courses/${course.id}`"
                  class="flex min-w-0 items-center gap-3"
                  :class="course.status === 'ARCHIVED' ? 'opacity-65' : ''"
                >
                  <span
                    class="grid size-10 shrink-0 place-items-center rounded-lg border border-[#f0dddd] bg-[#fff5f5] text-[#c9152b]"
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
                    <strong
                      class="block truncate text-sm font-bold text-[#302929] transition group-hover:text-[#a0001c]"
                    >
                      {{ course.title }}
                    </strong>
                  </span>
                </RouterLink>
              </td>

              <td class="px-5 py-4">
                <span
                  class="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-bold"
                  :class="statusClass(course.status)"
                >
                  <span class="size-1.5 rounded-full bg-current opacity-75" />
                  {{ statusLabel(course.status) }}
                </span>
              </td>

              <td class="px-5 py-4 text-center text-sm font-semibold text-[#514745]">
                {{ course.lessonCount }}
              </td>

              <td class="px-5 py-4 text-center text-sm font-semibold text-[#514745]">
                {{ course.studentCount }}
              </td>

              <td class="px-5 py-4 text-center">
                <span
                  class="inline-flex min-w-8 justify-center rounded-lg bg-[#f5f1f0] px-2 py-1 text-xs font-bold text-[#6f615f]"
                >
                  {{ course.sortOrder }}
                </span>
              </td>

              <td class="px-5 py-4 text-right">
                <div class="relative inline-block" @click.stop>
                  <button
                    type="button"
                    class="grid size-9 place-items-center rounded-lg text-[#81716f] hover:bg-[#f2eceb] hover:text-[#7f1020]"
                    :aria-expanded="openActionsId === course.id"
                    aria-label="Mở thao tác khóa học"
                    @click="toggleActions(course.id)"
                  >
                    <svg class="size-5" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                      <circle cx="5" cy="12" r="1.7" />
                      <circle cx="12" cy="12" r="1.7" />
                      <circle cx="19" cy="12" r="1.7" />
                    </svg>
                  </button>

                  <div
                    v-if="openActionsId === course.id"
                    class="absolute right-0 top-10 z-20 w-44 overflow-hidden rounded-xl border border-[#e3dada] bg-white p-1.5 text-left shadow-[0_12px_34px_rgba(50,28,28,0.12)]"
                  >
                    <RouterLink
                      :to="`/admin/courses/${course.id}`"
                      class="flex items-center gap-2.5 rounded-lg px-3 py-2.5 text-sm font-semibold text-[#544947] hover:bg-[#f7f3f2] hover:text-[#7f1020]"
                    >
                      <svg
                        class="size-4"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="1.8"
                        aria-hidden="true"
                      >
                        <path d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6Z" />
                        <circle cx="12" cy="12" r="2.5" />
                      </svg>
                      Xem chi tiết
                    </RouterLink>

                    <button
                      v-if="course.status !== 'ARCHIVED'"
                      type="button"
                      :disabled="archivingId === course.id"
                      class="flex w-full items-center gap-2.5 rounded-lg px-3 py-2.5 text-sm font-semibold text-[#a22435] hover:bg-[#fff1f2] disabled:cursor-not-allowed disabled:opacity-50"
                      @click="archiveCourse(course)"
                    >
                      <svg
                        class="size-4"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="1.8"
                        aria-hidden="true"
                      >
                        <path d="M4 7h16M6 7v13h12V7M9 11v5M15 11v5M8 4h8" />
                      </svg>
                      {{ archivingId === course.id ? 'Đang lưu trữ…' : 'Lưu trữ' }}
                    </button>

                    <div
                      v-else
                      class="flex items-center gap-2.5 rounded-lg px-3 py-2.5 text-sm font-semibold text-[#9a8d8b]"
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
                      Đã lưu trữ
                    </div>
                  </div>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Cursor pagination -->
      <div
        v-if="courses.length > 0"
        class="flex flex-col items-center justify-between gap-3 border-t border-[#ece5e4] bg-[#fffefe] px-5 py-4 sm:flex-row"
      >
        <p class="text-xs font-medium text-[#958582]">
          Đang hiển thị {{ courses.length }} khóa học
        </p>

        <button
          v-if="hasMore"
          type="button"
          :disabled="loading"
          class="inline-flex h-9 items-center gap-2 rounded-lg border border-[#d9c4c5] bg-white px-4 text-xs font-bold text-[#a0001c] hover:bg-[#fff4f5] disabled:cursor-not-allowed disabled:opacity-55"
          @click="load(false)"
        >
          <svg
            v-if="loading"
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
          {{ loading ? 'Đang tải…' : 'Tải thêm' }}
        </button>

        <span v-else class="text-xs font-medium text-[#a39492]">Đã hiển thị toàn bộ</span>
      </div>
    </div>

    <!-- Create course modal -->
    <Teleport to="body">
      <div
        v-if="createOpen"
        class="fixed inset-0 z-[100] flex items-center justify-center p-4 sm:p-6"
        role="dialog"
        aria-modal="true"
        aria-labelledby="create-course-title"
      >
        <button
          type="button"
          class="absolute inset-0 bg-[#251719]/45 backdrop-blur-[2px]"
          aria-label="Đóng cửa sổ tạo khóa học"
          @click="closeCreate"
        />

        <div
          class="relative z-10 max-h-[92vh] w-full max-w-[640px] overflow-y-auto rounded-2xl border border-[#e5dada] bg-white shadow-[0_24px_80px_rgba(40,20,23,0.2)]"
        >
          <div class="flex items-start justify-between gap-4 border-b border-[#eee6e5] px-6 py-5">
            <div>
              <p class="text-[11px] font-bold uppercase tracking-[0.16em] text-[#a0001c]">
                Khóa học mới
              </p>
              <h2
                id="create-course-title"
                class="mt-1 text-xl font-black tracking-[-0.02em] text-[#2c2525]"
              >
                Tạo khóa học
              </h2>
              <p class="mt-1 text-sm text-[#81716f]">
                Điền thông tin cơ bản. Bạn có thể thêm bài học sau khi tạo.
              </p>
            </div>

            <button
              type="button"
              :disabled="saving"
              aria-label="Đóng"
              class="grid size-9 shrink-0 place-items-center rounded-lg text-[#8d7d7b] hover:bg-[#f5f0ef] hover:text-[#5e4f4d] disabled:opacity-50"
              @click="closeCreate"
            >
              <svg
                class="size-5"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                aria-hidden="true"
              >
                <path d="M6 6l12 12M18 6 6 18" />
              </svg>
            </button>
          </div>

          <form class="p-6" @submit.prevent="createCourse">
            <div class="space-y-5">
              <label class="block">
                <span class="mb-2 block text-sm font-bold text-[#403635]">
                  Tên khóa học <span class="text-[#c9152b]">*</span>
                </span>
                <input
                  v-model.trim="form.title"
                  required
                  maxlength="200"
                  autofocus
                  placeholder="Ví dụ: Nhập môn Lập trình Web"
                  class="h-11 w-full rounded-xl border border-[#ddd4d3] bg-white px-3.5 text-sm outline-none placeholder:text-[#aa9b99] focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                />
              </label>

              <label class="block">
                <span class="mb-2 block text-sm font-bold text-[#403635]">Mô tả</span>
                <textarea
                  v-model.trim="form.description"
                  maxlength="4000"
                  rows="4"
                  placeholder="Mô tả ngắn về nội dung và mục tiêu của khóa học…"
                  class="w-full resize-y rounded-xl border border-[#ddd4d3] bg-white px-3.5 py-3 text-sm leading-6 outline-none placeholder:text-[#aa9b99] focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                />
                <span class="mt-1.5 block text-right text-[11px] font-medium text-[#a0918f]">
                  {{ form.description.length }}/4000
                </span>
              </label>

              <label class="block">
                <span class="mb-2 block text-sm font-bold text-[#403635]">URL ảnh đại diện</span>
                <input
                  v-model.trim="form.thumbnailUrl"
                  type="url"
                  placeholder="https://example.com/course-cover.jpg"
                  class="h-11 w-full rounded-xl border border-[#ddd4d3] bg-white px-3.5 text-sm outline-none placeholder:text-[#aa9b99] focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                />
              </label>

              <div class="grid gap-4 sm:grid-cols-2">
                <label class="block">
                  <span class="mb-2 block text-sm font-bold text-[#403635]">Trạng thái</span>
                  <select
                    v-model="form.status"
                    class="h-11 w-full rounded-xl border border-[#ddd4d3] bg-white px-3.5 text-sm font-semibold outline-none focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                  >
                    <option value="DRAFT">Bản nháp</option>
                    <option value="PUBLISHED">Đã xuất bản</option>
                  </select>
                </label>

                <label class="block">
                  <span class="mb-2 block text-sm font-bold text-[#403635]">Thứ tự hiển thị</span>
                  <input
                    v-model.number="form.sortOrder"
                    type="number"
                    min="0"
                    required
                    class="h-11 w-full rounded-xl border border-[#ddd4d3] bg-white px-3.5 text-sm font-semibold outline-none focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                  />
                </label>
              </div>

              <div
                class="rounded-xl border border-[#eadfdf] bg-[#faf7f6] px-4 py-3 text-xs leading-5 text-[#786967]"
              >
                <strong class="text-[#5a4e4c]">Lưu ý:</strong>
                Khóa học ở trạng thái “Bản nháp” chưa xuất hiện với học viên. Chỉ xuất bản khi nội
                dung đã sẵn sàng.
              </div>
            </div>

            <div
              class="mt-7 flex flex-col-reverse gap-3 border-t border-[#eee6e5] pt-5 sm:flex-row sm:justify-end"
            >
              <button
                type="button"
                :disabled="saving"
                class="h-10 rounded-lg border border-[#ddd3d2] bg-white px-4 text-sm font-bold text-[#6d5f5d] hover:bg-[#f8f5f4] disabled:opacity-50"
                @click="closeCreate"
              >
                Hủy
              </button>

              <button
                type="submit"
                :disabled="saving"
                class="inline-flex h-10 items-center justify-center gap-2 rounded-lg bg-[#c9152b] px-5 text-sm font-bold text-white hover:bg-[#a0001c] disabled:cursor-not-allowed disabled:opacity-60"
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
                {{ saving ? 'Đang tạo…' : 'Tạo khóa học' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>
  </section>
</template>
