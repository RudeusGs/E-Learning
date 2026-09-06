<script setup lang="ts">
import { computed, onBeforeUnmount, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { toProblem, type ProblemDetails } from '@/api/http'

import type { ProgressRow } from './models/ProgressRow'
import * as progressApi from './progress.api'

const route = useRoute()
const router = useRouter()

const rows = ref<ProgressRow[]>([])
const nextCursor = ref<string | null>(null)
const hasMore = ref(false)
const loading = ref(false)
const problem = ref<ProblemDetails | null>(null)

const filterOpen = ref(false)
const filterError = ref('')

const filters = reactive({
  search: '',
  studentId: '',
  courseId: '',
})

let abortController: AbortController | null = null
let searchTimeout: ReturnType<typeof setTimeout> | null = null

const activeFilterCount = computed(
  () => Number(Boolean(filters.studentId.trim())) + Number(Boolean(filters.courseId.trim())),
)

const hasAnyFilter = computed(
  () =>
    Boolean(filters.search.trim()) ||
    Boolean(filters.studentId.trim()) ||
    Boolean(filters.courseId.trim()),
)

const studentIdValid = computed(
  () => !filters.studentId.trim() || parsePositiveId(filters.studentId) !== null,
)

const courseIdValid = computed(
  () => !filters.courseId.trim() || parsePositiveId(filters.courseId) !== null,
)

function queryString(value: unknown): string {
  return typeof value === 'string' ? value : ''
}

function parsePositiveId(value: string): number | null {
  const normalized = value.trim()
  if (!normalized) return null

  const id = Number(normalized)
  return Number.isSafeInteger(id) && id > 0 ? id : null
}

function isCanceled(error: unknown): boolean {
  return (
    (error as Error)?.name === 'CanceledError' ||
    (error as { code?: string })?.code === 'ERR_CANCELED'
  )
}

function initials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/).filter(Boolean)

  if (parts.length === 0) return 'HV'
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()

  return `${parts[0][0] ?? ''}${parts[parts.length - 1][0] ?? ''}`.toUpperCase()
}

function progressWidth(value: number): string {
  return `${Math.max(0, Math.min(100, value))}%`
}

function progressLabel(row: ProgressRow): string {
  if (row.percentage >= 100) return 'Hoàn thành'
  if (row.completedLessons > 0) return 'Đang học'
  return 'Chưa bắt đầu'
}

function progressLabelClass(row: ProgressRow): string {
  if (row.percentage >= 100) {
    return 'border-[#b9dfcc] bg-[#eef9f3] text-[#16764f]'
  }

  if (row.completedLessons > 0) {
    return 'border-[#efc7cb] bg-[#fff3f4] text-[#a22435]'
  }

  return 'border-[#ddd5d4] bg-[#f2efee] text-[#766967]'
}

function progressBarClass(row: ProgressRow): string {
  if (row.percentage >= 100) return 'bg-[#16875b]'
  if (row.completedLessons > 0) return 'bg-[#c9152b]'
  return 'bg-[#cfc4c2]'
}

async function load(reset = false): Promise<void> {
  if (loading.value && !reset) return

  abortController?.abort()
  abortController = new AbortController()
  const signal = abortController.signal

  loading.value = true
  problem.value = null

  if (reset) {
    rows.value = []
    nextCursor.value = null
    hasMore.value = false
  }

  try {
    const page = await progressApi.getAdminProgress(
      reset ? null : nextCursor.value,
      filters.search.trim(),
      parsePositiveId(filters.studentId) ?? undefined,
      parsePositiveId(filters.courseId) ?? undefined,
      signal,
      20,
    )

    if (signal.aborted) return

    rows.value = reset ? page.items : [...rows.value, ...page.items]
    nextCursor.value = page.nextCursor
    hasMore.value = page.hasMore
  } catch (error) {
    if (isCanceled(error)) return
    problem.value = toProblem(error)
  } finally {
    if (!signal.aborted) loading.value = false
  }
}

async function updateRoute(): Promise<void> {
  filterError.value = ''

  if (!studentIdValid.value || !courseIdValid.value) {
    filterError.value = 'ID học viên và ID khóa học phải là số nguyên dương.'
    return
  }

  const nextQuery = { ...route.query }

  const search = filters.search.trim()
  const studentId = filters.studentId.trim()
  const courseId = filters.courseId.trim()

  if (search) nextQuery.search = search
  else delete nextQuery.search

  if (studentId) nextQuery.studentId = studentId
  else delete nextQuery.studentId

  if (courseId) nextQuery.courseId = courseId
  else delete nextQuery.courseId

  await router.replace({ query: nextQuery })
}

