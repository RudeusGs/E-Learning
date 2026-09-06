<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useRoute } from 'vue-router'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'
import * as courseApi from '@/modules/courses/course.api'
import type { AdminCourseListItem } from '@/modules/courses/models/AdminCourseListItem'
import type { ProgressRow } from '@/modules/progress/models/ProgressRow'
import * as progressApi from '@/modules/progress/progress.api'

import type { StudentDetail } from './models/StudentDetail'
import type { StudentEnrollment } from './models/StudentEnrollment'
import * as studentApi from './student.api'

type EnrollmentFilter = 'ALL' | 'ACTIVE' | 'INACTIVE'

const route = useRoute()
const studentId = computed(() => Number(route.params.studentId))

const student = ref<StudentDetail | null>(null)
const progressRows = ref<ProgressRow[]>([])
const courses = ref<AdminCourseListItem[]>([])

const fullName = ref('')
const courseSearch = ref('')
const selectedCourseId = ref<number | null>(null)
const enrollmentFilter = ref<EnrollmentFilter>('ALL')
const assignPanelOpen = ref(false)

const loading = ref(true)
const savingProfile = ref(false)
const disabling = ref(false)
const enrolling = ref(false)
const courseLoading = ref(false)

const problem = ref<ProblemDetails | null>(null)
const courseProblem = ref<ProblemDetails | null>(null)
const successMessage = ref('')

let courseSearchTimeout: ReturnType<typeof setTimeout> | null = null
let courseAbortController: AbortController | null = null

const activeEnrollments = computed(
  () => student.value?.enrollments.filter((item) => item.status === 'ACTIVE') ?? [],
)

const inactiveEnrollments = computed(
  () => student.value?.enrollments.filter((item) => item.status === 'INACTIVE') ?? [],
)

const displayedEnrollments = computed(() => {
  const all = student.value?.enrollments ?? []

  if (enrollmentFilter.value === 'ACTIVE') {
    return all.filter((item) => item.status === 'ACTIVE')
  }

  if (enrollmentFilter.value === 'INACTIVE') {
    return all.filter((item) => item.status === 'INACTIVE')
  }

  return all
})

const activeCourseIds = computed(
  () => new Set(activeEnrollments.value.map((item) => item.courseId)),
)

const availableCourses = computed(() =>
  courses.value.filter(
    (course) =>
      course.status !== 'ARCHIVED' &&
      !activeCourseIds.value.has(course.id),
  ),
)

const selectedCourse = computed(
  () => availableCourses.value.find((course) => course.id === selectedCourseId.value) ?? null,
)

const selectedExistingEnrollment = computed(() => {
  if (!selectedCourse.value || !student.value) return null

  return (
    student.value.enrollments.find(
      (item) =>
        item.courseId === selectedCourse.value?.id &&
        item.status === 'INACTIVE',
    ) ?? null
  )
})

const nameValid = computed(() => {
  const value = fullName.value.trim()
  return value.length > 0 && value.length <= 200
})

const hasNameChange = computed(
  () => Boolean(student.value) && fullName.value.trim() !== student.value?.fullName,
)

const progressByCourseId = computed(() => {
  const map = new Map<number, ProgressRow>()

  for (const row of progressRows.value) {
    map.set(row.courseId, row)
  }

  return map
})

function initials(fullNameValue: string): string {
  const parts = fullNameValue.trim().split(/\s+/).filter(Boolean)

  if (parts.length === 0) return 'HV'
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()

  return `${parts[0][0] ?? ''}${parts[parts.length - 1][0] ?? ''}`.toUpperCase()
}

function statusLabel(status: StudentDetail['status']): string {
  return status === 'ACTIVE' ? 'Đang hoạt động' : 'Đã vô hiệu hóa'
}

function enrollmentStatusLabel(status: StudentEnrollment['status']): string {
  return status === 'ACTIVE' ? 'Đang gán' : 'Đã dừng'
}

function courseStatusLabel(status: AdminCourseListItem['status']): string {
  if (status === 'PUBLISHED') return 'Đã xuất bản'
  if (status === 'DRAFT') return 'Bản nháp'
  return 'Đã lưu trữ'
}

function progressFor(courseId: number): ProgressRow | null {
  return progressByCourseId.value.get(courseId) ?? null
}

function progressWidth(value: number): string {
  return `${Math.max(0, Math.min(100, value))}%`
}

function isCanceled(error: unknown): boolean {
  return (
    (error as Error)?.name === 'CanceledError' ||
    (error as { code?: string })?.code === 'ERR_CANCELED'
  )
}

