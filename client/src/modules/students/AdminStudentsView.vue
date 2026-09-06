<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'

import { toProblem, type ProblemDetails } from '@/api/http'
import type { AccountStatus } from '@/types/AccountStatus'

import type { StudentListItem } from './models/StudentListItem'
import * as studentApi from './student.api'

type StatusFilter = 'ALL' | AccountStatus

const students = ref<StudentListItem[]>([])
const nextCursor = ref<string | null>(null)
const hasMore = ref(false)

const loading = ref(false)
const saving = ref(false)
const copyingPassword = ref(false)

const search = ref('')
const statusFilter = ref<StatusFilter>('ALL')
const problem = ref<ProblemDetails | null>(null)
const drawerProblem = ref<ProblemDetails | null>(null)

const drawerOpen = ref(false)
const showPassword = ref(false)

const createdStudent = ref<{ id: number; fullName: string } | null>(null)

const form = reactive({
  fullName: '',
  email: '',
  initialPassword: '',
})

let searchTimeout: ReturnType<typeof setTimeout> | null = null
let abortController: AbortController | null = null

const statusOptions: Array<{ value: StatusFilter; label: string }> = [
  { value: 'ALL', label: 'Tất cả trạng thái' },
  { value: 'ACTIVE', label: 'Đang hoạt động' },
  { value: 'DISABLED', label: 'Đã vô hiệu hóa' },
]

const formValid = computed(() => {
  return (
    form.fullName.trim().length > 0 &&
    form.fullName.trim().length <= 200 &&
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim()) &&
    form.initialPassword.length >= 10
  )
})

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

function statusLabel(status: AccountStatus): string {
  return status === 'ACTIVE' ? 'Đang hoạt động' : 'Đã vô hiệu hóa'
}

function statusClass(status: AccountStatus): string {
  return status === 'ACTIVE'
    ? 'border-[#b9dfcc] bg-[#eef9f3] text-[#16764f]'
    : 'border-[#ddd5d4] bg-[#f2efee] text-[#766967]'
}

async function load(reset = false): Promise<void> {
  if (loading.value && !reset) return

  abortController?.abort()
  abortController = new AbortController()
  const signal = abortController.signal

  loading.value = true
  if (reset) problem.value = null

  try {
    const page = await studentApi.getStudents(
      reset ? null : nextCursor.value,
      search.value.trim(),
      signal,
      20,
      statusFilter.value === 'ALL' ? null : statusFilter.value,
    )

    if (signal.aborted) return

    students.value = reset ? page.items : [...students.value, ...page.items]
    nextCursor.value = page.nextCursor
    hasMore.value = page.hasMore
  } catch (error) {
    if (isCanceled(error)) return
    problem.value = toProblem(error)
  } finally {
    if (!signal.aborted) loading.value = false
  }
}

function scheduleSearch(): void {
  if (searchTimeout) window.clearTimeout(searchTimeout)

  searchTimeout = window.setTimeout(() => {
    void load(true)
  }, 350)
}

