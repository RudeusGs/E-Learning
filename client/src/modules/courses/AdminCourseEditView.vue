<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'
import type { CourseStatus } from '@/types/CourseStatus'

import * as courseApi from './course.api'
import type { AdminCourseDetail } from './models/AdminCourseDetail'

type EditableCourseStatus = Exclude<CourseStatus, 'ARCHIVED'>

interface CourseFormState {
  title: string
  description: string
  thumbnailUrl: string
  status: EditableCourseStatus
  sortOrder: number
}

const route = useRoute()
const router = useRouter()

const courseId = computed(() => Number(route.params.courseId))

const course = ref<AdminCourseDetail | null>(null)
const loading = ref(true)
const saving = ref(false)
const archiving = ref(false)
const problem = ref<ProblemDetails | null>(null)
const thumbnailFailed = ref(false)
const allowLeave = ref(false)

const form = reactive<CourseFormState>({
  title: '',
  description: '',
  thumbnailUrl: '',
  status: 'DRAFT',
  sortOrder: 0,
})

const original = ref<CourseFormState | null>(null)

const titleValid = computed(() => {
  const value = form.title.trim()
  return value.length > 0 && value.length <= 200
})

const thumbnailValid = computed(() => {
  const value = form.thumbnailUrl.trim()
  return value.length === 0 || value.startsWith('https://')
})

const sortOrderValid = computed(
  () => Number.isInteger(form.sortOrder) && form.sortOrder >= 0,
)

const formValid = computed(
  () => titleValid.value && thumbnailValid.value && sortOrderValid.value,
)

const hasChanges = computed(() => {
  if (!original.value) return false

  return (
    form.title !== original.value.title ||
    form.description !== original.value.description ||
    form.thumbnailUrl !== original.value.thumbnailUrl ||
    form.status !== original.value.status ||
    form.sortOrder !== original.value.sortOrder
  )
})

const previewAvailable = computed(
  () => Boolean(form.thumbnailUrl.trim()) && thumbnailValid.value && !thumbnailFailed.value,
)

const statusLabel = computed(() =>
  form.status === 'PUBLISHED' ? 'Đã xuất bản' : 'Bản nháp',
)

const currentStatusClass = computed(() =>
  form.status === 'PUBLISHED'
    ? 'border-[#b9dfcc] bg-[#eef9f3] text-[#16764f]'
    : 'border-[#efcfaa] bg-[#fff7ed] text-[#b76500]',
)

function toEditableStatus(status: CourseStatus): EditableCourseStatus {
  return status === 'PUBLISHED' ? 'PUBLISHED' : 'DRAFT'
}

function fillForm(data: AdminCourseDetail): void {
  form.title = data.title
  form.description = data.description ?? ''
  form.thumbnailUrl = data.thumbnailUrl ?? ''
  form.status = toEditableStatus(data.status)
  form.sortOrder = data.sortOrder

  original.value = {
    title: form.title,
    description: form.description,
    thumbnailUrl: form.thumbnailUrl,
    status: form.status,
    sortOrder: form.sortOrder,
  }
}

