<script setup lang="ts">
import { computed, nextTick, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'
import type { CourseStatus } from '@/types/CourseStatus'

import * as courseApi from './course.api'

type CreateCourseStatus = Exclude<CourseStatus, 'ARCHIVED'>

type TouchedField = 'title' | 'thumbnailUrl' | 'sortOrder'

const router = useRouter()

const saving = ref(false)
const problem = ref<ProblemDetails | null>(null)
const thumbnailPreviewFailed = ref(false)

const titleInputRef = ref<HTMLInputElement | null>(null)
const thumbnailInputRef = ref<HTMLInputElement | null>(null)
const sortOrderInputRef = ref<HTMLInputElement | null>(null)

const form = reactive({
  title: '',
  description: '',
  thumbnailUrl: '',
  status: 'DRAFT' as CreateCourseStatus,
  sortOrder: 0,
})

const touched = reactive<Record<TouchedField, boolean>>({
  title: false,
  thumbnailUrl: false,
  sortOrder: false,
})

const previewTitle = computed(() => form.title.trim() || 'Tên khóa học sẽ hiển thị ở đây')
const previewDescription = computed(
  () =>
    form.description.trim() ||
    'Mô tả ngắn gọn về khóa học sẽ xuất hiện tại đây để học viên hiểu nội dung chính.',
)

const hasThumbnail = computed(
  () => form.thumbnailUrl.trim().startsWith('https://') && !thumbnailPreviewFailed.value,
)

const titleReady = computed(() => {
  const title = form.title.trim()
  return title.length > 0 && title.length <= 200
})

const thumbnailReady = computed(() => {
  const thumbnailUrl = form.thumbnailUrl.trim()
  return !thumbnailUrl || thumbnailUrl.startsWith('https://')
})

const sortOrderReady = computed(
  () => Number.isInteger(form.sortOrder) && Number(form.sortOrder) >= 0,
)

const formReady = computed(() => titleReady.value && thumbnailReady.value && sortOrderReady.value)

const titleError = computed(() => {
  if (!touched.title) return ''

  const title = form.title.trim()
  if (!title) return 'Nhập tên khóa học để tiếp tục.'
  if (title.length > 200) return 'Tên khóa học không được vượt quá 200 ký tự.'

  return ''
})

const thumbnailError = computed(() => {
  if (!touched.thumbnailUrl) return ''
  if (!form.thumbnailUrl.trim() || thumbnailReady.value) return ''

  return 'URL ảnh đại diện phải bắt đầu bằng https://'
})

const sortOrderError = computed(() => {
  if (!touched.sortOrder) return ''
  if (!Number.isInteger(form.sortOrder)) return 'Thứ tự hiển thị phải là số nguyên.'
  if (Number(form.sortOrder) < 0) return 'Thứ tự hiển thị không được nhỏ hơn 0.'

  return ''
})

const completionItems = computed(() => [
  {
    label: 'Tên khóa học',
    description: titleReady.value ? 'Đã nhập đầy đủ' : 'Bắt buộc, tối đa 200 ký tự',
    ready: titleReady.value,
  },
  {
    label: 'Ảnh đại diện',
    description: thumbnailReady.value ? 'URL hợp lệ hoặc có thể bỏ trống' : 'Chỉ chấp nhận URL HTTPS',
    ready: thumbnailReady.value,
  },
  {
    label: 'Thứ tự hiển thị',
    description: sortOrderReady.value ? 'Giá trị hợp lệ' : 'Cần là số nguyên từ 0 trở lên',
    ready: sortOrderReady.value,
  },
])

const completedCount = computed(() => completionItems.value.filter((item) => item.ready).length)

const submitLabel = computed(() => {
  if (saving.value) return 'Đang tạo…'
  return form.status === 'PUBLISHED' ? 'Tạo & xuất bản' : 'Tạo bản nháp'
})

const submitHint = computed(() => {
  if (!formReady.value) return 'Nhấn nút tạo để xem ngay mục cần bổ sung.'
  return form.status === 'PUBLISHED'
    ? 'Khóa học sẽ hiển thị cho học viên được phân công ngay sau khi tạo.'
    : 'Khóa học sẽ được lưu dưới dạng bản nháp và chưa hiển thị cho học viên.'
})

watch(
  () => form.thumbnailUrl,
  () => {
    thumbnailPreviewFailed.value = false
  },
)

function clearThumbnail(): void {
  form.thumbnailUrl = ''
  thumbnailPreviewFailed.value = false
  touched.thumbnailUrl = false
  void nextTick(() => thumbnailInputRef.value?.focus())
}

function cancel(): void {
  if (saving.value) return
  void router.push('/admin/courses')
}

function markAllTouched(): void {
  touched.title = true
  touched.thumbnailUrl = true
  touched.sortOrder = true
}

async function focusFirstInvalid(): Promise<void> {
  await nextTick()

  if (!titleReady.value) {
    titleInputRef.value?.focus()
    return
  }

  if (!thumbnailReady.value) {
    thumbnailInputRef.value?.focus()
    return
  }

  if (!sortOrderReady.value) {
    sortOrderInputRef.value?.focus()
  }
}

async function submit(): Promise<void> {
  if (saving.value) return

  problem.value = null
  markAllTouched()

  if (!formReady.value) {
    await focusFirstInvalid()
    return
  }

  const title = form.title.trim()
  const thumbnailUrl = form.thumbnailUrl.trim()

  saving.value = true

  try {
    const created = await courseApi.createCourse({
      title,
      description: form.description.trim() || null,
      thumbnailUrl: thumbnailUrl || null,
      status: form.status,
      sortOrder: Number(form.sortOrder),
    })

    await router.replace(`/admin/courses/${created.id}`)
  } catch (error) {
    problem.value = toProblem(error)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <section>
    <header class="mb-7">
      <nav
        aria-label="Breadcrumb"
        class="mb-3 flex items-center gap-2 text-xs font-semibold text-[#8c7c7a]"
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

        <span class="text-[#403735]">Tạo khóa học</span>
      </nav>

      <div class="flex flex-col justify-between gap-4 lg:flex-row lg:items-end">
        <div class="max-w-2xl">
          <div class="mb-2 inline-flex items-center gap-2 rounded-full border border-[#eadfdd] bg-white px-3 py-1.5 text-[11px] font-bold text-[#756765] shadow-sm">
            <span class="size-1.5 rounded-full bg-[#c9152b]" />
            Thiết lập khóa học mới
          </div>

          <h1 class="text-[30px] font-black tracking-[-0.035em] text-[#1c1b1b] sm:text-[36px]">
            Tạo khóa học mới
          </h1>
          <p class="mt-2 max-w-xl text-sm leading-6 text-[#7f716f]">
            Điền nội dung chính, chọn trạng thái hiển thị và kiểm tra bản xem trước trước khi tạo.
          </p>
        </div>

        <div class="hidden items-center gap-2 rounded-xl border border-[#e7dedd] bg-white px-3.5 py-2.5 text-xs font-bold text-[#6f625f] shadow-sm sm:flex">
          <span
            class="grid size-6 place-items-center rounded-full text-[11px]"
            :class="formReady ? 'bg-[#eaf7f0] text-[#16764f]' : 'bg-[#fff1f2] text-[#b22537]'"
          >
            {{ completedCount }}/3
          </span>
          {{ formReady ? 'Sẵn sàng tạo' : 'Đang hoàn thiện' }}
        </div>
      </div>
    </header>

    <ProblemAlert class="mb-6" :problem="problem" />

    <form novalidate @submit.prevent="submit">
      <div class="grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1fr)_370px] xl:items-start">
        <div class="space-y-6">
          <section
            class="rounded-2xl border border-[#e7dedd] bg-white p-5 shadow-[0_2px_12px_rgba(50,25,25,0.025)] sm:p-7"
          >
            <div class="mb-6 flex items-start justify-between gap-4">
              <div class="flex items-start gap-3">
                <span
                  class="grid size-10 shrink-0 place-items-center rounded-xl border border-[#f0d9dc] bg-[#fff3f4] text-[#c9152b]"
                >
                  <span class="text-sm font-black">1</span>
                </span>

                <div>
                  <h2 class="text-lg font-black tracking-[-0.02em] text-[#302929]">
                    Nội dung khóa học
                  </h2>
                  <p class="mt-1 text-sm leading-5 text-[#827270]">
                    Đây là thông tin học viên nhìn thấy đầu tiên.
                  </p>
                </div>
              </div>

              <span class="hidden rounded-md bg-[#f7f3f2] px-2 py-1 text-[10px] font-bold uppercase tracking-wide text-[#8b7a78] sm:inline-flex">
                1 trường bắt buộc
              </span>
            </div>

            <div class="space-y-6">
              <div>
                <label
                  class="mb-2 flex items-center justify-between gap-3 text-sm font-bold text-[#403735]"
                  for="course-title"
                >
                  <span>Tên khóa học <span class="text-[#c9152b]">*</span></span>
                  <span
                    class="text-[11px] font-semibold tabular-nums"
                    :class="form.title.length > 180 ? 'text-[#b22537]' : 'text-[#a0918f]'"
                  >
                    {{ form.title.length }}/200
                  </span>
                </label>

                <input
                  id="course-title"
                  ref="titleInputRef"
                  v-model="form.title"
                  maxlength="200"
                  autocomplete="off"
                  placeholder="Ví dụ: Lập trình cơ bản với Python"
                  :aria-invalid="Boolean(titleError)"
                  aria-describedby="course-title-help course-title-error"
                  class="h-12 w-full rounded-xl border bg-white px-3.5 text-[15px] text-[#2d2727] outline-none transition placeholder:text-[#a99a98] focus:ring-4"
                  :class="
                    titleError
                      ? 'border-[#d54b5b] focus:border-[#c9152b] focus:ring-[#c9152b]/10'
                      : 'border-[#ddd4d3] focus:border-[#c9152b] focus:ring-[#c9152b]/10'
                  "
                  @blur="touched.title = true"
                />

                <div class="mt-2 min-h-5">
                  <p v-if="titleError" id="course-title-error" role="alert" class="text-xs font-semibold text-[#b22537]">
                    {{ titleError }}
                  </p>
                  <p v-else id="course-title-help" class="text-xs leading-5 text-[#8d7c7a]">
                    Nên ngắn gọn, dễ quét và mô tả đúng nội dung chính của khóa học.
                  </p>
                </div>
              </div>

              <div>
                <label
                  class="mb-2 flex items-center justify-between gap-3 text-sm font-bold text-[#403735]"
                  for="course-description"
                >
                  <span>Mô tả <span class="font-medium text-[#9c8d8b]">(không bắt buộc)</span></span>
                  <span class="text-[11px] font-semibold tabular-nums text-[#a0918f]">
                    {{ form.description.length }}/4000
                  </span>
                </label>

                <textarea
                  id="course-description"
                  v-model="form.description"
                  maxlength="4000"
                  rows="5"
                  placeholder="Mô tả mục tiêu, nội dung chính và đối tượng phù hợp với khóa học…"
                  class="w-full resize-y rounded-xl border border-[#ddd4d3] bg-white px-3.5 py-3 text-sm leading-6 text-[#2d2727] outline-none transition placeholder:text-[#a99a98] focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
                />
                <p class="mt-2 text-xs leading-5 text-[#8d7c7a]">
                  Gợi ý: viết 1–3 câu để học viên hiểu nhanh khóa học này dành cho ai và học được gì.
                </p>
              </div>

              <div class="border-t border-[#eee6e4] pt-6">
                <div class="mb-3 flex items-center justify-between gap-3">
                  <label class="text-sm font-bold text-[#403735]" for="thumbnail-url">
                    Ảnh đại diện khóa học
                  </label>
                  <span class="rounded-md bg-[#f7f3f2] px-2 py-1 text-[10px] font-bold text-[#8b7a78]">
                    16:9 khuyến nghị
                  </span>
                </div>

                <div class="grid gap-4 md:grid-cols-[minmax(0,1fr)_220px] md:items-start">
                  <div>
                    <div class="relative">
                      <span class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3.5 text-[#8d7c7a]">
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
                        id="thumbnail-url"
                        ref="thumbnailInputRef"
                        v-model="form.thumbnailUrl"
                        type="url"
                        inputmode="url"
                        placeholder="https://example.com/course-cover.jpg"
                        :aria-invalid="Boolean(thumbnailError)"
                        aria-describedby="thumbnail-help thumbnail-error"
                        class="h-12 w-full rounded-xl border bg-white pl-11 text-sm text-[#2d2727] outline-none transition placeholder:text-[#a99a98] focus:ring-4"
                        :class="[
                          form.thumbnailUrl ? 'pr-11' : 'pr-3.5',
                          thumbnailError
                            ? 'border-[#d54b5b] focus:border-[#c9152b] focus:ring-[#c9152b]/10'
                            : 'border-[#ddd4d3] focus:border-[#c9152b] focus:ring-[#c9152b]/10',
                        ]"
                        @blur="touched.thumbnailUrl = true"
                      />

                      <button
                        v-if="form.thumbnailUrl"
                        type="button"
                        class="absolute inset-y-0 right-0 grid w-11 place-items-center text-[#8f807d] transition hover:text-[#a0001c] focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-[#c9152b]/10"
                        aria-label="Xóa URL ảnh đại diện"
                        @click="clearThumbnail"
                      >
                        <svg
                          class="size-4"
                          viewBox="0 0 24 24"
                          fill="none"
                          stroke="currentColor"
                          stroke-width="2"
                          aria-hidden="true"
                        >
                          <path d="m6 6 12 12M18 6 6 18" />
                        </svg>
                      </button>
                    </div>

                    <div class="mt-2 min-h-5">
                      <p v-if="thumbnailError" id="thumbnail-error" role="alert" class="text-xs font-semibold text-[#b22537]">
                        {{ thumbnailError }}
                      </p>
                      <p v-else id="thumbnail-help" class="text-xs leading-5 text-[#8d7c7a]">
                        Có thể bỏ trống. Nếu nhập, chỉ dùng URL HTTPS để đảm bảo ảnh tải an toàn.
                      </p>
                    </div>
                  </div>

                  <div
                    class="relative aspect-video overflow-hidden rounded-xl border border-[#e3d9d8] bg-[#f6f2f1]"
                  >
                    <img
                      v-if="hasThumbnail"
                      :src="form.thumbnailUrl.trim()"
                      alt="Xem trước ảnh đại diện khóa học"
                      class="absolute inset-0 size-full object-cover"
                      @error="thumbnailPreviewFailed = true"
                    />

                    <div
                      v-else
                      class="course-preview-grid absolute inset-0 flex flex-col items-center justify-center px-4 text-center text-[#9b8987]"
                    >
                      <svg
                        class="size-7"
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
                      <span class="mt-2 text-[11px] font-semibold">
                        {{ thumbnailPreviewFailed ? 'Không tải được ảnh' : 'Chưa có ảnh' }}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </section>

          <section
            class="rounded-2xl border border-[#e7dedd] bg-white p-5 shadow-[0_2px_12px_rgba(50,25,25,0.025)] sm:p-7"
          >
            <div class="mb-6 flex items-start gap-3">
              <span
                class="grid size-10 shrink-0 place-items-center rounded-xl border border-[#d7e7df] bg-[#f0f8f4] text-[#16764f]"
              >
                <span class="text-sm font-black">2</span>
              </span>

              <div>
                <h2 class="text-lg font-black tracking-[-0.02em] text-[#302929]">
                  Hiển thị & sắp xếp
                </h2>
                <p class="mt-1 text-sm leading-5 text-[#827270]">
                  Chọn khóa học có xuất hiện ngay hay chỉ lưu để tiếp tục chỉnh sửa.
                </p>
              </div>
            </div>

            <fieldset>
              <legend class="mb-3 text-sm font-bold text-[#403735]">Trạng thái sau khi tạo</legend>

              <div class="grid gap-3 sm:grid-cols-2">
                <label class="group cursor-pointer">
                  <input v-model="form.status" type="radio" value="DRAFT" class="peer sr-only" />
                  <span
                    class="block min-h-32 rounded-xl border border-[#e2d9d8] bg-white p-4 transition hover:-translate-y-0.5 hover:border-[#d5c8c6] hover:shadow-sm peer-checked:border-[#dc9fa6] peer-checked:bg-[#fff6f6] peer-focus-visible:ring-4 peer-focus-visible:ring-[#c9152b]/10"
                  >
                    <span class="flex items-start justify-between gap-4">
                      <span>
                        <span class="mb-2 inline-flex rounded-md bg-[#f4efee] px-2 py-1 text-[10px] font-black uppercase tracking-wide text-[#7b6c69]">
                          An toàn
                        </span>
                        <strong class="block text-sm font-black text-[#302929]">Lưu bản nháp</strong>
                        <span class="mt-1.5 block text-xs leading-5 text-[#81716f]">
                          Học viên chưa nhìn thấy. Phù hợp khi nội dung vẫn còn cần chỉnh sửa.
                        </span>
                      </span>

                      <span
                        class="mt-0.5 grid size-5 shrink-0 place-items-center rounded-full border"
                        :class="form.status === 'DRAFT' ? 'border-[#c9152b]' : 'border-[#cfbebc]'"
                      >
                        <span v-if="form.status === 'DRAFT'" class="size-2.5 rounded-full bg-[#c9152b]" />
                      </span>
                    </span>
                  </span>
                </label>

                <label class="group cursor-pointer">
                  <input v-model="form.status" type="radio" value="PUBLISHED" class="peer sr-only" />
                  <span
                    class="block min-h-32 rounded-xl border border-[#e2d9d8] bg-white p-4 transition hover:-translate-y-0.5 hover:border-[#c5d9cf] hover:shadow-sm peer-checked:border-[#9fcdb7] peer-checked:bg-[#f1faf5] peer-focus-visible:ring-4 peer-focus-visible:ring-[#16875b]/10"
                  >
                    <span class="flex items-start justify-between gap-4">
                      <span>
                        <span class="mb-2 inline-flex rounded-md bg-[#eaf7f0] px-2 py-1 text-[10px] font-black uppercase tracking-wide text-[#16764f]">
                          Hiển thị ngay
                        </span>
                        <strong class="block text-sm font-black text-[#302929]">Xuất bản</strong>
                        <span class="mt-1.5 block text-xs leading-5 text-[#81716f]">
                          Học viên được phân công có thể thấy khóa học ngay sau khi tạo.
                        </span>
                      </span>

                      <span
                        class="mt-0.5 grid size-5 shrink-0 place-items-center rounded-full border"
                        :class="form.status === 'PUBLISHED' ? 'border-[#16875b]' : 'border-[#cfbebc]'"
                      >
                        <span v-if="form.status === 'PUBLISHED'" class="size-2.5 rounded-full bg-[#16875b]" />
                      </span>
                    </span>
                  </span>
                </label>
              </div>
            </fieldset>

            <div class="mt-6 border-t border-[#eee6e4] pt-6">
              <div class="max-w-md">
                <div class="mb-2 flex items-center justify-between gap-3">
                  <label class="text-sm font-bold text-[#403735]" for="sort-order">
                    Thứ tự hiển thị
                  </label>
                  <span class="text-[11px] font-semibold text-[#9b8d8b]">Số nhỏ hơn lên trước</span>
                </div>

                <div class="relative">
                  <span class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3.5 text-[#8d7c7a]">
                    <svg
                      class="size-5"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="1.8"
                      aria-hidden="true"
                    >
                      <path d="M8 6h11M8 12h8M8 18h5M4 5v14" />
                    </svg>
                  </span>

                  <input
                    id="sort-order"
                    ref="sortOrderInputRef"
                    v-model.number="form.sortOrder"
                    type="number"
                    min="0"
                    step="1"
                    inputmode="numeric"
                    :aria-invalid="Boolean(sortOrderError)"
                    aria-describedby="sort-order-help sort-order-error"
                    class="h-12 w-full rounded-xl border bg-white pl-11 pr-3.5 text-sm font-bold tabular-nums text-[#2d2727] outline-none transition focus:ring-4"
                    :class="
                      sortOrderError
                        ? 'border-[#d54b5b] focus:border-[#c9152b] focus:ring-[#c9152b]/10'
                        : 'border-[#ddd4d3] focus:border-[#c9152b] focus:ring-[#c9152b]/10'
                    "
                    @blur="touched.sortOrder = true"
                  />
                </div>

                <div class="mt-2 min-h-5">
                  <p v-if="sortOrderError" id="sort-order-error" role="alert" class="text-xs font-semibold text-[#b22537]">
                    {{ sortOrderError }}
                  </p>
                  <p v-else id="sort-order-help" class="text-xs leading-5 text-[#8d7c7a]">
                    Có thể để 0 nếu chưa cần ưu tiên vị trí hiển thị.
                  </p>
                </div>
              </div>
            </div>
          </section>
        </div>

        <aside class="space-y-5 xl:sticky xl:top-24 xl:self-start">
          <section class="overflow-hidden rounded-2xl border border-[#e4dbda] bg-white shadow-[0_8px_26px_rgba(65,28,34,0.055)]">
            <div class="flex items-center justify-between border-b border-[#eee6e4] px-5 py-3.5">
              <div>
                <p class="text-sm font-black text-[#302929]">Xem trước</p>
                <p class="mt-0.5 text-[11px] font-medium text-[#948582]">Thẻ khóa học phía học viên</p>
              </div>
              <span class="rounded-md bg-[#f5f1f0] px-2 py-1 text-[10px] font-bold text-[#7e706d]">Live</span>
            </div>

            <div class="relative aspect-[16/8.5] overflow-hidden bg-[#f3eeee]">
              <img
                v-if="hasThumbnail"
                :src="form.thumbnailUrl.trim()"
                alt=""
                class="absolute inset-0 size-full object-cover"
                @error="thumbnailPreviewFailed = true"
              />

              <div v-else class="course-preview-grid absolute inset-0">
                <div class="absolute -right-10 -top-12 size-40 rounded-full border border-[#ead4d6] bg-[#fff4f5]" />
                <div class="absolute bottom-4 left-5 size-10 rounded-xl border border-[#ebd5d7] bg-white/70" />
                <div class="absolute bottom-7 left-20 h-px w-28 -rotate-6 bg-[#dcbfc2]" />
              </div>

              <div class="absolute inset-x-0 bottom-0 h-20 bg-gradient-to-t from-black/25 to-transparent" />
            </div>

            <div class="p-5">
              <div class="mb-3 flex flex-wrap gap-2">
                <span
                  class="rounded-md border px-2 py-1 text-[10px] font-bold uppercase tracking-wide"
                  :class="
                    form.status === 'PUBLISHED'
                      ? 'border-[#b9dfcc] bg-[#eef9f3] text-[#16764f]'
                      : 'border-[#efc7cb] bg-[#fff3f4] text-[#a22435]'
                  "
                >
                  {{ form.status === 'PUBLISHED' ? 'Đã xuất bản' : 'Bản nháp' }}
                </span>
                <span class="rounded-md bg-[#f1edec] px-2 py-1 text-[10px] font-bold text-[#766866]">
                  Thứ tự {{ form.sortOrder }}
                </span>
              </div>

              <h3 class="line-clamp-2 text-[17px] font-black leading-6 tracking-[-0.02em] text-[#302929]">
                {{ previewTitle }}
              </h3>
              <p class="mt-2 line-clamp-3 text-sm leading-6 text-[#81716f]">
                {{ previewDescription }}
              </p>
            </div>
          </section>

          <section class="rounded-2xl border border-[#e4dbda] bg-white p-5 shadow-[0_4px_18px_rgba(65,28,34,0.035)]">
            <div class="mb-4 flex items-center justify-between gap-3">
              <div>
                <h3 class="text-sm font-black text-[#302929]">Kiểm tra trước khi tạo</h3>
                <p class="mt-1 text-xs leading-5 text-[#8b7c79]">Các mục quan trọng được kiểm tra tự động.</p>
              </div>

              <span
                class="grid size-9 shrink-0 place-items-center rounded-full text-xs font-black"
                :class="formReady ? 'bg-[#eaf7f0] text-[#16764f]' : 'bg-[#fff1f2] text-[#b22537]'"
              >
                {{ completedCount }}/3
              </span>
            </div>

            <div class="space-y-2.5">
              <div
                v-for="item in completionItems"
                :key="item.label"
                class="flex items-start gap-3 rounded-xl border px-3.5 py-3"
                :class="item.ready ? 'border-[#dfeae4] bg-[#f8fcfa]' : 'border-[#eee3e1] bg-[#fcfaf9]'"
              >
                <span
                  class="mt-0.5 grid size-5 shrink-0 place-items-center rounded-full"
                  :class="item.ready ? 'bg-[#dff3e9] text-[#16764f]' : 'bg-[#f1e9e8] text-[#9b8b89]'"
                >
                  <svg
                    v-if="item.ready"
                    class="size-3"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2.6"
                    aria-hidden="true"
                  >
                    <path d="m5 12 4 4L19 6" />
                  </svg>
                  <span v-else class="size-1.5 rounded-full bg-current" />
                </span>

                <div class="min-w-0">
                  <p class="text-xs font-black text-[#4b4140]">{{ item.label }}</p>
                  <p class="mt-0.5 text-[11px] leading-4 text-[#8d7e7b]">{{ item.description }}</p>
                </div>
              </div>
            </div>

            <div
              class="mt-4 rounded-xl border px-3.5 py-3"
              :class="
                form.status === 'PUBLISHED'
                  ? 'border-[#d7e8df] bg-[#f3faf6]'
                  : 'border-[#eee2e1] bg-[#fbf8f7]'
              "
            >
              <p
                class="text-xs font-black"
                :class="form.status === 'PUBLISHED' ? 'text-[#16764f]' : 'text-[#6f625f]'"
              >
                {{ form.status === 'PUBLISHED' ? 'Sẽ xuất bản ngay' : 'Sẽ lưu dưới dạng bản nháp' }}
              </p>
              <p class="mt-1 text-[11px] leading-4 text-[#837472]">
                {{ submitHint }}
              </p>
            </div>
          </section>
        </aside>
      </div>

      <div
        class="sticky bottom-0 z-20 -mx-4 -mb-4 mt-7 border-t border-[#e5dcdb] bg-[#f8f7f5]/95 px-4 py-3.5 shadow-[0_-10px_30px_rgba(53,31,31,0.04)] backdrop-blur sm:-mx-5 sm:-mb-5 sm:px-5 lg:-mx-6 lg:px-6"
      >
        <div class="mx-auto flex max-w-[1360px] flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div class="hidden min-w-0 sm:block">
            <div class="flex items-center gap-2">
              <span
                class="grid size-6 place-items-center rounded-full text-[10px] font-black"
                :class="formReady ? 'bg-[#eaf7f0] text-[#16764f]' : 'bg-[#fff1f2] text-[#b22537]'"
              >
                {{ completedCount }}/3
              </span>
              <p class="text-xs font-black text-[#4a403f]">
                {{ formReady ? 'Thông tin hợp lệ' : 'Còn mục cần hoàn thiện' }}
              </p>
            </div>
            <p class="mt-1 max-w-xl truncate text-[11px] text-[#8d7e7b]">
              {{ submitHint }}
            </p>
          </div>

          <div class="flex flex-col-reverse gap-2.5 sm:flex-row sm:items-center">
            <button
              type="button"
              :disabled="saving"
              class="inline-flex h-11 items-center justify-center rounded-xl border border-[#d9cfce] bg-white px-5 text-sm font-bold text-[#6e605e] transition hover:bg-[#f5f1f0] focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-[#8d7c7a]/10 disabled:cursor-not-allowed disabled:opacity-50"
              @click="cancel"
            >
              Hủy
            </button>

            <button
              type="submit"
              :disabled="saving"
              class="inline-flex h-11 min-w-40 items-center justify-center gap-2 rounded-xl bg-[#c9152b] px-5 text-sm font-black text-white shadow-[0_7px_18px_rgba(160,0,28,0.17)] transition hover:bg-[#a0001c] focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-[#c9152b]/20 disabled:cursor-not-allowed disabled:opacity-60"
            >
              <svg
                v-if="saving"
                class="size-4 animate-spin"
                viewBox="0 0 24 24"
                fill="none"
                aria-hidden="true"
              >
                <circle class="opacity-30" cx="12" cy="12" r="9" stroke="currentColor" stroke-width="3" />
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
                stroke-width="2"
                aria-hidden="true"
              >
                <path d="M12 5v14M5 12h14" />
              </svg>

              {{ submitLabel }}
            </button>
          </div>
        </div>
      </div>
    </form>
  </section>
</template>

<style scoped>
.course-preview-grid {
  background-image:
    linear-gradient(to right, rgb(232 227 227 / 0.7) 1px, transparent 1px),
    linear-gradient(to bottom, rgb(232 227 227 / 0.7) 1px, transparent 1px);
  background-size: 22px 22px;
}
</style>