function submitSearch(): void {
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

function setStatusFilter(status: StatusFilter): void {
  if (statusFilter.value === status) return
  statusFilter.value = status
  void load(true)
}

function openCreateDrawer(): void {
  drawerProblem.value = null
  showPassword.value = false
  form.fullName = ''
  form.email = ''
  form.initialPassword = generatePassword()
  drawerOpen.value = true
}

function closeCreateDrawer(): void {
  if (saving.value) return
  drawerOpen.value = false
  drawerProblem.value = null
}

function generatePassword(): string {
  const upper = 'ABCDEFGHJKLMNPQRSTUVWXYZ'
  const lower = 'abcdefghijkmnopqrstuvwxyz'
  const digits = '23456789'
  const symbols = '!@#$%*-_'
  const all = `${upper}${lower}${digits}${symbols}`

  const required = [
    securePick(upper),
    securePick(lower),
    securePick(digits),
    securePick(symbols),
  ]

  while (required.length < 14) {
    required.push(securePick(all))
  }

  for (let i = required.length - 1; i > 0; i -= 1) {
    const j = secureRandomInt(i + 1)
    ;[required[i], required[j]] = [required[j], required[i]]
  }

  return required.join('')
}

function securePick(source: string): string {
  return source[secureRandomInt(source.length)] ?? ''
}

function secureRandomInt(maxExclusive: number): number {
  const values = new Uint32Array(1)
  window.crypto.getRandomValues(values)
  return (values[0] ?? 0) % maxExclusive
}

async function copyPassword(): Promise<void> {
  if (!form.initialPassword || copyingPassword.value) return

  copyingPassword.value = true
  try {
    await navigator.clipboard.writeText(form.initialPassword)
  } finally {
    window.setTimeout(() => {
      copyingPassword.value = false
    }, 1200)
  }
}

async function createStudent(): Promise<void> {
  if (saving.value || !formValid.value) return

  saving.value = true
  drawerProblem.value = null
  createdStudent.value = null

  try {
    const created = await studentApi.createStudent({
      fullName: form.fullName.trim(),
      email: form.email.trim(),
      initialPassword: form.initialPassword,
    })

    createdStudent.value = {
      id: created.id,
      fullName: created.fullName,
    }

    drawerOpen.value = false
    await load(true)
  } catch (error) {
    drawerProblem.value = toProblem(error)
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  void load(true)
})

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
    <header class="flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
      <div>
        <p class="text-xs font-bold uppercase tracking-[0.16em] text-[#9b8583]">
          Quản lý tài khoản
        </p>
        <h1 class="mt-1.5 text-[30px] font-black tracking-[-0.03em] text-[#1c1b1b] sm:text-[34px]">
          Học viên
        </h1>
        <p class="mt-2 max-w-2xl text-sm leading-6 text-[#6b5d5c] sm:text-[15px]">
          Quản lý tài khoản, trạng thái và số khóa học đang được phân công cho học viên.
        </p>
      </div>

      <button
        type="button"
        class="inline-flex h-10 shrink-0 items-center justify-center gap-2 self-start rounded-lg bg-[#c9152b] px-4 text-sm font-bold text-white shadow-[0_6px_16px_rgba(160,0,28,0.13)] transition hover:bg-[#a0001c] focus:outline-none focus-visible:ring-4 focus-visible:ring-[#c9152b]/20 sm:self-auto"
        @click="openCreateDrawer"
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
        Tạo học viên
      </button>
    </header>

    <ProblemAlert :problem="problem" />

    <!-- Created success -->
    <div
      v-if="createdStudent"
      class="flex flex-col justify-between gap-3 rounded-xl border border-[#b9dfcc] bg-[#f0faf5] px-4 py-3.5 sm:flex-row sm:items-center"
    >
      <div class="flex items-start gap-3">
        <span
          class="mt-0.5 grid size-8 shrink-0 place-items-center rounded-lg bg-[#dff3e8] text-[#16764f]"
        >
          <svg
            class="size-4.5"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2.2"
            aria-hidden="true"
          >
            <path d="m6 12 4 4 8-8" />
          </svg>
        </span>
        <div>
          <p class="text-sm font-black text-[#245b43]">
            Đã tạo tài khoản {{ createdStudent.fullName }}
          </p>
          <p class="mt-1 text-xs leading-5 text-[#527363]">
            Tài khoản đã sẵn sàng. Bạn có thể mở hồ sơ để phân công khóa học.
          </p>
        </div>
      </div>

      <RouterLink
        :to="`/admin/students/${createdStudent.id}`"
        class="inline-flex h-9 shrink-0 items-center justify-center rounded-lg border border-[#a9d4be] bg-white px-3 text-xs font-bold text-[#16764f] hover:bg-[#f7fcf9]"
      >
        Xem hồ sơ
      </RouterLink>
    </div>

    <!-- Table container -->
    <div
      class="overflow-hidden rounded-2xl border border-[#e5dedd] bg-white shadow-[0_1px_3px_rgba(44,24,24,0.025)]"
    >
      <!-- Toolbar -->
      <div class="border-b border-[#ece5e4] bg-[#fffefe] p-4 sm:p-5">
        <div class="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
          <form class="w-full xl:max-w-[430px]" @submit.prevent="submitSearch">
            <label for="student-search" class="sr-only">Tìm học viên</label>

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
                id="student-search"
                v-model="search"
                type="search"
                maxlength="100"
                autocomplete="off"
                placeholder="Tìm theo họ tên hoặc email…"
                class="h-11 w-full rounded-xl border border-[#ddd4d3] bg-white pl-11 pr-10 text-sm text-[#302929] outline-none placeholder:text-[#aa9b99] focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                @input="scheduleSearch"
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

          <div class="flex max-w-full gap-1.5 overflow-x-auto pb-1 xl:pb-0">
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
        </div>
      </div>

      <!-- Loading skeleton -->
      <div v-if="loading && students.length === 0" class="overflow-x-auto">
        <table class="w-full min-w-[820px] border-collapse text-left">
          <thead>
            <tr class="border-b border-[#ece5e4] bg-[#faf8f7]">
              <th class="px-5 py-3.5 text-xs font-bold text-[#786967]">Học viên</th>
              <th class="px-5 py-3.5 text-xs font-bold text-[#786967]">Email</th>
              <th class="px-5 py-3.5 text-center text-xs font-bold text-[#786967]">Khóa học</th>
              <th class="px-5 py-3.5 text-xs font-bold text-[#786967]">Trạng thái</th>
              <th class="px-5 py-3.5 text-right text-xs font-bold text-[#786967]">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="index in 6"
              :key="index"
              class="border-b border-[#f0eae9] last:border-0"
            >
              <td class="px-5 py-4">
                <div class="flex items-center gap-3">
                  <div class="size-10 animate-pulse rounded-full bg-[#eee8e7]" />
                  <div class="h-4 w-40 animate-pulse rounded bg-[#ebe4e3]" />
                </div>
              </td>
              <td class="px-5 py-4">
                <div class="h-4 w-44 animate-pulse rounded bg-[#eee8e7]" />
              </td>
              <td class="px-5 py-4">
                <div class="mx-auto h-7 w-16 animate-pulse rounded-lg bg-[#eee8e7]" />
              </td>
              <td class="px-5 py-4">
                <div class="h-7 w-28 animate-pulse rounded-full bg-[#eee8e7]" />
              </td>
              <td class="px-5 py-4">
                <div class="ml-auto h-8 w-20 animate-pulse rounded-lg bg-[#eee8e7]" />
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Empty -->
      <div
        v-else-if="students.length === 0"
        class="flex min-h-[360px] flex-col items-center justify-center px-6 py-12 text-center"
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
            <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
            <circle cx="9" cy="7" r="4" />
            <path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" />
          </svg>
        </div>

        <h2 class="mt-5 text-lg font-black text-[#342d2d]">
          {{
            search || statusFilter !== 'ALL'
              ? 'Không tìm thấy học viên phù hợp'
              : 'Chưa có học viên'
          }}
        </h2>
        <p class="mt-2 max-w-md text-sm leading-6 text-[#81716f]">
          {{
            search || statusFilter !== 'ALL'
              ? 'Thử thay đổi từ khóa hoặc trạng thái để xem kết quả khác.'
              : 'Tạo tài khoản học viên đầu tiên để bắt đầu phân công khóa học.'
          }}
        </p>

        <button
          v-if="!search && statusFilter === 'ALL'"
          type="button"
          class="mt-5 inline-flex h-10 items-center gap-2 rounded-lg bg-[#c9152b] px-4 text-sm font-bold text-white hover:bg-[#a0001c]"
          @click="openCreateDrawer"
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
          Tạo học viên
        </button>
      </div>

      <!-- Table -->
      <div v-else class="overflow-x-auto">
        <table class="w-full min-w-[820px] border-collapse text-left">
          <thead>
            <tr class="border-b border-[#ece5e4] bg-[#faf8f7]">
              <th class="w-[30%] px-5 py-3.5 text-xs font-bold text-[#786967]">Học viên</th>
              <th class="w-[30%] px-5 py-3.5 text-xs font-bold text-[#786967]">Email</th>
              <th class="px-5 py-3.5 text-center text-xs font-bold text-[#786967]">Khóa học</th>
              <th class="px-5 py-3.5 text-xs font-bold text-[#786967]">Trạng thái</th>
              <th class="px-5 py-3.5 text-right text-xs font-bold text-[#786967]">Thao tác</th>
            </tr>
          </thead>

          <tbody>
            <tr
              v-for="student in students"
              :key="student.id"
              class="group border-b border-[#f0eae9] transition last:border-0 hover:bg-[#fcfaf9]"
              :class="student.status === 'DISABLED' ? 'bg-[#faf9f8]/70' : ''"
            >
              <td class="px-5 py-4">
                <RouterLink
                  :to="`/admin/students/${student.id}`"
                  class="flex min-w-0 items-center gap-3"
                  :class="student.status === 'DISABLED' ? 'opacity-65' : ''"
                >
                  <span
                    class="grid size-10 shrink-0 place-items-center rounded-full border border-[#efd9dc] bg-[#fff2f3] text-xs font-black text-[#a0001c]"
                  >
                    {{ initials(student.fullName) }}
                  </span>

                  <span class="min-w-0">
                    <strong
                      class="block truncate text-sm font-bold text-[#302929] transition group-hover:text-[#a0001c]"
                    >
                      {{ student.fullName }}
                    </strong>
                  </span>
                </RouterLink>
              </td>

              <td
                class="max-w-[280px] truncate px-5 py-4 text-sm font-medium"
                :class="student.status === 'DISABLED' ? 'text-[#978987]' : 'text-[#594e4c]'"
              >
                {{ student.email }}
              </td>

              <td class="px-5 py-4 text-center">
                <span
                  class="inline-flex min-w-16 items-center justify-center rounded-lg bg-[#f2eeed] px-2.5 py-1.5 text-xs font-bold text-[#675b59]"
                >
                  {{ student.courseCount }} khóa
                </span>
              </td>

              <td class="px-5 py-4">
                <span
                  class="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-bold"
                  :class="statusClass(student.status)"
                >
                  <span class="size-1.5 rounded-full bg-current opacity-75" />
                  {{ statusLabel(student.status) }}
                </span>
              </td>

              <td class="px-5 py-4 text-right">
                <RouterLink
                  :to="`/admin/students/${student.id}`"
                  class="inline-flex h-9 items-center gap-2 rounded-lg border border-[#ddd3d2] bg-white px-3 text-xs font-bold text-[#665957] transition hover:border-[#d5a9ad] hover:bg-[#fff5f5] hover:text-[#a0001c]"
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
                  Hồ sơ
                </RouterLink>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Cursor pagination -->
      <div
        v-if="students.length > 0"
        class="flex flex-col items-center justify-between gap-3 border-t border-[#ece5e4] bg-[#fffefe] px-5 py-4 sm:flex-row"
      >
        <p class="text-xs font-medium text-[#958582]">
          Đang hiển thị {{ students.length }} học viên
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
          Đã hiển thị toàn bộ
        </span>
      </div>
    </div>

    <!-- Create student drawer -->
    <Teleport to="body">
      <div v-if="drawerOpen" class="fixed inset-0 z-[100]">
        <button
          type="button"
          aria-label="Đóng form tạo học viên"
          class="absolute inset-0 bg-[#241719]/45 backdrop-blur-[2px]"
          @click="closeCreateDrawer"
        />

        <aside
          class="absolute inset-y-0 right-0 flex w-full max-w-[500px] flex-col border-l border-[#e4dada] bg-white shadow-[-18px_0_60px_rgba(42,19,23,0.16)]"
          role="dialog"
          aria-modal="true"
          aria-labelledby="create-student-title"
        >
          <!-- Drawer header -->
          <div class="flex items-start justify-between gap-4 border-b border-[#ece5e4] bg-[#faf8f7] px-5 py-5 sm:px-6">
            <div>
              <p class="text-[10px] font-black uppercase tracking-[0.16em] text-[#a0001c]">
                Tài khoản học viên
              </p>
              <h2
                id="create-student-title"
                class="mt-1 text-xl font-black tracking-[-0.02em] text-[#302929]"
              >
                Tạo học viên mới
              </h2>
              <p class="mt-1 text-xs leading-5 text-[#81716f]">
                Nhập thông tin cơ bản để cấp tài khoản đăng nhập.
              </p>
            </div>

            <button
              type="button"
              :disabled="saving"
              aria-label="Đóng"
              class="grid size-9 shrink-0 place-items-center rounded-lg text-[#887875] hover:bg-[#f0eae9] hover:text-[#584c4a] disabled:opacity-50"
              @click="closeCreateDrawer"
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

          <!-- Drawer body -->
          <form
            id="create-student-form"
            class="flex-1 overflow-y-auto px-5 py-6 sm:px-6"
            @submit.prevent="createStudent"
          >
            <div
              v-if="drawerProblem"
              class="mb-5 rounded-xl border border-[#efc3c7] bg-[#fff1f2] px-4 py-3"
            >
              <p class="text-sm font-black text-[#a22435]">
                {{ drawerProblem.title || 'Không thể tạo học viên' }}
              </p>
              <p v-if="drawerProblem.detail" class="mt-1 text-xs leading-5 text-[#87545a]">
                {{ drawerProblem.detail }}
              </p>
            </div>

            <div class="space-y-5">
              <label class="block">
                <span
                  class="mb-2 flex items-center justify-between gap-3 text-sm font-bold text-[#403735]"
                >
                  <span>Họ và tên <span class="text-[#c9152b]">*</span></span>
                  <span class="text-[11px] font-semibold text-[#a0918f]">
                    {{ form.fullName.length }}/200
                  </span>
                </span>

                <input
                  v-model="form.fullName"
                  required
                  maxlength="200"
                  autocomplete="name"
                  placeholder="Ví dụ: Nguyễn Văn An"
                  class="h-11 w-full rounded-xl border border-[#ddd4d3] bg-white px-3.5 text-sm text-[#302929] outline-none placeholder:text-[#aa9b99] focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                />
              </label>

              <label class="block">
                <span class="mb-2 block text-sm font-bold text-[#403735]">
                  Email <span class="text-[#c9152b]">*</span>
                </span>

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
                      <rect x="3" y="5" width="18" height="14" rx="2" />
                      <path d="m4 7 8 6 8-6" />
                    </svg>
                  </span>

                  <input
                    v-model="form.email"
                    required
                    type="email"
                    autocomplete="email"
                    placeholder="student@school.edu.vn"
                    class="h-11 w-full rounded-xl border border-[#ddd4d3] bg-white pl-11 pr-3.5 text-sm text-[#302929] outline-none placeholder:text-[#aa9b99] focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                  />
                </div>

                <p class="mt-2 text-xs leading-5 text-[#8d7c7a]">
                  Email được dùng làm tên đăng nhập và phải là duy nhất trong hệ thống.
                </p>
              </label>

              <div>
                <div class="mb-2 flex items-end justify-between gap-3">
                  <label class="text-sm font-bold text-[#403735]" for="student-password">
                    Mật khẩu ban đầu <span class="text-[#c9152b]">*</span>
                  </label>

                  <button
                    type="button"
                    class="text-[11px] font-bold text-[#a0001c] hover:underline"
                    @click="form.initialPassword = generatePassword()"
                  >
                    Tạo mật khẩu mới
                  </button>
                </div>

                <div class="relative">
                  <input
                    id="student-password"
                    v-model="form.initialPassword"
                    required
                    minlength="10"
                    :type="showPassword ? 'text' : 'password'"
                    autocomplete="new-password"
                    class="h-11 w-full rounded-xl border border-[#ddd4d3] bg-white pl-3.5 pr-[76px] font-mono text-sm tracking-wide text-[#302929] outline-none focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                  />

                  <div class="absolute inset-y-0 right-1.5 flex items-center">
                    <button
                      type="button"
                      class="grid size-8 place-items-center rounded-lg text-[#8d7c7a] hover:bg-[#f5f1f0] hover:text-[#5d504e]"
                      :aria-label="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
                      @click="showPassword = !showPassword"
                    >
                      <svg
                        v-if="!showPassword"
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
                      <svg
                        v-else
                        class="size-4"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="1.8"
                        aria-hidden="true"
                      >
                        <path d="m3 3 18 18M10.6 10.6a2 2 0 0 0 2.8 2.8M9.9 4.3A10 10 0 0 1 12 4c6 0 9.5 8 9.5 8a16 16 0 0 1-2.1 3.1M6.2 6.2C3.8 7.8 2.5 12 2.5 12S6 20 12 20a9.7 9.7 0 0 0 4.1-.9" />
                      </svg>
                    </button>

                    <button
                      type="button"
                      class="grid size-8 place-items-center rounded-lg text-[#8d7c7a] hover:bg-[#f5f1f0] hover:text-[#5d504e]"
                      :title="copyingPassword ? 'Đã sao chép' : 'Sao chép mật khẩu'"
                      @click="copyPassword"
                    >
                      <svg
                        class="size-4"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="1.8"
                        aria-hidden="true"
                      >
                        <rect x="8" y="8" width="11" height="11" rx="2" />
                        <path d="M16 8V5a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h3" />
                      </svg>
                    </button>
                  </div>
                </div>

                <div class="mt-2 flex items-start gap-2">
                  <svg
                    class="mt-0.5 size-4 shrink-0 text-[#8f7e7c]"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="1.8"
                    aria-hidden="true"
                  >
                    <circle cx="12" cy="12" r="9" />
                    <path d="M12 10.5V17M12 7h.01" />
                  </svg>
                  <p class="text-xs leading-5 text-[#8d7c7a]">
                    Tối thiểu 10 ký tự. Hãy chuyển mật khẩu ban đầu cho học viên qua kênh phù hợp.
                  </p>
                </div>
              </div>

              <div
                class="rounded-xl border border-[#eadfdf] bg-[#faf7f6] px-4 py-3.5 text-xs leading-5 text-[#786967]"
              >
                <strong class="text-[#544846]">Lưu ý:</strong>
                backend hiện chưa có chức năng bắt buộc đổi mật khẩu ở lần đăng nhập đầu tiên, nên form này không hiển thị tùy chọn giả đó.
              </div>
            </div>
          </form>

          <!-- Drawer footer -->
          <div
            class="flex flex-col-reverse gap-3 border-t border-[#ece5e4] bg-[#faf8f7] px-5 py-4 sm:flex-row sm:justify-end sm:px-6"
          >
            <button
              type="button"
              :disabled="saving"
              class="h-10 rounded-lg border border-[#d9cfce] bg-white px-4 text-sm font-bold text-[#6e605e] hover:bg-[#f4f0ef] disabled:opacity-50"
              @click="closeCreateDrawer"
            >
              Hủy
            </button>

            <button
              type="submit"
              form="create-student-form"
              :disabled="saving || !formValid"
              class="inline-flex h-10 items-center justify-center gap-2 rounded-lg bg-[#c9152b] px-5 text-sm font-bold text-white shadow-[0_5px_14px_rgba(160,0,28,0.12)] hover:bg-[#a0001c] disabled:cursor-not-allowed disabled:opacity-50"
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
              {{ saving ? 'Đang tạo…' : 'Tạo tài khoản' }}
            </button>
          </div>
        </aside>
      </div>
    </Teleport>
  </section>
</template>