function scheduleSearch(): void {
  if (searchTimeout) {
    window.clearTimeout(searchTimeout)
  }

  searchTimeout = window.setTimeout(() => {
    void updateRoute()
  }, 350)
}

function submitSearch(): void {
  if (searchTimeout) {
    window.clearTimeout(searchTimeout)
    searchTimeout = null
  }

  void updateRoute()
}

function clearSearch(): void {
  filters.search = ''
  submitSearch()
}

function applyFilters(): void {
  if (!studentIdValid.value || !courseIdValid.value) {
    filterError.value = 'ID học viên và ID khóa học phải là số nguyên dương.'
    return
  }

  filterOpen.value = false
  void updateRoute()
}

function clearIdFilters(): void {
  filters.studentId = ''
  filters.courseId = ''
  filterError.value = ''
  filterOpen.value = false
  void updateRoute()
}

function clearAllFilters(): void {
  filters.search = ''
  filters.studentId = ''
  filters.courseId = ''
  filterError.value = ''
  filterOpen.value = false

  if (searchTimeout) {
    window.clearTimeout(searchTimeout)
    searchTimeout = null
  }

  void updateRoute()
}

watch(
  () => [route.query.search, route.query.studentId, route.query.courseId],
  () => {
    filters.search = queryString(route.query.search)
    filters.studentId = queryString(route.query.studentId)
    filters.courseId = queryString(route.query.courseId)
    void load(true)
  },
  { immediate: true },
)

onBeforeUnmount(() => {
  abortController?.abort()

  if (searchTimeout) {
    window.clearTimeout(searchTimeout)
  }
})
</script>

