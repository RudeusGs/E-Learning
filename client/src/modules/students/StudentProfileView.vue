<script setup lang="ts">
import { computed } from 'vue'

import { useAuthStore } from '@/modules/auth/auth.store'

const auth = useAuthStore()

const user = computed(() => auth.user)

const initials = computed(() => {
  const fullName = user.value?.fullName?.trim()
  if (!fullName) return 'MX'

  const parts = fullName.split(/\s+/).filter(Boolean)
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()

  return `${parts[0]?.[0] ?? ''}${parts[parts.length - 1]?.[0] ?? ''}`.toUpperCase()
})

const roleLabel = computed(() =>
  user.value?.role === 'STUDENT' ? 'Học viên' : (user.value?.role ?? ''),
)
</script>

<template>
  <section class="mx-auto w-full max-w-4xl pb-6">
    <!-- Heading -->
    <header class="mb-8 text-center sm:mb-10">
      <p class="text-[10px] font-black uppercase tracking-[0.17em] text-[#a0001c]">
        Tài khoản học tập
      </p>
      <h1 class="mt-2 text-[29px] font-black tracking-[-0.03em] text-[#1c1b1b] sm:text-[34px]">
        Hồ sơ cá nhân
      </h1>
      <p class="mx-auto mt-2 max-w-xl text-sm leading-6 text-[#6b5d5c] sm:text-[15px]">
        Xem thông tin tài khoản đang được sử dụng trong hệ thống E Leaning MindX.
      </p>
    </header>

    <!-- Profile card -->
    <section
      class="relative overflow-hidden rounded-2xl border border-[#e5dedd] bg-white shadow-[0_4px_20px_rgba(52,27,31,0.035)]"
    >
      <div
        class="profile-grid pointer-events-none absolute inset-0 opacity-40"
        aria-hidden="true"
      />
      <div
        class="pointer-events-none absolute -right-16 -top-24 size-64 rounded-full border-[46px] border-[#a0001c]/[0.035]"
        aria-hidden="true"
      />

      <div class="relative p-5 sm:p-8 lg:p-10">
        <!-- Identity -->
        <div
          class="flex flex-col items-center gap-6 border-b border-[#eee7e6] pb-8 text-center sm:flex-row sm:items-start sm:text-left"
        >
          <div class="relative shrink-0">
            <div
              class="grid size-28 place-items-center rounded-full border-4 border-[#f0e9e8] bg-[#fff1f2] text-2xl font-black tracking-[-0.03em] text-[#a0001c] shadow-[0_6px_18px_rgba(90,29,38,0.06)] sm:size-32"
              aria-label="Ảnh đại diện bằng chữ cái tên học viên"
            >
              {{ initials }}
            </div>

            <span
              class="absolute bottom-1 right-1 grid size-8 place-items-center rounded-full border-4 border-white bg-[#16875b] text-white"
              title="Đang hoạt động"
              aria-label="Tài khoản đang hoạt động"
            >
              <svg
                class="size-3.5"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2.6"
                aria-hidden="true"
              >
                <path d="m6 12 4 4 8-8" />
              </svg>
            </span>
          </div>

          <div class="min-w-0 flex-1 pt-1">
            <div class="flex flex-wrap items-center justify-center gap-2.5 sm:justify-start">
              <h2
                class="max-w-full truncate text-[24px] font-black tracking-[-0.025em] text-[#302929] sm:text-[27px]"
              >
                {{ user?.fullName || 'Học viên' }}
              </h2>

              <span
                class="inline-flex items-center gap-1.5 rounded-full border border-[#edc2c7] bg-[#fff3f4] px-2.5 py-1 text-[10px] font-black uppercase tracking-wide text-[#a0001c]"
              >
                <span class="size-1.5 rounded-full bg-current" />
                {{ roleLabel }}
              </span>
            </div>

            <p class="mt-2 text-sm font-medium text-[#756765]">
              {{ user?.email }}
            </p>

            <div
              class="mt-5 inline-flex max-w-full items-center gap-3 rounded-xl border border-[#e6dedc] bg-[#faf7f6] px-4 py-3 text-left"
            >
              <span
                class="grid size-9 shrink-0 place-items-center rounded-lg border border-[#ead9da] bg-white text-[#a0001c]"
              >
                <svg
                  class="size-4.5"
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
                <span
                  class="block text-[10px] font-bold uppercase tracking-[0.11em] text-[#958582]"
                >
                  Vai trò trong hệ thống
                </span>
                <strong class="mt-0.5 block text-sm font-black text-[#403735]">
                  {{ roleLabel }}
                </strong>
              </span>
            </div>
          </div>
        </div>

        <!-- Account information -->
        <div class="grid gap-x-8 gap-y-5 py-8 md:grid-cols-2">
          <div>
            <p class="mb-2 text-[10px] font-black uppercase tracking-[0.12em] text-[#91817e]">
              Họ và tên
            </p>
            <div
              class="flex min-h-12 items-center gap-3 rounded-xl border border-[#e2d9d8] bg-[#f8f5f4] px-4 py-3"
            >
              <svg
                class="size-4.5 shrink-0 text-[#9b8987]"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="1.8"
                aria-hidden="true"
              >
                <circle cx="12" cy="8" r="4" />
                <path d="M5 21a7 7 0 0 1 14 0" />
              </svg>
              <span class="min-w-0 truncate text-sm font-bold text-[#403735]">
                {{ user?.fullName || '—' }}
              </span>
            </div>
          </div>

          <div>
            <p class="mb-2 text-[10px] font-black uppercase tracking-[0.12em] text-[#91817e]">
              Email đăng nhập
            </p>
            <div
              class="flex min-h-12 items-center gap-3 rounded-xl border border-[#e2d9d8] bg-[#f8f5f4] px-4 py-3"
            >
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
              <span class="min-w-0 break-all text-sm font-bold text-[#403735]">
                {{ user?.email || '—' }}
              </span>
            </div>
          </div>

          <div>
            <p class="mb-2 text-[10px] font-black uppercase tracking-[0.12em] text-[#91817e]">
              Loại tài khoản
            </p>
            <div
              class="flex min-h-12 items-center gap-3 rounded-xl border border-[#e2d9d8] bg-[#f8f5f4] px-4 py-3"
            >
              <svg
                class="size-4.5 shrink-0 text-[#9b8987]"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="1.8"
                aria-hidden="true"
              >
                <path d="M12 3 4 7v5c0 5 3.4 8 8 9 4.6-1 8-4 8-9V7l-8-4Z" />
                <path d="m9 12 2 2 4-4" />
              </svg>
              <span class="text-sm font-bold text-[#403735]">
                {{ roleLabel }}
              </span>
            </div>
          </div>
        </div>

        <!-- Notice -->
        <div
          class="flex flex-col gap-4 rounded-xl border border-[#e3dbda] bg-[#f1eeed] p-4 sm:flex-row sm:items-start"
        >
          <span
            class="grid size-9 shrink-0 place-items-center rounded-full border border-[#eadcdd] bg-white text-[#a0001c]"
          >
            <svg
              class="size-4.5"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="1.9"
              aria-hidden="true"
            >
              <circle cx="12" cy="12" r="9" />
              <path d="M12 10.5V17M12 7h.01" />
            </svg>
          </span>

          <div class="min-w-0 flex-1">
            <p class="text-sm font-black text-[#403735]">
              Thông tin tài khoản được quản lý bởi hệ thống
            </p>
            <p class="mt-1 text-xs leading-5 text-[#756765]">
              Phiên bản hiện tại chưa có chức năng tự chỉnh sửa hồ sơ học viên. Nếu họ tên hoặc
              email chưa chính xác, vui lòng liên hệ quản trị viên để được cập nhật.
            </p>
          </div>
        </div>
      </div>
    </section>

    <!-- Supporting actions -->
    <div class="mt-6 grid gap-4 sm:grid-cols-2">
      <RouterLink
        to="/student/courses"
        class="group flex items-center gap-4 rounded-xl border border-[#e5dedd] bg-white p-4 transition hover:border-[#d7a8ad] hover:bg-[#fffafa]"
      >
        <span
          class="grid size-10 shrink-0 place-items-center rounded-xl bg-[#fff1f2] text-[#a0001c]"
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

        <span class="min-w-0 flex-1">
          <strong class="block text-sm font-black text-[#403735] group-hover:text-[#a0001c]">
            Khóa học của tôi
          </strong>
          <span class="mt-1 block text-xs leading-5 text-[#81716f]">
            Quay lại danh sách khóa học đang được phân công.
          </span>
        </span>

        <svg
          class="size-4 shrink-0 text-[#a3918f] transition group-hover:translate-x-0.5 group-hover:text-[#a0001c]"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          aria-hidden="true"
        >
          <path d="M5 12h14M14 7l5 5-5 5" />
        </svg>
      </RouterLink>

      <div class="flex items-center gap-4 rounded-xl border border-[#e5dedd] bg-[#faf8f7] p-4">
        <span
          class="grid size-10 shrink-0 place-items-center rounded-xl bg-[#f0eceb] text-[#756765]"
        >
          <svg
            class="size-5"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="1.8"
            aria-hidden="true"
          >
            <path d="M12 3 4 7v5c0 5 3.4 8 8 9 4.6-1 8-4 8-9V7l-8-4Z" />
            <path d="M9 12h6" />
          </svg>
        </span>

        <span class="min-w-0">
          <strong class="block text-sm font-black text-[#4f4442]"> Hồ sơ chỉ đọc </strong>
          <span class="mt-1 block text-xs leading-5 text-[#81716f]">
            Không có thao tác lưu giả hoặc upload avatar giả ở frontend.
          </span>
        </span>
      </div>
    </div>
  </section>
</template>

<style scoped>
.profile-grid {
  background-image:
    linear-gradient(to right, rgb(127 16 32 / 0.025) 1px, transparent 1px),
    linear-gradient(to bottom, rgb(127 16 32 / 0.025) 1px, transparent 1px);
  background-size: 26px 26px;
}
</style>