async function loadStudent(): Promise<void> {
  if (!Number.isFinite(studentId.value) || studentId.value <= 0) {
    problem.value = {
      status: 400,
      code: 'INVALID_STUDENT_ID',
      title: 'Học viên không hợp lệ',
      detail: 'Mã học viên không hợp lệ.',
    }
    loading.value = false
    return
  }

  loading.value = true
  problem.value = null
  successMessage.value = ''

  try {
    const data = await studentApi.getStudent(studentId.value)

    student.value = data
    fullName.value = data.fullName

    await Promise.all([
      loadProgress(),
      loadCourses(),
    ])
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}

async function loadProgress(): Promise<void> {
  const items: ProgressRow[] = []
  let cursor: string | null = null
  let hasMore = true

  while (hasMore) {
    const page = await progressApi.getAdminProgress(
      cursor,
      undefined,
      studentId.value,
      undefined,
    )

    items.push(...page.items)
    cursor = page.nextCursor
    hasMore = page.hasMore && Boolean(cursor)
  }

  progressRows.value = items
}

async function loadCourses(): Promise<void> {
  courseAbortController?.abort()
  courseAbortController = new AbortController()
  const signal = courseAbortController.signal

  courseLoading.value = true
  courseProblem.value = null

  try {
    const page = await courseApi.getAdminCourses(
      null,
      courseSearch.value.trim(),
      signal,
      20,
    )

    if (signal.aborted) return

    courses.value = page.items

    if (
      selectedCourseId.value !== null &&
      !availableCourses.value.some((course) => course.id === selectedCourseId.value)
    ) {
      selectedCourseId.value = null
    }
  } catch (error) {
    if (isCanceled(error)) return
    courseProblem.value = toProblem(error)
  } finally {
    if (!signal.aborted) {
      courseLoading.value = false
    }
  }
}

function scheduleCourseSearch(): void {
  if (courseSearchTimeout) {
    window.clearTimeout(courseSearchTimeout)
  }

  courseSearchTimeout = window.setTimeout(() => {
    selectedCourseId.value = null
    void loadCourses()
  }, 350)
}

function clearCourseSearch(): void {
  courseSearch.value = ''
  selectedCourseId.value = null

  if (courseSearchTimeout) {
    window.clearTimeout(courseSearchTimeout)
    courseSearchTimeout = null
  }

  void loadCourses()
}


function openAssignPanel(): void {
  if (!student.value || student.value.status === 'DISABLED') return

  courseProblem.value = null
  selectedCourseId.value = null
  assignPanelOpen.value = true
}

function closeAssignPanel(): void {
  if (enrolling.value) return

  assignPanelOpen.value = false
  selectedCourseId.value = null
  courseProblem.value = null
}

function handleAssignPanelKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape') {
    closeAssignPanel()
  }
}

async function saveProfile(): Promise<void> {
  if (
    !student.value ||
    savingProfile.value ||
    !nameValid.value ||
    !hasNameChange.value
  ) {
    return
  }

  savingProfile.value = true
  problem.value = null
  successMessage.value = ''

  try {
    const updated = await studentApi.updateStudent(
      student.value.id,
      fullName.value.trim(),
    )

    student.value = updated
    fullName.value = updated.fullName
    successMessage.value = 'Đã cập nhật thông tin học viên.'
  } catch (error) {
    problem.value = toProblem(error)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    savingProfile.value = false
  }
}

async function disableStudent(): Promise<void> {
  if (
    !student.value ||
    student.value.status === 'DISABLED' ||
    disabling.value
  ) {
    return
  }

  const confirmed = window.confirm(
    `Vô hiệu hóa tài khoản "${student.value.fullName}"?\n\nHọc viên sẽ không thể đăng nhập. API hiện tại không có thao tác kích hoạt lại tài khoản từ trang quản trị.`,
  )

  if (!confirmed) return

  disabling.value = true
  problem.value = null
  successMessage.value = ''

  try {
    await studentApi.disableStudent(student.value.id)
    student.value = await studentApi.getStudent(student.value.id)
    fullName.value = student.value.fullName
    successMessage.value = 'Tài khoản học viên đã được vô hiệu hóa.'
  } catch (error) {
    problem.value = toProblem(error)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    disabling.value = false
  }
}

async function enrollSelectedCourse(): Promise<void> {
  if (
    !student.value ||
    student.value.status === 'DISABLED' ||
    !selectedCourse.value ||
    enrolling.value
  ) {
    return
  }

  enrolling.value = true
  courseProblem.value = null
  successMessage.value = ''

  try {
    await studentApi.enrollStudent(student.value.id, selectedCourse.value.id)

    const actionLabel = selectedExistingEnrollment.value ? 'Gán lại' : 'Gán'
    const courseTitle = selectedCourse.value.title

    student.value = await studentApi.getStudent(student.value.id)
    selectedCourseId.value = null
    await Promise.all([loadProgress(), loadCourses()])

    successMessage.value = `${actionLabel} khóa học "${courseTitle}" thành công.`
    assignPanelOpen.value = false
  } catch (error) {
    courseProblem.value = toProblem(error)
  } finally {
    enrolling.value = false
  }
}

