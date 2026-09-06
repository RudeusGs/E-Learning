<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router'

import ContextHelpDrawer from '@/components/ContextHelpDrawer.vue'
import { useAuthStore } from '@/modules/auth/auth.store'

interface NavItem {
  to: string
  label: string
  icon: 'dashboard' | 'courses' | 'students' | 'progress' | 'profile'
}

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

const mobileMenuOpen = ref(false)
const helpOpen = ref(false)
const signingOut = ref(false)

const isAdmin = computed(() => auth.user?.role === 'ADMIN')

const adminLinks: NavItem[] = [
  { to: '/admin/dashboard', label: 'Tổng quan', icon: 'dashboard' },
  { to: '/admin/courses', label: 'Khóa học', icon: 'courses' },
  { to: '/admin/students', label: 'Học viên', icon: 'students' },
  { to: '/admin/progress', label: 'Tiến độ học tập', icon: 'progress' },
]

const studentLinks: NavItem[] = [
  { to: '/student/courses', label: 'Khóa học của tôi', icon: 'courses' },
  { to: '/student/profile', label: 'Hồ sơ', icon: 'profile' },
]

const pageTitle = computed(() => {
  const path = route.path

  if (path.startsWith('/admin/dashboard')) return 'Tổng quan'
  if (path === '/admin/courses/create') return 'Tạo khóa học'
  if (/^\/admin\/courses\/[^/]+\/edit$/.test(path)) return 'Chỉnh sửa khóa học'
  if (/^\/admin\/courses\/[^/]+\/lessons\/create$/.test(path)) return 'Tạo bài học'
  if (path.startsWith('/admin/courses')) return 'Khóa học'
  if (/^\/admin\/lessons\/[^/]+\/exercises$/.test(path)) return 'Câu hỏi & kiểm tra'
  if (/^\/admin\/lessons\/[^/]+\/edit$/.test(path)) return 'Chỉnh sửa bài học'
  if (path.startsWith('/admin/lessons')) return 'Bài học'
  if (/^\/admin\/students\/[^/]+$/.test(path)) return 'Chi tiết học viên'
  if (path.startsWith('/admin/students')) return 'Học viên'
  if (path.startsWith('/admin/progress')) return 'Tiến độ học tập'
  if (path.startsWith('/student/profile')) return 'Hồ sơ'
  if (path.startsWith('/student/learn')) return 'Bài học'
  if (/^\/student\/courses\/[^/]+\/progress$/.test(path)) return 'Tiến độ khóa học'
  if (path.startsWith('/student/courses')) return 'Khóa học'

  return 'E Leaning MindX'
})

const userInitials = computed(() => {
  const fullName = auth.user?.fullName?.trim()
  if (!fullName) return 'MX'

  const parts = fullName.split(/\s+/).filter(Boolean)
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()

  return `${parts[0][0] ?? ''}${parts[parts.length - 1][0] ?? ''}`.toUpperCase()
})

watch(
  () => route.fullPath,
  () => {
    mobileMenuOpen.value = false
    helpOpen.value = false
  },
)

async function signOut(): Promise<void> {
  if (signingOut.value) return

  signingOut.value = true
  try {
    await auth.signOut()
    await router.replace('/login')
  } finally {
    signingOut.value = false
  }
}
</script>

