<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'
import { useAuthStore } from '@/modules/auth/auth.store'

import * as dashboardApi from './dashboard.api'
import type { Dashboard } from './models/Dashboard'

const auth = useAuthStore()

const dashboard = ref<Dashboard | null>(null)
const loading = ref(true)
const problem = ref<ProblemDetails | null>(null)
const currentTime = ref(new Date())

let clockTimer: ReturnType<typeof setInterval> | null = null

const greeting = computed(() => {
  const hour = currentTime.value.getHours()

  if (hour >= 5 && hour < 11) return 'Chào buổi sáng'
  if (hour >= 11 && hour < 14) return 'Chào buổi trưa'
  if (hour >= 14 && hour < 18) return 'Chào buổi chiều'
  if (hour >= 18 && hour < 24) return 'Chào buổi tối'

  return 'Xin chào'
})

const currentDateLabel = computed(() => {
  const value = new Intl.DateTimeFormat('vi-VN', {
    weekday: 'long',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(currentTime.value)

  return value.charAt(0).toUpperCase() + value.slice(1)
})

const adminName = computed(() => auth.user?.fullName?.trim() || 'Quản trị viên')

const metrics = computed(() => [
  {
    key: 'courses',
    label: 'Tổng khóa học',
    value: dashboard.value?.courseCount ?? 0,
    icon: 'courses',
  },
  {
    key: 'students',
    label: 'Học viên đang hoạt động',
    value: dashboard.value?.studentCount ?? 0,
    icon: 'students',
  },
  {
    key: 'lessons',
    label: 'Tổng bài học',
    value: dashboard.value?.lessonCount ?? 0,
    icon: 'lessons',
  },
  {
    key: 'completion',
    label: 'Lượt hoàn thành bài học',
    value: dashboard.value?.lessonCompletionCount ?? 0,
    icon: 'completion',
  },
])

const quickAccess = [
  {
    to: '/admin/courses',
    title: 'Quản lý khóa học',
    description: 'Xem, chỉnh sửa và tổ chức nội dung các khóa học hiện có.',
    icon: 'courses',
  },
  {
    to: '/admin/courses/create',
    title: 'Tạo khóa học mới',
    description: 'Thiết lập khóa học mới và bắt đầu xây dựng lộ trình bài học.',
    icon: 'create',
  },
  {
    to: '/admin/students',
    title: 'Quản lý học viên',
    description: 'Quản lý tài khoản học viên, trạng thái và các khóa học được phân công.',
    icon: 'students',
  },
  {
    to: '/admin/progress',
    title: 'Xem tiến độ',
    description: 'Theo dõi tỷ lệ hoàn thành theo từng học viên và từng khóa học.',
    icon: 'progress',
  },
]

function formatNumber(value: number): string {
  return new Intl.NumberFormat('vi-VN').format(value)
}

async function loadDashboard(): Promise<void> {
  loading.value = true
  problem.value = null

  try {
    dashboard.value = await dashboardApi.getDashboard()
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  void loadDashboard()

  // Lấy giờ trực tiếp bằng JavaScript trên trình duyệt.
  // Cập nhật mỗi phút để lời chào tự đổi nếu trang đang mở khi chuyển khung giờ.
  clockTimer = window.setInterval(() => {
    currentTime.value = new Date()
  }, 60_000)
})

onBeforeUnmount(() => {
  if (clockTimer !== null) {
    window.clearInterval(clockTimer)
  }
})
</script>

<template>
  <section class="flex h-[calc(100vh-88px)] flex-col gap-4 overflow-hidden sm:h-[calc(100vh-96px)] lg:gap-6">
    <header
      class="relative shrink-0 overflow-hidden rounded-2xl border border-[#eadfdf] bg-white px-6 py-5 shadow-[0_1px_2px_rgba(38,20,20,0.02)] sm:px-8 sm:py-6"
    >
      <div
        class="pointer-events-none absolute -right-20 -top-24 size-72 rounded-full bg-[#fff1f2]"
        aria-hidden="true"
      />
      <div
        class="pointer-events-none absolute right-10 top-1/2 hidden size-32 -translate-y-1/2 rounded-full border border-[#f3d5d8] xl:block"
        aria-hidden="true"
      />
      <div
        class="pointer-events-none absolute right-[86px] top-1/2 hidden size-2 -translate-y-1/2 rounded-full bg-[#c9152b] xl:block"
        aria-hidden="true"
      />

      <div class="relative z-10 flex flex-col justify-between gap-6 xl:flex-row xl:items-end">
        <div class="min-w-0">
          <div
            class="mb-3 inline-flex items-center gap-2 rounded-full border border-[#f0d5d8] bg-[#fff7f7] px-3 py-1.5 text-xs font-bold text-[#a0001c]"
          >
            <span class="size-1.5 rounded-full bg-[#c9152b]" />
            {{ currentDateLabel }}
          </div>

          <h1
            class="max-w-4xl text-[30px] font-black leading-[1.18] tracking-[-0.03em] text-[#1c1b1b] sm:text-[38px] lg:text-[42px]"
          >
            {{ greeting }},
            <span class="text-[#7f1020]">{{ adminName }}</span>
          </h1>

          <p class="mt-3 max-w-2xl text-[15px] leading-6 text-[#6b5d5c] sm:text-base">
            Tổng quan nội dung và hoạt động học tập đang được quản lý trong E Leaning MindX.
          </p>
        </div>

        <RouterLink
          to="/admin/courses/create"
          class="inline-flex h-11 shrink-0 items-center justify-center gap-2 self-start rounded-xl bg-[#c9152b] px-4 text-sm font-bold text-white shadow-[0_8px_18px_rgba(160,0,28,0.16)] transition hover:bg-[#a0001c] focus:outline-none focus-visible:ring-4 focus-visible:ring-[#c9152b]/20 xl:self-auto"
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
        </RouterLink>
      </div>
    </header>

    <ProblemAlert :problem="problem" />

    <section aria-labelledby="dashboard-metrics-title" class="shrink-0">
      <div class="mb-4 flex items-end justify-between gap-4">
        <div>
          <p class="text-xs font-bold uppercase tracking-[0.16em] text-[#9b8583]">Số liệu hệ thống</p>
          <h2 id="dashboard-metrics-title" class="mt-1 text-xl font-black tracking-[-0.02em]">
            Tổng quan
          </h2>
        </div>

        <button
          v-if="problem"
          type="button"
          class="rounded-lg border border-[#e6dada] bg-white px-3 py-2 text-xs font-bold text-[#7f1020] hover:bg-[#fff1f2]"
          @click="loadDashboard"
        >
          Thử tải lại
        </button>
      </div>

      <div
        v-if="loading"
        class="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4"
        aria-label="Đang tải số liệu dashboard"
      >
        <div
          v-for="index in 4"
          :key="index"
          class="overflow-hidden rounded-2xl border border-[#e8e3e3] bg-white p-5"
        >
          <div class="h-4 w-32 animate-pulse rounded bg-[#eee8e7]" />
          <div class="mt-5 h-10 w-24 animate-pulse rounded-lg bg-[#eee8e7]" />
          <div class="mt-4 h-3 w-20 animate-pulse rounded bg-[#f3eeee]" />
        </div>
      </div>

      <div v-else-if="dashboard" class="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <article
          v-for="metric in metrics"
          :key="metric.key"
          class="group relative overflow-hidden rounded-2xl border border-[#e8e3e3] bg-white p-4 transition duration-200 hover:-translate-y-0.5 hover:border-[#e1c5c8] hover:shadow-[0_10px_30px_rgba(84,31,39,0.06)] sm:p-5"
        >
          <div class="absolute inset-y-0 left-0 w-1 bg-[#c9152b]" />
          <div class="metric-dots pointer-events-none absolute -right-5 -top-5 size-24 opacity-45" />

          <div class="relative z-10 flex items-start justify-between gap-4">
            <div>
              <p class="text-[11px] font-bold uppercase tracking-[0.12em] text-[#806f6e]">
                {{ metric.label }}
              </p>
              <p class="mt-3 text-[36px] font-black leading-none tracking-[-0.035em] text-[#1c1b1b]">
                {{ formatNumber(metric.value) }}
              </p>
            </div>

            <span
              class="grid size-10 shrink-0 place-items-center rounded-xl border border-[#f0dddd] bg-[#fff5f5] text-[#c9152b] transition group-hover:bg-[#c9152b] group-hover:text-white"
            >
              <svg
                v-if="metric.icon === 'courses'"
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

              <svg
                v-else-if="metric.icon === 'students'"
                class="size-5"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="1.8"
                aria-hidden="true"
              >
                <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
                <circle cx="9" cy="7" r="4" />
                <path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" />
              </svg>

              <svg
                v-else-if="metric.icon === 'lessons'"
                class="size-5"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="1.8"
                aria-hidden="true"
              >
                <rect x="4" y="3" width="16" height="18" rx="2" />
                <path d="M8 7h8M8 11h8M8 15h5" />
              </svg>

              <svg
                v-else
                class="size-5"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="1.8"
                aria-hidden="true"
              >
                <circle cx="12" cy="12" r="9" />
                <path d="m8 12 2.5 2.5L16.5 9" />
              </svg>
            </span>
          </div>

        </article>
      </div>
    </section>

    <section aria-labelledby="quick-access-title" class="flex min-h-0 flex-1 flex-col">
      <div class="mb-4">
        <p class="text-xs font-bold uppercase tracking-[0.16em] text-[#9b8583]">Điều hướng</p>
        <h2 id="quick-access-title" class="mt-1 text-xl font-black tracking-[-0.02em]">
          Truy cập nhanh
        </h2>
      </div>

      <div class="grid min-h-0 flex-1 grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
        <RouterLink
          v-for="item in quickAccess"
          :key="item.to"
          :to="item.to"
          class="group relative flex min-h-0 flex-1 flex-col overflow-hidden rounded-2xl border border-[#e8e3e3] bg-white p-4 transition duration-200 hover:-translate-y-0.5 hover:border-[#dcb9bd] hover:shadow-[0_12px_34px_rgba(84,31,39,0.07)] sm:p-5"
        >
          <div
            class="mb-5 grid size-11 place-items-center rounded-xl border border-[#ebe3e2] bg-[#f6f3f2] text-[#6b5d5c] transition group-hover:border-[#f1d1d4] group-hover:bg-[#fff1f2] group-hover:text-[#c9152b]"
          >
            <svg
              v-if="item.icon === 'courses'"
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

            <svg
              v-else-if="item.icon === 'create'"
              class="size-5"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="1.8"
              aria-hidden="true"
            >
              <path d="M12 5v14M5 12h14" />
              <circle cx="12" cy="12" r="9" />
            </svg>

            <svg
              v-else-if="item.icon === 'students'"
              class="size-5"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="1.8"
              aria-hidden="true"
            >
              <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
              <circle cx="9" cy="7" r="4" />
              <path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" />
            </svg>

            <svg
              v-else
              class="size-5"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="1.8"
              aria-hidden="true"
            >
              <path d="M4 19V9M10 19V5M16 19v-7M22 19H2" />
            </svg>
          </div>

          <h3 class="text-[17px] font-black tracking-[-0.02em] text-[#2b2525]">
            {{ item.title }}
          </h3>
          <p class="mt-2 text-sm leading-6 text-[#786a68]">
            {{ item.description }}
          </p>

          <span
            class="mt-auto flex items-center gap-1.5 pt-3 text-sm font-bold text-[#a0001c] transition group-hover:gap-2.5 sm:pt-4"
          >
            Truy cập
            <svg
              class="size-4"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              aria-hidden="true"
            >
              <path d="M5 12h14M14 7l5 5-5 5" />
            </svg>
          </span>
        </RouterLink>
      </div>
    </section>
  </section>
</template>

<style scoped>
.metric-dots {
  background-image: radial-gradient(#e4d6d5 1px, transparent 1px);
  background-size: 8px 8px;
}
</style>