async function reactivateEnrollment(enrollment: StudentEnrollment): Promise<void> {
  if (
    !student.value ||
    student.value.status === 'DISABLED' ||
    enrollment.status !== 'INACTIVE' ||
    enrolling.value
  ) {
    return
  }

  enrolling.value = true
  problem.value = null
  successMessage.value = ''

  try {
    await studentApi.enrollStudent(student.value.id, enrollment.courseId)
    student.value = await studentApi.getStudent(student.value.id)
    await Promise.all([loadProgress(), loadCourses()])
    successMessage.value = `Đã gán lại khóa học "${enrollment.courseTitle}".`
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    enrolling.value = false
  }
}

watch(assignPanelOpen, (open) => {
  document.body.style.overflow = open ? 'hidden' : ''

  if (open) {
    window.addEventListener('keydown', handleAssignPanelKeydown)
  } else {
    window.removeEventListener('keydown', handleAssignPanelKeydown)
  }
})

watch(
  () => route.params.studentId,
  () => {
    assignPanelOpen.value = false
    void loadStudent()
  },
  { immediate: true },
)

onBeforeUnmount(() => {
  courseAbortController?.abort()
  window.removeEventListener('keydown', handleAssignPanelKeydown)
  document.body.style.overflow = ''

  if (courseSearchTimeout) {
    window.clearTimeout(courseSearchTimeout)
  }
})
</script>