<template>
  <div v-if="isAdmin" class="min-h-screen bg-[#f8f7f5] text-[#1c1b1b]">
    <aside
      class="fixed inset-y-0 left-0 z-40 hidden w-[252px] flex-col border-r border-white/10 bg-[#7f1020] text-white lg:flex"
    >
      <div class="px-4 pb-4 pt-4">
        <RouterLink to="/admin/dashboard" class="flex items-center gap-3 rounded-xl">
          <span
            class="grid size-10 shrink-0 place-items-center overflow-hidden rounded-xl border border-white/15 bg-white shadow-lg shadow-black/10"
          >
            <img src="/favicon.ico" alt="" class="size-7 object-contain" />
          </span>
          <span class="min-w-0">
            <strong class="block truncate text-[15px] font-black tracking-[-0.02em]">
              E Leaning MindX
            </strong>
            <span class="mt-0.5 block text-[10px] font-medium text-white/55">Hệ thống Giáo dục</span>
          </span>
        </RouterLink>
      </div>

      <div class="mx-5 h-px bg-white/10" />

      <nav class="flex-1 overflow-y-auto px-3 py-4" aria-label="Điều hướng quản trị">
        <p class="mb-2 px-3 text-[9px] font-bold uppercase tracking-[0.18em] text-white/35">
          Quản trị hệ thống
        </p>

        <div class="space-y-1">
          <RouterLink
            v-for="link in adminLinks"
            :key="link.to"
            :to="link.to"
            class="group flex h-10 items-center gap-2.5 rounded-lg px-3 text-[13px] font-semibold text-white/62 transition hover:bg-white/[0.07] hover:text-white"
            active-class="bg-white/[0.11] text-white"
          >
            <span
              class="grid size-7 shrink-0 place-items-center rounded-md text-white/65 transition group-hover:text-white"
            >
              <svg
                v-if="link.icon === 'dashboard'"
                class="size-4.5"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="1.8"
                aria-hidden="true"
              >
                <rect x="3" y="3" width="7" height="7" rx="2" />
                <rect x="14" y="3" width="7" height="7" rx="2" />
                <rect x="3" y="14" width="7" height="7" rx="2" />
                <rect x="14" y="14" width="7" height="7" rx="2" />
              </svg>

              <svg
                v-else-if="link.icon === 'courses'"
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

              <svg
                v-else-if="link.icon === 'students'"
                class="size-4.5"
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
                class="size-4.5"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="1.8"
                aria-hidden="true"
              >
                <path d="M4 19V9M10 19V5M16 19v-7M22 19H2" />
              </svg>
            </span>

            <span>{{ link.label }}</span>
          </RouterLink>
        </div>
      </nav>

      <div class="m-3 border-t border-white/10 pt-3">
        <div class="mb-3 flex items-center gap-3 rounded-xl bg-black/[0.08] p-2.5">
          <span
            class="grid size-10 shrink-0 place-items-center rounded-full border border-white/15 bg-[#c9152b] text-xs font-black text-white"
          >
            {{ userInitials }}
          </span>
          <span class="min-w-0 flex-1">
            <strong class="block truncate text-[13px] font-bold">{{ auth.user?.fullName }}</strong>
            <span class="mt-0.5 block truncate text-[11px] font-medium text-white/50">
              Quản trị viên
            </span>
          </span>
        </div>

        <button
          type="button"
          :disabled="signingOut"
          class="flex w-full items-center justify-center gap-2 rounded-lg px-3 py-2 text-[13px] font-semibold text-white/65 transition hover:bg-white/[0.08] hover:text-white disabled:cursor-not-allowed disabled:opacity-50"
          @click="signOut"
        >
          <svg
            class="size-4.5"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="1.8"
            aria-hidden="true"
          >
            <path d="M10 17l5-5-5-5M15 12H3" />
            <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4" />
          </svg>
          {{ signingOut ? 'Đang đăng xuất…' : 'Đăng xuất' }}
        </button>
      </div>
    </aside>

    <div v-if="mobileMenuOpen" class="fixed inset-0 z-50 lg:hidden">
      <button
        type="button"
        aria-label="Đóng menu"
        class="absolute inset-0 bg-black/45 backdrop-blur-[2px]"
        @click="mobileMenuOpen = false"
      />

      <aside
        class="absolute inset-y-0 left-0 flex w-[86%] max-w-[300px] flex-col bg-[#7f1020] text-white shadow-2xl"
      >
        <div class="flex items-center justify-between px-5 py-5">
          <RouterLink to="/admin/dashboard" class="flex items-center gap-3">
            <span
              class="grid size-11 place-items-center overflow-hidden rounded-xl border border-white/15 bg-white"
            >
              <img src="/favicon.ico" alt="" class="size-7 object-contain" />
            </span>
            <span>
              <strong class="block text-base font-black">E Leaning MindX</strong>
              <span class="text-[11px] font-medium text-white/50">Hệ thống Giáo dục</span>
            </span>
          </RouterLink>

          <button
            type="button"
            aria-label="Đóng menu"
            class="grid size-9 place-items-center rounded-lg text-white/65 hover:bg-white/10 hover:text-white"
            @click="mobileMenuOpen = false"
          >
            <svg
              class="size-4.5"
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

        <nav class="flex-1 px-4 py-4" aria-label="Điều hướng quản trị trên di động">
          <div class="space-y-1">
            <RouterLink
              v-for="link in adminLinks"
              :key="link.to"
              :to="link.to"
              class="flex h-11 items-center gap-3 rounded-lg px-4 text-sm font-semibold text-white/65 hover:bg-white/[0.07] hover:text-white"
              active-class="bg-white/[0.11] text-white"
            >
              <span class="size-1.5 rounded-full bg-current" />
              {{ link.label }}
            </RouterLink>
          </div>
        </nav>

        <div class="m-3 border-t border-white/10 pt-3">
          <p class="truncate px-2 text-sm font-bold">{{ auth.user?.fullName }}</p>
          <p class="mt-1 truncate px-2 text-xs text-white/45">{{ auth.user?.email }}</p>
          <button
            type="button"
            :disabled="signingOut"
            class="mt-4 w-full rounded-xl border border-white/10 px-4 py-2.5 text-sm font-semibold text-white/70 hover:bg-white/10 hover:text-white"
            @click="signOut"
          >
            {{ signingOut ? 'Đang đăng xuất…' : 'Đăng xuất' }}
          </button>
        </div>
      </aside>
    </div>

    <div class="min-h-screen lg:pl-[252px]">
      <header
        class="sticky top-0 z-30 flex h-14 items-center border-b border-[#e8e3e3] bg-white/95 px-4 backdrop-blur sm:px-6 lg:px-8"
      >
        <button
          type="button"
          aria-label="Mở menu"
          class="mr-3 grid size-10 place-items-center rounded-xl border border-[#e8e3e3] text-[#5c403e] hover:bg-[#f8f7f5] lg:hidden"
          @click="mobileMenuOpen = true"
        >
          <svg
            class="size-4.5"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            aria-hidden="true"
          >
            <path d="M4 7h16M4 12h16M4 17h16" />
          </svg>
        </button>

        <div class="flex min-w-0 items-center gap-2 text-sm">
          <span class="hidden font-medium text-[#806f6e] sm:inline">Quản trị</span>
          <svg
            class="hidden size-4 text-[#ad9b99] sm:block"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            aria-hidden="true"
          >
            <path d="m9 18 6-6-6-6" />
          </svg>
          <strong class="truncate font-bold text-[#302929]">{{ pageTitle }}</strong>
        </div>

        <div class="ml-auto flex items-center gap-2">
          <button
            type="button"
            class="inline-flex h-9 items-center gap-2 rounded-lg border border-[#e4dcdb] bg-white px-3 text-xs font-bold text-[#665957] transition hover:border-[#d7a8ad] hover:bg-[#fff4f5] hover:text-[#a0001c]"
            aria-label="Mở hướng dẫn cho màn hình hiện tại"
            @click="helpOpen = true"
          >
            <span class="grid size-5 place-items-center rounded-full bg-[#fff0f1] text-[11px] font-black text-[#a0001c]">?</span>
            <span class="hidden sm:inline">Hướng dẫn</span>
          </button>
          <span
            class="hidden max-w-[210px] truncate text-right text-xs font-semibold text-[#6b5d5c] md:block"
          >
            {{ auth.user?.email }}
          </span>
          <span
            class="grid size-9 shrink-0 place-items-center rounded-full bg-[#c9152b] text-[11px] font-black text-white shadow-sm"
          >
            {{ userInitials }}
          </span>
        </div>
      </header>

      <main class="mx-auto w-full max-w-[1360px] px-4 py-4 sm:px-5 sm:py-5 lg:px-6">
        <RouterView />
      </main>
    </div>
  </div>

  <div v-else class="min-h-screen bg-[#f8f7f5] text-[#1c1b1b]">
    <header
      class="sticky top-0 z-40 border-b border-[#e8e3e3] bg-white/95 shadow-sm shadow-black/[0.02] backdrop-blur"
    >
      <div
        class="mx-auto flex max-w-7xl flex-wrap items-center gap-3 px-4 py-3 sm:px-6 lg:flex-nowrap lg:gap-6"
      >
        <RouterLink
          to="/student/courses"
          class="flex shrink-0 items-center gap-2.5 font-black tracking-tight text-[#7f1020]"
        >
          <span
            class="grid size-9 place-items-center overflow-hidden rounded-lg border border-[#eadcdd] bg-white"
          >
            <img src="/favicon.ico" alt="" class="size-6 object-contain" />
          </span>
          <span class="hidden sm:inline">E Leaning MindX</span>
        </RouterLink>

        <nav
          class="order-3 flex w-full gap-1 overflow-x-auto rounded-xl bg-[#f4efee] p-1 text-sm font-semibold lg:order-none lg:w-auto lg:flex-1 lg:bg-transparent lg:p-0"
          aria-label="Điều hướng học viên"
        >
          <RouterLink
            v-for="link in studentLinks"
            :key="link.to"
            :to="link.to"
            class="whitespace-nowrap rounded-lg px-3.5 py-2 text-[#6b5d5c] hover:bg-white hover:text-[#a0001c]"
            active-class="bg-white text-[#a0001c] shadow-sm lg:bg-[#fff1f2] lg:shadow-none"
          >
            {{ link.label }}
          </RouterLink>
        </nav>

        <div class="ml-auto flex items-center gap-2 lg:ml-0">
          <button
            type="button"
            class="inline-flex h-9 items-center gap-1.5 rounded-lg border border-[#e4dcdb] bg-white px-2.5 text-xs font-bold text-[#665957] transition hover:border-[#d7a8ad] hover:bg-[#fff4f5] hover:text-[#a0001c]"
            aria-label="Mở hướng dẫn cho màn hình hiện tại"
            @click="helpOpen = true"
          >
            <span class="grid size-5 place-items-center rounded-full bg-[#fff0f1] text-[11px] font-black text-[#a0001c]">?</span>
            <span class="hidden xl:inline">Hướng dẫn</span>
          </button>
          <div class="hidden text-right sm:block">
            <p class="max-w-40 truncate text-sm font-bold">{{ auth.user?.fullName }}</p>
            <p class="text-[11px] font-medium text-[#806f6e]">Học viên</p>
          </div>
          <span
            class="grid size-9 place-items-center rounded-full bg-[#c9152b] text-[11px] font-black text-white"
          >
            {{ userInitials }}
          </span>
          <button
            type="button"
            :disabled="signingOut"
            class="rounded-lg border border-[#e8e3e3] bg-white px-3 py-2 text-xs font-bold text-[#6b5d5c] hover:border-[#efb6bc] hover:bg-[#fff1f2] hover:text-[#a0001c] disabled:opacity-50"
            @click="signOut"
          >
            Đăng xuất
          </button>
        </div>
      </div>
    </header>

    <main class="mx-auto w-full max-w-7xl px-4 py-7 sm:px-6 sm:py-10">
      <RouterView />
    </main>
  </div>

  <ContextHelpDrawer
    :open="helpOpen"
    :role="isAdmin ? 'ADMIN' : 'STUDENT'"
    @close="helpOpen = false"
  />
</template>