async function load(): Promise<void> {
  if (!Number.isFinite(courseId.value) || courseId.value <= 0) {
    problem.value = {
      status: 400,
      title: 'Khóa học không hợp lệ',
      detail: 'Mã khóa học không hợp lệ.',
      code: 'INVALID_COURSE_ID',
    }
    loading.value = false
    return
  }

  loading.value = true
  problem.value = null
  thumbnailFailed.value = false

  try {
    const data = await courseApi.getAdminCourse(courseId.value)

    if (data.status === 'ARCHIVED') {
      allowLeave.value = true
      await router.replace(`/admin/courses/${data.id}`)
      return
    }

    course.value = data
    fillForm(data)
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}

function cancel(): void {
  if (saving.value || archiving.value) return
  void router.push(`/admin/courses/${courseId.value}`)
}

async function save(): Promise<void> {
  if (!course.value || saving.value || !formValid.value) return

  saving.value = true
  problem.value = null

  try {
    const updated = await courseApi.updateCourse(course.value.id, {
      title: form.title.trim(),
      description: form.description.trim() || null,
      thumbnailUrl: form.thumbnailUrl.trim() || null,
      status: form.status,
      sortOrder: form.sortOrder,
      version: course.value.version,
    })

    course.value = updated
    fillForm(updated)
    allowLeave.value = true
    await router.replace(`/admin/courses/${updated.id}`)
  } catch (error) {
    problem.value = toProblem(error)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    saving.value = false
  }
}

async function reloadLatest(): Promise<void> {
  original.value = null
  await load()
}

async function archive(): Promise<void> {
  if (!course.value || archiving.value) return

  const confirmed = window.confirm(
    `Lưu trữ khóa học "${course.value.title}"?\n\nKhóa học sẽ bị ẩn khỏi học viên và không thể nhận enrollment mới. Lịch sử học tập cũ vẫn được giữ lại.`,
  )

  if (!confirmed) return

  archiving.value = true
  problem.value = null

  try {
    await courseApi.archiveCourse(course.value.id)
    allowLeave.value = true
    await router.replace(`/admin/courses/${course.value.id}`)
  } catch (error) {
    problem.value = toProblem(error)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    archiving.value = false
  }
}

watch(
  () => form.thumbnailUrl,
  () => {
    thumbnailFailed.value = false
  },
)

watch(
  () => route.params.courseId,
  () => {
    void load()
  },
)

onBeforeRouteLeave(() => {
  if (allowLeave.value || !hasChanges.value || saving.value || archiving.value) {
    return true
  }

  return window.confirm('Bạn có thay đổi chưa lưu. Bạn có chắc muốn rời khỏi trang?')
})

onMounted(load)
</script>

<template>
  <section>
    <!-- Loading -->
    <div v-if="loading" class="mx-auto max-w-[1080px] space-y-6">
      <div class="h-4 w-56 animate-pulse rounded bg-[#eae3e2]" />
      <div class="h-10 w-2/3 animate-pulse rounded-lg bg-[#eae3e2]" />

      <div class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <div class="space-y-6">
          <div class="h-72 animate-pulse rounded-2xl bg-[#eee8e7]" />
          <div class="h-80 animate-pulse rounded-2xl bg-[#eee8e7]" />
        </div>
        <div class="h-80 animate-pulse rounded-2xl bg-[#eee8e7]" />
      </div>
    </div>

    <template v-else>
      <div class="mx-auto max-w-[1080px]">
        <ProblemAlert class="mb-5" :problem="problem" />

        <div
          v-if="problem?.status === 409"
          class="mb-5 flex flex-col justify-between gap-3 rounded-xl border border-[#efd1a7] bg-[#fff8ed] px-4 py-3 sm:flex-row sm:items-center"
        >
          <p class="text-sm leading-6 text-[#7b5521]">
            Dữ liệu khóa học có thể đã được người khác cập nhật. Hãy tải phiên bản mới nhất trước khi sửa tiếp.
          </p>
          <button
            type="button"
            class="h-9 shrink-0 rounded-lg border border-[#d8b57f] bg-white px-3 text-xs font-bold text-[#8c5b16] hover:bg-[#fffaf3]"
            @click="reloadLatest"
          >
            Tải lại dữ liệu
          </button>
        </div>

        <template v-if="course">
          <!-- Breadcrumb -->
          <nav
            aria-label="Breadcrumb"
            class="mb-4 flex flex-wrap items-center gap-2 text-xs font-semibold text-[#8d7d7b]"
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
            <span class="text-[#403735]">Chỉnh sửa</span>
          </nav>

          <!-- Page heading -->
          <header class="mb-7 flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
            <div class="min-w-0">
              <p class="text-xs font-bold uppercase tracking-[0.16em] text-[#9b8583]">
                Quản lý khóa học
              </p>
              <h1
                class="mt-1.5 truncate text-[28px] font-black tracking-[-0.03em] text-[#1c1b1b] sm:text-[34px]"
              >
                Chỉnh sửa: {{ course.title }}
              </h1>
              <p class="mt-2 text-sm leading-6 text-[#6b5d5c]">
                Cập nhật thông tin, khả năng hiển thị và thứ tự của khóa học.
              </p>
            </div>

            <span
              class="inline-flex h-8 shrink-0 items-center gap-2 self-start rounded-lg border px-3 text-xs font-bold sm:self-auto"
              :class="currentStatusClass"
            >
              <span class="size-1.5 rounded-full bg-current" />
              {{ statusLabel }}
            </span>
          </header>

          <form @submit.prevent="save">
            <div class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_320px] lg:items-start">
              <!-- Left -->
              <div class="space-y-6">
                <section
                  class="rounded-2xl border border-[#e7dedd] bg-white p-5 shadow-[0_2px_12px_rgba(50,25,25,0.025)] sm:p-7"
                >
                  <div class="mb-6 border-b border-[#eee7e6] pb-4">
                    <h2 class="text-lg font-black tracking-[-0.02em] text-[#302929]">
                      Thông tin cơ bản
                    </h2>
                  </div>

                  <div class="space-y-5">
                    <label class="block">
                      <span
                        class="mb-2 flex items-center justify-between gap-3 text-sm font-bold text-[#403735]"
                      >
                        <span>Tên khóa học <span class="text-[#c9152b]">*</span></span>
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
                        class="h-11 w-full rounded-xl border border-[#ddd4d3] bg-white px-3.5 text-sm text-[#2d2727] outline-none focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                      />
                    </label>

                    <label class="block">
                      <span
                        class="mb-2 flex items-center justify-between gap-3 text-sm font-bold text-[#403735]"
                      >
                        <span>Mô tả</span>
                        <span class="text-[11px] font-semibold text-[#a0918f]">
                          {{ form.description.length }}/4000
                        </span>
                      </span>

                      <textarea
                        v-model="form.description"
                        maxlength="4000"
                        rows="5"
                        class="w-full resize-y rounded-xl border border-[#ddd4d3] bg-white px-3.5 py-3 text-sm leading-6 text-[#2d2727] outline-none focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                      />
                    </label>
                  </div>
                </section>

                <section
                  class="rounded-2xl border border-[#e7dedd] bg-white p-5 shadow-[0_2px_12px_rgba(50,25,25,0.025)] sm:p-7"
                >
                  <div class="mb-6 border-b border-[#eee7e6] pb-4">
                    <h2 class="text-lg font-black tracking-[-0.02em] text-[#302929]">
                      Ảnh đại diện
                    </h2>
                    <p class="mt-1 text-sm text-[#81716f]">
                      Sử dụng ảnh ngang, rõ nét để khóa học dễ nhận diện hơn.
                    </p>
                  </div>

                  <label class="block">
                    <span class="mb-2 block text-sm font-bold text-[#403735]">
                      Thumbnail URL
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
                          <path d="M10 13a5 5 0 0 0 7.1 0l2-2a5 5 0 0 0-7.1-7.1l-1.1 1.1" />
                          <path d="M14 11a5 5 0 0 0-7.1 0l-2 2A5 5 0 0 0 12 20.1l1.1-1.1" />
                        </svg>
                      </span>

                      <input
                        v-model="form.thumbnailUrl"
                        type="url"
                        pattern="https://.*"
                        placeholder="https://example.com/course-cover.jpg"
                        class="h-11 w-full rounded-xl border bg-white pl-11 pr-3.5 text-sm text-[#2d2727] outline-none focus:ring-4"
                        :class="
                          thumbnailValid
                            ? 'border-[#ddd4d3] focus:border-[#c9152b] focus:ring-[#c9152b]/10'
                            : 'border-[#d95b68] focus:border-[#ba1a1a] focus:ring-[#ba1a1a]/10'
                        "
                      />
                    </div>

                    <span
                      class="mt-2 block text-xs leading-5"
                      :class="thumbnailValid ? 'text-[#8d7c7a]' : 'text-[#ba1a1a]'"
                    >
                      {{
                        thumbnailValid
                          ? 'Backend chỉ chấp nhận URL HTTPS. Có thể để trống nếu chưa có ảnh.'
                          : 'URL ảnh phải bắt đầu bằng https://'
                      }}
                    </span>
                  </label>

                  <div class="mt-5">
                    <p class="mb-2 text-sm font-bold text-[#403735]">Xem trước</p>

                    <div
                      class="course-preview-grid relative aspect-[16/7] overflow-hidden rounded-xl border border-[#e2d8d7] bg-[#f4efee]"
                    >
                      <img
                        v-if="previewAvailable"
                        :src="form.thumbnailUrl.trim()"
                        alt="Xem trước ảnh đại diện khóa học"
                        class="absolute inset-0 size-full object-cover"
                        @error="thumbnailFailed = true"
                      />

                      <div
                        v-else
                        class="absolute inset-0 flex flex-col items-center justify-center text-[#9c8987]"
                      >
                        <svg
                          class="size-8"
                          viewBox="0 0 24 24"
                          fill="none"
                          stroke="currentColor"
                          stroke-width="1.6"
                          aria-hidden="true"
                        >
                          <rect x="3" y="4" width="18" height="16" rx="3" />
                          <circle cx="9" cy="10" r="2" />
                          <path d="m5 18 5-5 3 3 2-2 4 4" />
                        </svg>
                        <span class="mt-2 text-xs font-semibold">
                          {{
                            thumbnailFailed
                              ? 'Không thể tải ảnh từ URL này'
                              : 'Nhập URL HTTPS để xem trước'
                          }}
                        </span>
                      </div>
                    </div>
                  </div>
                </section>

                <!-- Danger zone -->
                <section
                  class="rounded-2xl border border-[#efc7cb] bg-white p-5 shadow-[0_2px_12px_rgba(50,25,25,0.02)] sm:p-7"
                >
                  <div class="flex items-start gap-3">
                    <span
                      class="grid size-10 shrink-0 place-items-center rounded-xl border border-[#efc7cb] bg-[#fff1f2] text-[#ba1a1a]"
                    >
                      <svg
                        class="size-5"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="1.8"
                        aria-hidden="true"
                      >
                        <path d="M12 9v4M12 17h.01" />
                        <path d="M10.3 3.6 2.4 18a2 2 0 0 0 1.8 3h15.6a2 2 0 0 0 1.8-3L13.7 3.6a2 2 0 0 0-3.4 0Z" />
                      </svg>
                    </span>

                    <div class="min-w-0 flex-1">
                      <h2 class="text-lg font-black tracking-[-0.02em] text-[#9d1d2a]">
                        Vùng nguy hiểm
                      </h2>
                      <p class="mt-2 max-w-2xl text-sm leading-6 text-[#786967]">
                        Lưu trữ khóa học sẽ ẩn khóa học khỏi học viên và không cho phân công mới.
                        Dữ liệu lịch sử và tiến độ trước đó vẫn được giữ lại.
                      </p>

                      <button
                        type="button"
                        :disabled="archiving || saving"
                        class="mt-5 inline-flex h-10 items-center gap-2 rounded-lg border border-[#dc8e97] bg-white px-4 text-sm font-bold text-[#a22435] transition hover:bg-[#fff1f2] disabled:cursor-not-allowed disabled:opacity-50"
                        @click="archive"
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
                        {{ archiving ? 'Đang lưu trữ…' : 'Lưu trữ khóa học' }}
                      </button>
                    </div>
                  </div>
                </section>
              </div>

              <!-- Right -->
              <aside class="lg:sticky lg:top-24">
                <section
                  class="rounded-2xl border border-[#e7dedd] bg-white p-5 shadow-[0_6px_24px_rgba(53,27,31,0.04)] sm:p-6"
                >
                  <div class="border-b border-[#eee7e6] pb-4">
                    <h2 class="text-lg font-black tracking-[-0.02em] text-[#302929]">
                      Trạng thái & hiển thị
                    </h2>
                  </div>

                  <div class="mt-5 space-y-5">
                    <label class="block">
                      <span class="mb-2 block text-sm font-bold text-[#403735]">
                        Trạng thái
                      </span>

                      <div class="relative">
                        <select
                          v-model="form.status"
                          class="h-11 w-full appearance-none rounded-xl border border-[#ddd4d3] bg-white pl-3.5 pr-10 text-sm font-semibold text-[#342d2d] outline-none focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                        >
                          <option value="PUBLISHED">Đã xuất bản</option>
                          <option value="DRAFT">Bản nháp</option>
                        </select>

                        <svg
                          class="pointer-events-none absolute right-3 top-1/2 size-4 -translate-y-1/2 text-[#8d7c7a]"
                          viewBox="0 0 24 24"
                          fill="none"
                          stroke="currentColor"
                          stroke-width="2"
                          aria-hidden="true"
                        >
                          <path d="m7 10 5 5 5-5" />
                        </svg>
                      </div>
                    </label>

                    <label class="block">
                      <span class="mb-2 block text-sm font-bold text-[#403735]">
                        Thứ tự hiển thị
                      </span>

                      <input
                        v-model.number="form.sortOrder"
                        required
                        min="0"
                        step="1"
                        type="number"
                        class="h-11 w-full rounded-xl border bg-white px-3.5 text-sm font-bold text-[#342d2d] outline-none focus:ring-4"
                        :class="
                          sortOrderValid
                            ? 'border-[#ddd4d3] focus:border-[#c9152b] focus:ring-[#c9152b]/10'
                            : 'border-[#d95b68] focus:border-[#ba1a1a] focus:ring-[#ba1a1a]/10'
                        "
                      />

                      <span class="mt-2 block text-xs leading-5 text-[#8d7c7a]">
                        Số nhỏ hơn được ưu tiên hiển thị trước.
                      </span>
                    </label>

                    <div
                      class="rounded-xl border px-4 py-4"
                      :class="
                        form.status === 'PUBLISHED'
                          ? 'border-[#cee5d9] bg-[#f3faf6]'
                          : 'border-[#eadfdf] bg-[#f8f5f4]'
                      "
                    >
                      <div class="flex items-start gap-3">
                        <svg
                          class="mt-0.5 size-5 shrink-0"
                          :class="form.status === 'PUBLISHED' ? 'text-[#16875b]' : 'text-[#887977]'"
                          viewBox="0 0 24 24"
                          fill="none"
                          stroke="currentColor"
                          stroke-width="1.8"
                          aria-hidden="true"
                        >
                          <path d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6Z" />
                          <circle cx="12" cy="12" r="2.8" />
                        </svg>

                        <div>
                          <h3 class="text-sm font-black text-[#403735]">
                            Hiển thị với học viên
                          </h3>
                          <p class="mt-1.5 text-xs leading-5 text-[#756765]">
                            <template v-if="form.status === 'PUBLISHED'">
                              Khóa học có thể xuất hiện với những học viên đang có enrollment Active.
                              Học viên không tự đăng ký khóa học.
                            </template>
                            <template v-else>
                              Khóa học chỉ hiển thị trong khu vực quản trị và không thể học từ phía học viên.
                            </template>
                          </p>
                        </div>
                      </div>
                    </div>

                    <div class="rounded-xl border border-[#eadfdf] bg-[#faf7f6] px-4 py-3">
                      <p class="text-[11px] font-bold uppercase tracking-[0.14em] text-[#927f7d]">
                        Phiên bản dữ liệu
                      </p>
                      <p class="mt-1 text-sm font-black text-[#4f4442]">
                        Version {{ course.version }}
                      </p>
                      <p class="mt-1 text-xs leading-5 text-[#897977]">
                        Hệ thống dùng version để tránh ghi đè thay đổi của người quản trị khác.
                      </p>
                    </div>
                  </div>
                </section>
              </aside>
            </div>

            <!-- Sticky bottom actions -->
            <div
              class="sticky bottom-0 z-20 -mx-4 -mb-4 mt-7 border-t border-[#e5dcdb] bg-[#f8f7f5]/95 px-4 py-4 backdrop-blur sm:-mx-5 sm:-mb-5 sm:px-5 lg:-mx-6 lg:px-6"
            >
              <div class="mx-auto flex max-w-[1080px] flex-col-reverse gap-3 sm:flex-row sm:items-center sm:justify-between">
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

                    {{ saving ? 'Đang lưu…' : 'Lưu thay đổi' }}
                  </button>
                </div>
              </div>
            </div>
          </form>
        </template>
      </div>
    </template>
  </section>
</template>

<style scoped>
.course-preview-grid {
  background-image:
    linear-gradient(to right, rgb(232 227 227 / 0.72) 1px, transparent 1px),
    linear-gradient(to bottom, rgb(232 227 227 / 0.72) 1px, transparent 1px);
  background-size: 22px 22px;
}
</style>