<template>
  <section class="space-y-6">
    <!-- Page heading -->
    <header class="flex flex-col justify-between gap-5 xl:flex-row xl:items-end">
      <div>
        <p class="text-xs font-bold uppercase tracking-[0.16em] text-[#9b8583]">
          Theo dõi kết quả
        </p>
        <h1 class="mt-1.5 text-[30px] font-black tracking-[-0.03em] text-[#1c1b1b] sm:text-[34px]">
          Tiến độ học tập
        </h1>
        <p class="mt-2 max-w-2xl text-sm leading-6 text-[#6b5d5c] sm:text-[15px]">
          Theo dõi số bài học đã hoàn thành và tỷ lệ tiến độ theo từng học viên, từng khóa học.
        </p>
      </div>

      <!-- Search + filters -->
      <div class="flex w-full flex-col gap-3 sm:flex-row xl:w-auto">
        <form class="w-full sm:min-w-[320px] xl:w-[380px]" @submit.prevent="submitSearch">
          <label for="progress-search" class="sr-only">
            Tìm học viên hoặc khóa học
          </label>

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
              id="progress-search"
              v-model="filters.search"
              type="search"
              maxlength="100"
              autocomplete="off"
              placeholder="Tìm học viên hoặc khóa học…"
              class="h-11 w-full rounded-xl border border-[#ddd4d3] bg-white pl-11 pr-10 text-sm text-[#302929] outline-none placeholder:text-[#aa9b99] focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
              @input="scheduleSearch"
            />

            <button
              v-if="filters.search"
              type="button"
              aria-label="Xóa nội dung tìm kiếm"
              class="absolute inset-y-0 right-0 grid w-10 place-items-center text-[#9a8886] transition hover:text-[#7f1020]"
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

        <div class="relative shrink-0">
          <button
            type="button"
            class="inline-flex h-11 w-full items-center justify-center gap-2 rounded-xl border bg-white px-4 text-sm font-bold transition sm:w-auto"
            :class="
              activeFilterCount > 0
                ? 'border-[#d8a5ab] text-[#a0001c] shadow-[0_2px_8px_rgba(160,0,28,0.04)]'
                : 'border-[#ddd4d3] text-[#655856] hover:bg-[#faf7f6]'
            "
            :aria-expanded="filterOpen"
            @click="filterOpen = !filterOpen"
          >
            <svg
              class="size-4.5"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="1.8"
              aria-hidden="true"
            >
              <path d="M4 6h16M7 12h10M10 18h4" />
            </svg>
            Lọc kết quả

            <span
              v-if="activeFilterCount > 0"
              class="grid size-5 place-items-center rounded-full bg-[#c9152b] text-[10px] font-black text-white"
            >
              {{ activeFilterCount }}
            </span>
          </button>

          <!-- Real backend filters -->
          <div
            v-if="filterOpen"
            class="absolute right-0 top-[calc(100%+8px)] z-40 w-[min(360px,calc(100vw-32px))] rounded-2xl border border-[#e2d9d8] bg-white p-5 shadow-[0_18px_55px_rgba(48,25,28,0.15)]"
          >
            <div class="flex items-start justify-between gap-4">
              <div>
                <p class="text-xs font-black uppercase tracking-[0.14em] text-[#9b8583]">
                  Bộ lọc
                </p>
                <h2 class="mt-1 text-base font-black text-[#302929]">Lọc dữ liệu tiến độ</h2>
              </div>

              <button
                type="button"
                aria-label="Đóng bộ lọc"
                class="grid size-8 shrink-0 place-items-center rounded-lg text-[#8d7c7a] hover:bg-[#f4f0ef]"
                @click="filterOpen = false"
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

            <div class="mt-5 space-y-4">
              <label class="block">
                <span class="mb-2 block text-xs font-bold text-[#4f4442]">ID học viên</span>
                <input
                  v-model="filters.studentId"
                  type="number"
                  min="1"
                  step="1"
                  inputmode="numeric"
                  placeholder="Ví dụ: 12"
                  class="h-10 w-full rounded-lg border bg-white px-3 text-sm text-[#302929] outline-none focus:ring-3"
                  :class="
                    studentIdValid
                      ? 'border-[#ddd4d3] focus:border-[#c9152b] focus:ring-[#c9152b]/10'
                      : 'border-[#d95b68] focus:border-[#ba1a1a] focus:ring-[#ba1a1a]/10'
                  "
                />
              </label>

              <label class="block">
                <span class="mb-2 block text-xs font-bold text-[#4f4442]">ID khóa học</span>
                <input
                  v-model="filters.courseId"
                  type="number"
                  min="1"
                  step="1"
                  inputmode="numeric"
                  placeholder="Ví dụ: 3"
                  class="h-10 w-full rounded-lg border bg-white px-3 text-sm text-[#302929] outline-none focus:ring-3"
                  :class="
                    courseIdValid
                      ? 'border-[#ddd4d3] focus:border-[#c9152b] focus:ring-[#c9152b]/10'
                      : 'border-[#d95b68] focus:border-[#ba1a1a] focus:ring-[#ba1a1a]/10'
                  "
                />
              </label>

              <p
                v-if="filterError"
                class="rounded-lg border border-[#efc7cb] bg-[#fff3f4] px-3 py-2 text-xs leading-5 text-[#a22435]"
              >
                {{ filterError }}
              </p>

              <p class="text-[11px] leading-5 text-[#8d7c7a]">
                Đây là filter phía server. Có thể kết hợp ID học viên, ID khóa học và ô tìm kiếm phía trên.
              </p>
            </div>

            <div class="mt-5 flex justify-end gap-2 border-t border-[#eee7e6] pt-4">
              <button
                type="button"
                class="h-9 rounded-lg px-3 text-xs font-bold text-[#756765] hover:bg-[#f3efee]"
                @click="clearIdFilters"
              >
                Xóa bộ lọc
              </button>

              <button
                type="button"
                class="h-9 rounded-lg bg-[#c9152b] px-4 text-xs font-bold text-white hover:bg-[#a0001c]"
                @click="applyFilters"
              >
                Áp dụng
              </button>
            </div>
          </div>
        </div>
      </div>
    </header>

    <ProblemAlert :problem="problem" />

    <!-- Active filter summary -->
    <div
      v-if="hasAnyFilter"
      class="flex flex-col justify-between gap-3 rounded-xl border border-[#eadfdf] bg-[#faf7f6] px-4 py-3 sm:flex-row sm:items-center"
    >
      <div class="flex flex-wrap items-center gap-2">
        <span class="text-xs font-bold text-[#6e605e]">Đang lọc:</span>

        <span
          v-if="filters.search.trim()"
          class="rounded-lg border border-[#e1d6d5] bg-white px-2.5 py-1 text-[11px] font-semibold text-[#665957]"
        >
          Từ khóa: “{{ filters.search.trim() }}”
        </span>

        <span
          v-if="filters.studentId.trim()"
          class="rounded-lg border border-[#e1d6d5] bg-white px-2.5 py-1 text-[11px] font-semibold text-[#665957]"
        >
          Học viên #{{ filters.studentId }}
        </span>

        <span
          v-if="filters.courseId.trim()"
          class="rounded-lg border border-[#e1d6d5] bg-white px-2.5 py-1 text-[11px] font-semibold text-[#665957]"
        >
          Khóa học #{{ filters.courseId }}
        </span>
      </div>

      <button
        type="button"
        class="shrink-0 text-xs font-bold text-[#a0001c] hover:underline"
        @click="clearAllFilters"
      >
        Xóa tất cả
      </button>
    </div>

    <!-- Table card -->
    <div
      class="overflow-hidden rounded-2xl border border-[#e5dedd] bg-white shadow-[0_2px_12px_rgba(50,25,25,0.025)]"
    >
      <!-- Loading skeleton -->
      <div v-if="loading && rows.length === 0" class="overflow-x-auto">
        <table class="w-full min-w-[880px] border-collapse text-left">
          <thead>
            <tr class="border-b border-[#ece5e4] bg-[#faf8f7]">
              <th class="px-5 py-3.5 text-xs font-bold text-[#786967]">Học viên</th>
              <th class="px-5 py-3.5 text-xs font-bold text-[#786967]">Khóa học</th>
              <th class="px-5 py-3.5 text-center text-xs font-bold text-[#786967]">Bài học</th>
              <th class="w-[34%] px-5 py-3.5 text-xs font-bold text-[#786967]">Tiến độ</th>
              <th class="px-5 py-3.5 text-right text-xs font-bold text-[#786967]">Trạng thái</th>
            </tr>
          </thead>

          <tbody>
            <tr
              v-for="index in 7"
              :key="index"
              class="border-b border-[#f0eae9] last:border-0"
            >
              <td class="px-5 py-4">
                <div class="flex items-center gap-3">
                  <div class="size-10 animate-pulse rounded-full bg-[#eee8e7]" />
                  <div>
                    <div class="h-4 w-36 animate-pulse rounded bg-[#ebe4e3]" />
                    <div class="mt-2 h-3 w-16 animate-pulse rounded bg-[#f2edec]" />
                  </div>
                </div>
              </td>

              <td class="px-5 py-4">
                <div class="h-4 w-44 animate-pulse rounded bg-[#eee8e7]" />
              </td>

              <td class="px-5 py-4">
                <div class="mx-auto h-4 w-12 animate-pulse rounded bg-[#eee8e7]" />
              </td>

              <td class="px-5 py-4">
                <div class="flex items-center gap-3">
                  <div class="h-2 flex-1 animate-pulse rounded-full bg-[#eee8e7]" />
                  <div class="h-4 w-10 animate-pulse rounded bg-[#eee8e7]" />
                </div>
              </td>

              <td class="px-5 py-4">
                <div class="ml-auto h-7 w-24 animate-pulse rounded-full bg-[#eee8e7]" />
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Empty -->
      <div
        v-else-if="rows.length === 0"
        class="flex min-h-[380px] flex-col items-center justify-center px-6 py-12 text-center"
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
            <path d="M4 19V9M10 19V5M16 19v-7M22 19H2" />
          </svg>
        </div>

        <h2 class="mt-5 text-lg font-black text-[#342d2d]">
          {{ hasAnyFilter ? 'Không có kết quả phù hợp' : 'Chưa có dữ liệu tiến độ' }}
        </h2>

        <p class="mt-2 max-w-md text-sm leading-6 text-[#81716f]">
          {{
            hasAnyFilter
              ? 'Thử thay đổi từ khóa hoặc bộ lọc để xem dữ liệu khác.'
              : 'Dữ liệu sẽ xuất hiện khi có học viên được phân công khóa học.'
          }}
        </p>

        <button
          v-if="hasAnyFilter"
          type="button"
          class="mt-5 h-10 rounded-lg border border-[#d8a5ab] bg-white px-4 text-sm font-bold text-[#a0001c] hover:bg-[#fff4f5]"
          @click="clearAllFilters"
        >
          Xóa bộ lọc
        </button>
      </div>

      <!-- Data -->
      <div v-else class="overflow-x-auto">
        <table class="w-full min-w-[880px] border-collapse text-left">
          <thead>
            <tr class="border-b border-[#ece5e4] bg-[#faf8f7]">
              <th class="w-[24%] px-5 py-3.5 text-xs font-bold text-[#786967]">Học viên</th>
              <th class="w-[25%] px-5 py-3.5 text-xs font-bold text-[#786967]">Khóa học</th>
              <th class="px-5 py-3.5 text-center text-xs font-bold text-[#786967]">Bài học</th>
              <th class="w-[31%] px-5 py-3.5 text-xs font-bold text-[#786967]">Tiến độ</th>
              <th class="px-5 py-3.5 text-right text-xs font-bold text-[#786967]">Trạng thái</th>
            </tr>
          </thead>

          <tbody>
            <tr
              v-for="row in rows"
              :key="`${row.studentId}-${row.courseId}`"
              class="group border-b border-[#f0eae9] transition last:border-0 hover:bg-[#fcfaf9]"
            >
              <td class="px-5 py-4">
                <RouterLink
                  :to="`/admin/students/${row.studentId}`"
                  class="flex min-w-0 items-center gap-3"
                >
                  <span
                    class="grid size-10 shrink-0 place-items-center rounded-full border border-[#efd9dc] bg-[#fff2f3] text-xs font-black text-[#a0001c]"
                  >
                    {{ initials(row.studentName) }}
                  </span>

                  <span class="min-w-0">
                    <strong
                      class="block truncate text-sm font-bold text-[#302929] transition group-hover:text-[#a0001c]"
                    >
                      {{ row.studentName }}
                    </strong>
                    <span class="mt-1 block text-[11px] font-semibold text-[#9a8987]">
                      ID #{{ row.studentId }}
                    </span>
                  </span>
                </RouterLink>
              </td>

              <td class="px-5 py-4">
                <RouterLink
                  :to="`/admin/courses/${row.courseId}`"
                  class="block max-w-[300px]"
                >
                  <strong
                    class="block truncate text-sm font-bold text-[#504644] transition hover:text-[#a0001c] hover:underline"
                  >
                    {{ row.courseTitle }}
                  </strong>
                  <span class="mt-1 block text-[11px] font-semibold text-[#9a8987]">
                    Khóa học #{{ row.courseId }}
                  </span>
                </RouterLink>
              </td>

              <td class="px-5 py-4 text-center">
                <span class="text-sm font-black text-[#4d4341]">
                  {{ row.completedLessons }}
                </span>
                <span class="mx-1 text-sm text-[#a0908e]">/</span>
                <span class="text-sm font-semibold text-[#756765]">
                  {{ row.totalLessons }}
                </span>
              </td>

              <td class="px-5 py-4">
                <div class="flex items-center gap-4">
                  <div
                    class="h-2.5 min-w-[150px] flex-1 overflow-hidden rounded-full bg-[#eee8e7]"
                    role="progressbar"
                    :aria-valuenow="row.percentage"
                    aria-valuemin="0"
                    aria-valuemax="100"
                    :aria-label="`Tiến độ ${row.percentage}%`"
                  >
                    <div
                      class="h-full rounded-full transition-[width]"
                      :class="progressBarClass(row)"
                      :style="{ width: progressWidth(row.percentage) }"
                    />
                  </div>

                  <strong
                    class="w-12 shrink-0 text-right text-sm tabular-nums"
                    :class="row.percentage >= 100 ? 'text-[#16764f]' : 'text-[#4b403f]'"
                  >
                    {{ row.percentage }}%
                  </strong>
                </div>
              </td>

              <td class="px-5 py-4 text-right">
                <span
                  class="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-bold"
                  :class="progressLabelClass(row)"
                >
                  <span class="size-1.5 rounded-full bg-current opacity-75" />
                  {{ progressLabel(row) }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Cursor pagination -->
      <div
        v-if="rows.length > 0"
        class="flex flex-col items-center justify-between gap-3 border-t border-[#ece5e4] bg-[#fffefe] px-5 py-4 sm:flex-row"
      >
        <p class="text-xs font-medium text-[#958582]">
          Đang hiển thị {{ rows.length }} dòng tiến độ
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

        <span v-else class="text-xs font-medium text-[#a39492]">
          Đã hiển thị toàn bộ kết quả
        </span>
      </div>
    </div>
  </section>
</template>