<template>
  <section class="space-y-6">
    <!-- Loading -->
    <div v-if="loading" class="space-y-6">
      <div class="h-4 w-52 animate-pulse rounded bg-[#eae3e2]" />
      <div class="h-40 animate-pulse rounded-2xl bg-[#eee8e7]" />

      <div class="grid gap-6 xl:grid-cols-[360px_minmax(0,1fr)]">
        <div class="space-y-6">
          <div class="h-80 animate-pulse rounded-2xl bg-[#eee8e7]" />
        </div>
        <div class="h-[620px] animate-pulse rounded-2xl bg-[#eee8e7]" />
      </div>
    </div>

    <template v-else>
      <ProblemAlert :problem="problem" />

      <div
        v-if="successMessage"
        class="flex items-center gap-3 rounded-xl border border-[#b9dfcc] bg-[#f0faf5] px-4 py-3.5 text-sm font-bold text-[#245b43]"
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
        {{ successMessage }}
      </div>

      <template v-if="student">
        <!-- Breadcrumb -->
        <nav
          aria-label="Breadcrumb"
          class="flex flex-wrap items-center gap-2 text-xs font-semibold text-[#8d7d7b]"
        >
          <RouterLink to="/admin/students" class="transition hover:text-[#a0001c]">
            Học viên
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
          <span class="text-[#403735]">Chi tiết học viên</span>
        </nav>

        <!-- Student profile header -->
        <section
          class="relative overflow-hidden rounded-2xl border border-[#e5dedd] bg-white p-5 shadow-[0_2px_14px_rgba(50,25,25,0.025)] sm:p-6"
        >
          <div class="absolute inset-x-0 top-0 h-1 bg-[#c9152b]" />

          <div class="flex flex-col justify-between gap-6 md:flex-row md:items-center">
            <div class="flex min-w-0 flex-col gap-4 sm:flex-row sm:items-center sm:gap-5">
              <span
                class="grid size-20 shrink-0 place-items-center rounded-full border border-[#ead7d9] bg-[#fff2f3] text-xl font-black text-[#a0001c]"
              >
                {{ initials(student.fullName) }}
              </span>

              <div class="min-w-0">
                <div class="flex flex-wrap items-center gap-3">
                  <h1
                    class="truncate text-[28px] font-black tracking-[-0.03em] text-[#1c1b1b] sm:text-[32px]"
                  >
                    {{ student.fullName }}
                  </h1>

                  <span
                    class="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-bold"
                    :class="
                      student.status === 'ACTIVE'
                        ? 'border-[#b9dfcc] bg-[#eef9f3] text-[#16764f]'
                        : 'border-[#ddd5d4] bg-[#f2efee] text-[#766967]'
                    "
                  >
                    <span class="size-1.5 rounded-full bg-current opacity-75" />
                    {{ statusLabel(student.status) }}
                  </span>
                </div>

                <div class="mt-2 flex flex-wrap items-center gap-x-5 gap-y-2 text-sm text-[#756765]">
                  <span class="inline-flex min-w-0 items-center gap-2">
                    <svg
                      class="size-4.5 shrink-0 text-[#9b8987]"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="1.8"
                      aria-hidden="true"
                    >
                      <rect x="3" y="5" width="18" height="14" rx="2" />
                      <path d="m4 7 8 6 8-6" />
                    </svg>
                    <span class="truncate">{{ student.email }}</span>
                  </span>


                  <span class="text-xs font-semibold text-[#9a8987]">
                    {{ activeEnrollments.length }} khóa đang gán
                  </span>
                </div>
              </div>
            </div>

            <div class="flex w-full flex-col gap-2 sm:w-auto sm:flex-row md:self-auto">
              <button
                type="button"
                :disabled="student.status === 'DISABLED'"
                class="inline-flex h-10 items-center justify-center gap-2 rounded-lg bg-[#c9152b] px-4 text-sm font-bold text-white shadow-[0_6px_16px_rgba(160,0,28,0.12)] transition hover:bg-[#a0001c] disabled:cursor-not-allowed disabled:bg-[#d8cfce] disabled:text-[#817775] disabled:shadow-none"
                @click="openAssignPanel"
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
                Gán khóa học
              </button>

              <RouterLink
                :to="{ path: '/admin/progress', query: { studentId: String(student.id) } }"
                class="inline-flex h-10 items-center justify-center gap-2 rounded-lg border border-[#d5a3a9] bg-white px-4 text-sm font-bold text-[#a0001c] transition hover:bg-[#fff3f4]"
              >
                <svg
                  class="size-4"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="1.8"
                  aria-hidden="true"
                >
                  <path d="M4 19V9M10 19V5M16 19v-7M22 19H2" />
                </svg>
                Xem tiến độ
              </RouterLink>
            </div>
          </div>
        </section>

        <div class="grid gap-6 xl:grid-cols-[360px_minmax(0,1fr)] xl:items-start">
          <!-- Left column -->
          <div class="space-y-6 xl:sticky xl:top-6">
            <!-- Student info -->
            <section
              class="rounded-2xl border border-[#e5dedd] bg-white p-5 shadow-[0_2px_12px_rgba(50,25,25,0.025)] sm:p-6"
            >
              <div class="border-b border-[#eee7e6] pb-4">
                <h2 class="text-lg font-black tracking-[-0.02em] text-[#302929]">
                  Thông tin học viên
                </h2>
              </div>

              <form class="mt-5 space-y-5" @submit.prevent="saveProfile">
                <label class="block">
                  <span
                    class="mb-2 flex items-center justify-between gap-3 text-sm font-bold text-[#403735]"
                  >
                    <span>Họ và tên <span class="text-[#c9152b]">*</span></span>
                    <span
                      class="text-[11px] font-semibold"
                      :class="fullName.length > 200 ? 'text-[#ba1a1a]' : 'text-[#a0918f]'"
                    >
                      {{ fullName.length }}/200
                    </span>
                  </span>

                  <input
                    v-model="fullName"
                    required
                    maxlength="200"
                    autocomplete="name"
                    class="h-11 w-full rounded-xl border border-[#ddd4d3] bg-white px-3.5 text-sm text-[#302929] outline-none focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                  />
                </label>

                <label class="block">
                  <span class="mb-2 block text-sm font-bold text-[#403735]">Email</span>
                  <input
                    :value="student.email"
                    readonly
                    type="email"
                    class="h-11 w-full cursor-not-allowed rounded-xl border border-[#e2dada] bg-[#f5f2f1] px-3.5 text-sm text-[#827472]"
                  />
                  <span class="mt-1.5 block text-[11px] leading-4 text-[#9a8987]">
                    API hiện tại không hỗ trợ đổi email học viên.
                  </span>
                </label>

                <div>
                  <span class="mb-2 block text-sm font-bold text-[#403735]">
                    Trạng thái tài khoản
                  </span>

                  <div
                    class="flex items-center justify-between gap-3 rounded-xl border px-3.5 py-3"
                    :class="
                      student.status === 'ACTIVE'
                        ? 'border-[#cce2d6] bg-[#f4faf7]'
                        : 'border-[#e0d9d8] bg-[#f4f1f0]'
                    "
                  >
                    <div class="flex items-center gap-2.5">
                      <span
                        class="size-2 rounded-full"
                        :class="student.status === 'ACTIVE' ? 'bg-[#16875b]' : 'bg-[#938582]'"
                      />
                      <span class="text-sm font-black text-[#4e4442]">
                        {{ statusLabel(student.status) }}
                      </span>
                    </div>

                    <span class="text-[10px] font-bold uppercase tracking-[0.12em] text-[#958582]">
                      {{ student.status }}
                    </span>
                  </div>
                </div>

                <button
                  type="submit"
                  :disabled="savingProfile || !nameValid || !hasNameChange"
                  class="inline-flex h-10 w-full items-center justify-center gap-2 rounded-lg bg-[#c9152b] px-4 text-sm font-bold text-white transition hover:bg-[#a0001c] disabled:cursor-not-allowed disabled:opacity-50"
                >
                  <svg
                    v-if="savingProfile"
                    class="size-4 animate-spin"
                    viewBox="0 0 24 24"
                    fill="none"
                    aria-hidden="true"
                  >
                    <circle class="opacity-30" cx="12" cy="12" r="9" stroke="currentColor" stroke-width="3" />
                    <path d="M21 12a9 9 0 0 0-9-9" stroke="currentColor" stroke-width="3" stroke-linecap="round" />
                  </svg>
                  {{ savingProfile ? 'Đang lưu…' : 'Lưu thay đổi' }}
                </button>
              </form>

              <div
                v-if="student.status === 'ACTIVE'"
                class="mt-6 border-t border-[#eee7e6] pt-5"
              >
                <p class="text-xs font-black text-[#9d1d2a]">Quản lý tài khoản</p>
                <p class="mt-1.5 text-xs leading-5 text-[#81716f]">
                  Vô hiệu hóa sẽ chặn đăng nhập và thu hồi phiên hiện tại. Contract quản trị hiện chưa có thao tác kích hoạt lại.
                </p>

                <button
                  type="button"
                  :disabled="disabling"
                  class="mt-4 inline-flex h-9 items-center gap-2 rounded-lg border border-[#dc8e97] bg-white px-3.5 text-xs font-bold text-[#a22435] transition hover:bg-[#fff1f2] disabled:opacity-50"
                  @click="disableStudent"
                >
                  <svg
                    class="size-4"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="1.8"
                    aria-hidden="true"
                  >
                    <circle cx="12" cy="12" r="9" />
                    <path d="M8 12h8" />
                  </svg>
                  {{ disabling ? 'Đang vô hiệu hóa…' : 'Vô hiệu hóa tài khoản' }}
                </button>
              </div>
            </section>

          </div>

          <!-- Assigned courses -->
          <section
            class="min-w-0 overflow-hidden rounded-2xl border border-[#e5dedd] bg-white shadow-[0_2px_12px_rgba(50,25,25,0.025)]"
          >
            <div class="border-b border-[#eee7e6] px-5 py-5 sm:px-6">
              <div class="flex flex-col justify-between gap-4 lg:flex-row lg:items-center">
                <div>
                  <h2 class="text-lg font-black tracking-[-0.02em] text-[#302929]">
                    Khóa học đã gán
                    <span class="text-[#9a8987]">({{ student.enrollments.length }})</span>
                  </h2>
                </div>

                <button
                  type="button"
                  :disabled="student.status === 'DISABLED'"
                  class="inline-flex h-9 shrink-0 items-center justify-center gap-2 rounded-lg bg-[#c9152b] px-3.5 text-xs font-bold text-white transition hover:bg-[#a0001c] disabled:cursor-not-allowed disabled:bg-[#d8cfce] disabled:text-[#817775]"
                  @click="openAssignPanel"
                >
                  <svg class="size-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                    <path d="M12 5v14M5 12h14" />
                  </svg>
                  Gán khóa học
                </button>
              </div>

              <div class="mt-4 flex max-w-full gap-1.5 overflow-x-auto">
                <button
                  v-for="option in [
                    { value: 'ALL', label: 'Tất cả', count: student.enrollments.length },
                    { value: 'ACTIVE', label: 'Đang gán', count: activeEnrollments.length },
                    { value: 'INACTIVE', label: 'Đã dừng', count: inactiveEnrollments.length },
                  ]"
                  :key="option.value"
                  type="button"
                  class="h-8 shrink-0 rounded-lg border px-2.5 text-[11px] font-bold transition"
                  :class="
                    enrollmentFilter === option.value
                      ? 'border-[#d9a8ae] bg-[#fff1f2] text-[#a0001c]'
                      : 'border-[#e4dcdb] bg-white text-[#756765] hover:bg-[#faf7f7]'
                  "
                  @click="enrollmentFilter = option.value as EnrollmentFilter"
                >
                  {{ option.label }} · {{ option.count }}
                </button>
              </div>
            </div>

            <div class="bg-[#fcfbfa] p-4 sm:p-6">
              <div
                v-if="displayedEnrollments.length === 0"
                class="flex min-h-[360px] flex-col items-center justify-center rounded-xl border border-dashed border-[#d9cecd] bg-white px-6 text-center"
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

                <h3 class="mt-4 text-base font-black text-[#403735]">
                  {{
                    enrollmentFilter === 'ALL'
                      ? 'Chưa có khóa học được gán'
                      : 'Không có khóa học ở trạng thái này'
                  }}
                </h3>
                <p class="mt-2 max-w-sm text-xs leading-5 text-[#81716f]">
                  {{
                    enrollmentFilter === 'ALL'
                      ? 'Nhấn Gán khóa học để thêm enrollment đầu tiên cho học viên.'
                      : 'Chọn bộ lọc khác để xem các enrollment còn lại.'
                  }}
                </p>
              </div>

              <div v-else class="space-y-4">
                <article
                  v-for="enrollment in displayedEnrollments"
                  :key="enrollment.id"
                  class="relative overflow-hidden rounded-xl border bg-white p-4 transition sm:p-5"
                  :class="
                    enrollment.status === 'ACTIVE'
                      ? 'border-[#e2d9d8] hover:border-[#d8b3b7]'
                      : 'border-[#e1dcdb] opacity-80'
                  "
                >
                  <div
                    class="absolute inset-y-0 left-0 w-1"
                    :class="enrollment.status === 'ACTIVE' ? 'bg-[#16875b]' : 'bg-[#b9aeac]'"
                  />

                  <div class="pl-2">
                    <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
                      <div class="min-w-0 flex-1">
                        <div class="flex flex-wrap items-center gap-2.5">
                          <RouterLink
                            :to="`/admin/courses/${enrollment.courseId}`"
                            class="min-w-0"
                          >
                            <h3
                              class="truncate text-base font-black tracking-[-0.015em] text-[#302929] transition hover:text-[#a0001c] sm:text-[17px]"
                            >
                              {{ enrollment.courseTitle }}
                            </h3>
                          </RouterLink>

                          <span
                            class="rounded-lg border px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide"
                            :class="
                              enrollment.status === 'ACTIVE'
                                ? 'border-[#b9dfcc] bg-[#eef9f3] text-[#16764f]'
                                : 'border-[#ddd5d4] bg-[#f2efee] text-[#766967]'
                            "
                          >
                            {{ enrollmentStatusLabel(enrollment.status) }}
                          </span>
                        </div>


                      </div>

                      <div class="flex shrink-0 items-center gap-2">
                        <button
                          v-if="enrollment.status === 'INACTIVE' && student.status === 'ACTIVE'"
                          type="button"
                          :disabled="enrolling"
                          class="h-9 rounded-lg border border-[#d7a8ad] bg-white px-3 text-xs font-bold text-[#a0001c] transition hover:bg-[#fff3f4] disabled:opacity-50"
                          @click="reactivateEnrollment(enrollment)"
                        >
                          Gán lại
                        </button>

                        <RouterLink
                          :to="`/admin/courses/${enrollment.courseId}`"
                          class="inline-flex h-9 items-center rounded-lg border border-[#ddd3d2] bg-[#faf8f7] px-3 text-xs font-bold text-[#665957] transition hover:border-[#d5a9ad] hover:text-[#a0001c]"
                        >
                          Xem chi tiết
                        </RouterLink>
                      </div>
                    </div>

                    <!-- Real progress for active enrollment -->
                    <div
                      v-if="enrollment.status === 'ACTIVE'"
                      class="mt-4 border-t border-[#f0eae9] pt-4"
                    >
                      <template v-if="progressFor(enrollment.courseId)">
                        <div class="flex items-center gap-3">
                          <div
                            class="h-2 flex-1 overflow-hidden rounded-full bg-[#eee8e7]"
                            role="progressbar"
                            :aria-valuenow="progressFor(enrollment.courseId)?.percentage ?? 0"
                            aria-valuemin="0"
                            aria-valuemax="100"
                          >
                            <div
                              class="h-full rounded-full"
                              :class="
                                (progressFor(enrollment.courseId)?.percentage ?? 0) >= 100
                                  ? 'bg-[#16875b]'
                                  : 'bg-[#c9152b]'
                              "
                              :style="{
                                width: progressWidth(
                                  progressFor(enrollment.courseId)?.percentage ?? 0,
                                ),
                              }"
                            />
                          </div>

                          <strong
                            class="w-11 shrink-0 text-right text-xs tabular-nums text-[#4f4442]"
                          >
                            {{ progressFor(enrollment.courseId)?.percentage ?? 0 }}%
                          </strong>
                        </div>

                        <p class="mt-2 text-[11px] font-semibold text-[#8d7c7a]">
                          {{ progressFor(enrollment.courseId)?.completedLessons ?? 0 }}
                          /
                          {{ progressFor(enrollment.courseId)?.totalLessons ?? 0 }}
                          bài học Published đã hoàn thành
                        </p>
                      </template>

                      <p v-else class="text-xs leading-5 text-[#8d7c7a]">
                        Chưa có dữ liệu tiến độ cho enrollment này trong báo cáo hiện tại.
                      </p>
                    </div>

                    <div
                      v-else
                      class="mt-4 rounded-lg border border-[#e3dcdb] bg-[#f7f4f3] px-3 py-2.5 text-[11px] leading-5 text-[#81716f]"
                    >
                      Enrollment đang Inactive nên không xuất hiện trong báo cáo tiến độ Admin.
                    </div>
                  </div>
                </article>
              </div>
            </div>
          </section>
        </div>

        <Teleport to="body">
          <Transition
            enter-active-class="transition duration-200 ease-out"
            enter-from-class="opacity-0"
            enter-to-class="opacity-100"
            leave-active-class="transition duration-150 ease-in"
            leave-from-class="opacity-100"
            leave-to-class="opacity-0"
          >
            <div
              v-if="assignPanelOpen"
              class="fixed inset-0 z-[80] bg-black/30 backdrop-blur-[1px]"
              aria-hidden="true"
              @click="closeAssignPanel"
            />
          </Transition>

          <Transition
            enter-active-class="transition duration-250 ease-out"
            enter-from-class="translate-x-full"
            enter-to-class="translate-x-0"
            leave-active-class="transition duration-200 ease-in"
            leave-from-class="translate-x-0"
            leave-to-class="translate-x-full"
          >
            <aside
              v-if="assignPanelOpen"
              class="fixed inset-y-0 right-0 z-[90] flex w-full max-w-[520px] flex-col border-l border-[#e5dedd] bg-white shadow-[-20px_0_50px_rgba(45,24,28,0.12)]"
              role="dialog"
              aria-modal="true"
              aria-labelledby="assign-course-title"
            >
              <header class="flex shrink-0 items-start justify-between gap-4 border-b border-[#eee7e6] px-5 py-5 sm:px-6">
                <div class="min-w-0">
                  <div class="mb-2 inline-flex items-center gap-2 rounded-full bg-[#fff1f2] px-2.5 py-1 text-[10px] font-black uppercase tracking-[0.12em] text-[#a0001c]">
                    {{ activeEnrollments.length }} khóa đang gán
                  </div>
                  <h2 id="assign-course-title" class="text-xl font-black tracking-[-0.025em] text-[#302929]">
                    Gán khóa học
                  </h2>
                  <p class="mt-1 text-xs leading-5 text-[#81716f]">
                    Tìm khóa học và gán cho {{ student.fullName }} mà không rời khỏi trang hiện tại.
                  </p>
                </div>

                <button
                  type="button"
                  aria-label="Đóng panel gán khóa học"
                  :disabled="enrolling"
                  class="grid size-10 shrink-0 place-items-center rounded-xl border border-[#e4dcdb] text-[#776967] transition hover:bg-[#f7f3f2] hover:text-[#a0001c] disabled:opacity-50"
                  @click="closeAssignPanel"
                >
                  <svg class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                    <path d="M6 6l12 12M18 6 6 18" />
                  </svg>
                </button>
              </header>

              <div class="flex min-h-0 flex-1 flex-col px-5 py-5 sm:px-6">
                <div class="relative shrink-0">
                  <span class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3.5 text-[#8d7c7a]">
                    <svg class="size-4.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
                      <circle cx="11" cy="11" r="7" />
                      <path d="m20 20-4-4" />
                    </svg>
                  </span>

                  <input
                    v-model="courseSearch"
                    type="search"
                    maxlength="100"
                    placeholder="Tìm theo tên khóa học…"
                    class="h-11 w-full rounded-xl border border-[#ddd4d3] bg-white pl-11 pr-10 text-sm text-[#302929] outline-none placeholder:text-[#aa9b99] focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                    @input="scheduleCourseSearch"
                  />

                  <button
                    v-if="courseSearch"
                    type="button"
                    aria-label="Xóa tìm kiếm khóa học"
                    class="absolute inset-y-0 right-0 grid w-10 place-items-center text-[#9a8886] hover:text-[#7f1020]"
                    @click="clearCourseSearch"
                  >
                    <svg class="size-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                      <path d="M6 6l12 12M18 6 6 18" />
                    </svg>
                  </button>
                </div>

                <div
                  v-if="courseProblem"
                  class="mt-3 shrink-0 rounded-xl border border-[#efc7cb] bg-[#fff3f4] px-3.5 py-3"
                >
                  <p class="text-xs font-bold text-[#a22435]">{{ courseProblem.title }}</p>
                  <p v-if="courseProblem.detail" class="mt-1 text-[11px] leading-4 text-[#87545a]">
                    {{ courseProblem.detail }}
                  </p>
                </div>

                <div class="mt-4 min-h-0 flex-1 overflow-y-auto rounded-xl border border-[#e3dad9] bg-[#faf8f7]">
                  <div v-if="courseLoading" class="space-y-2 p-3">
                    <div v-for="index in 6" :key="index" class="h-[72px] animate-pulse rounded-xl bg-[#eee8e7]" />
                  </div>

                  <div
                    v-else-if="availableCourses.length === 0"
                    class="flex min-h-[260px] flex-col items-center justify-center px-6 text-center"
                  >
                    <div class="grid size-11 place-items-center rounded-xl bg-white text-[#a48f8d] shadow-sm">
                      <svg class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true">
                        <path d="M4 5.5A2.5 2.5 0 0 1 6.5 3H20v16H6.5A2.5 2.5 0 0 0 4 21.5v-16Z" />
                        <path d="M4 18.5A2.5 2.5 0 0 1 6.5 16H20" />
                      </svg>
                    </div>
                    <p class="mt-3 text-sm font-bold text-[#5b4e4c]">
                      {{ courseSearch ? 'Không tìm thấy khóa học' : 'Không có khóa học có thể gán' }}
                    </p>
                    <p class="mt-1 max-w-xs text-xs leading-5 text-[#8d7c7a]">
                      {{ courseSearch ? 'Thử từ khóa ngắn hơn hoặc xóa tìm kiếm.' : 'Các khóa học khả dụng đã được gán hoặc đã lưu trữ.' }}
                    </p>
                  </div>

                  <label
                    v-for="courseItem in availableCourses"
                    v-else
                    :key="courseItem.id"
                    class="group flex cursor-pointer items-start gap-3 border-b border-[#ece5e4] px-4 py-3.5 last:border-0 hover:bg-white"
                    :class="selectedCourseId === courseItem.id ? 'bg-white ring-1 ring-inset ring-[#e7b7bc]' : ''"
                  >
                    <input
                      v-model="selectedCourseId"
                      type="radio"
                      name="student-course-panel"
                      :value="courseItem.id"
                      class="mt-1 size-4 border-[#cdbfbd] text-[#c9152b] focus:ring-[#c9152b]"
                    />

                    <span class="min-w-0 flex-1">
                      <span class="block text-sm font-bold leading-5 text-[#403735]">{{ courseItem.title }}</span>
                      <span class="mt-1.5 flex flex-wrap items-center gap-2 text-[10px] font-semibold text-[#958582]">
                        <span>{{ courseStatusLabel(courseItem.status) }}</span>
                        <span
                          v-if="student.enrollments.some((item) => item.courseId === courseItem.id && item.status === 'INACTIVE')"
                          class="rounded-md bg-[#fff3e7] px-1.5 py-0.5 font-bold text-[#a66315]"
                        >
                          Đã từng gán
                        </span>
                      </span>
                    </span>

                    <span
                      class="mt-0.5 grid size-6 shrink-0 place-items-center rounded-full border transition"
                      :class="selectedCourseId === courseItem.id ? 'border-[#c9152b] bg-[#c9152b] text-white' : 'border-[#ddd3d2] bg-white text-transparent group-hover:border-[#d2a5aa]'"
                    >
                      <svg class="size-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" aria-hidden="true">
                        <path d="m6 12 4 4 8-8" />
                      </svg>
                    </span>
                  </label>
                </div>
              </div>

              <footer class="shrink-0 border-t border-[#eee7e6] bg-white px-5 py-4 sm:px-6">
                <div v-if="selectedCourse" class="mb-3 rounded-xl bg-[#f8f5f4] px-3.5 py-3">
                  <p class="text-[10px] font-black uppercase tracking-[0.12em] text-[#9a8987]">Đã chọn</p>
                  <p class="mt-1 line-clamp-2 text-xs font-bold leading-5 text-[#403735]">{{ selectedCourse.title }}</p>
                </div>

                <div class="flex gap-2.5">
                  <button
                    type="button"
                    :disabled="enrolling"
                    class="h-11 flex-1 rounded-xl border border-[#ddd3d2] bg-white px-4 text-sm font-bold text-[#665957] transition hover:bg-[#f7f3f2] disabled:opacity-50"
                    @click="closeAssignPanel"
                  >
                    Hủy
                  </button>

                  <button
                    type="button"
                    :disabled="enrolling || !selectedCourse"
                    class="inline-flex h-11 flex-[1.4] items-center justify-center gap-2 rounded-xl bg-[#c9152b] px-4 text-sm font-bold text-white shadow-[0_6px_16px_rgba(160,0,28,0.12)] transition hover:bg-[#a0001c] disabled:cursor-not-allowed disabled:bg-[#d8cfce] disabled:text-[#817775] disabled:shadow-none"
                    @click="enrollSelectedCourse"
                  >
                    <svg v-if="enrolling" class="size-4 animate-spin" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                      <circle class="opacity-30" cx="12" cy="12" r="9" stroke="currentColor" stroke-width="3" />
                      <path d="M21 12a9 9 0 0 0-9-9" stroke="currentColor" stroke-width="3" stroke-linecap="round" />
                    </svg>
                    {{ enrolling ? 'Đang xử lý…' : selectedExistingEnrollment ? 'Gán lại khóa học' : 'Gán khóa học' }}
                  </button>
                </div>
              </footer>
            </aside>
          </Transition>
        </Teleport>
      </template>
    </template>
  </section>
</template>
