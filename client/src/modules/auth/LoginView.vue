<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { toProblem, type ProblemDetails } from '@/api/http'

import { useAuthStore } from './auth.store'

const REMEMBERED_EMAIL_KEY = 'elearning-mindx:remembered-email'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const email = ref('')
const password = ref('')
const rememberEmail = ref(false)
const showPassword = ref(false)
const loading = ref(false)
const problem = ref<ProblemDetails | null>(null)

const errorMessage = computed(() => {
  if (!problem.value) return null

  if (problem.value.code === 'MISSING_FIELDS') {
    return 'Vui lòng nhập đầy đủ email và mật khẩu để tiếp tục.'
  }

  if (problem.value.code === 'INVALID_CREDENTIALS' || problem.value.status === 401) {
    return 'Email hoặc mật khẩu không chính xác. Vui lòng kiểm tra lại.'
  }

  if (problem.value.code === 'RATE_LIMITED' || problem.value.status === 429) {
    return 'Bạn đã thử đăng nhập quá nhiều lần. Vui lòng thử lại sau.'
  }

  if (problem.value.status === 0 || problem.value.code === 'NETWORK_ERROR') {
    return 'Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối và thử lại.'
  }

  return problem.value.detail || problem.value.title || 'Đăng nhập không thành công. Vui lòng thử lại.'
})

const errorTitle = computed(() => {
  if (!problem.value) return ''
  if (problem.value.code === 'MISSING_FIELDS') return 'Thông tin chưa đầy đủ'
  return 'Không thể đăng nhập'
})

onMounted(() => {
  const rememberedEmail = window.localStorage.getItem(REMEMBERED_EMAIL_KEY)
  if (!rememberedEmail) return

  email.value = rememberedEmail
  rememberEmail.value = true
})

async function submit(): Promise<void> {
  if (loading.value) return

  if (!email.value.trim() || !password.value) {
    problem.value = { code: 'MISSING_FIELDS' } as any
    return
  }

  loading.value = true
  problem.value = null

  try {
    await auth.signIn(email.value.trim(), password.value)

    if (rememberEmail.value) {
      window.localStorage.setItem(REMEMBERED_EMAIL_KEY, email.value.trim())
    } else {
      window.localStorage.removeItem(REMEMBERED_EMAIL_KEY)
    }

    const fallback = auth.user?.role === 'ADMIN' ? '/admin/dashboard' : '/student/courses'
    const redirect = getSafeRedirect(route.query.redirect, fallback)
    await router.replace(redirect)
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}

function togglePassword(): void {
  showPassword.value = !showPassword.value
}

function getSafeRedirect(value: unknown, fallback: string): string {
  return typeof value === 'string' && value.startsWith('/') && !value.startsWith('//')
    ? value
    : fallback
}
</script>

<template>
  <main
    class="min-h-screen bg-[#fcf9f8] font-sans text-[#1c1b1b] lg:grid lg:h-screen lg:grid-cols-[55%_45%] lg:overflow-hidden"
  >
    <!-- Brand / learning-path panel -->
    <section
      class="academic-grid relative hidden overflow-hidden bg-[#7f1020] px-12 py-10 text-white lg:flex lg:flex-col"
      aria-label="Giới thiệu E Leaning MindX"
    >
      <div
        class="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_82%_20%,rgba(255,255,255,0.12),transparent_24rem),radial-gradient(circle_at_15%_85%,rgba(201,21,43,0.38),transparent_28rem)]"
      />

      <div
        class="pointer-events-none absolute -right-24 -top-24 h-96 w-96 rounded-full border border-white/10"
      />
      <div
        class="pointer-events-none absolute -right-12 -top-12 h-72 w-72 rounded-full border border-white/10"
      />

      <!-- Abstract learning path -->
      <div class="pointer-events-none absolute inset-0" aria-hidden="true">
        <div
          class="absolute left-[61%] top-[19%] h-[52%] w-px bg-gradient-to-b from-white/5 via-white/20 to-white/5"
        />
        <div
          class="absolute left-[61%] top-[47%] h-px w-[25%] origin-left rotate-[28deg] bg-gradient-to-r from-white/20 to-white/5"
        />
        <div
          class="absolute left-[38%] top-[69%] h-px w-[23%] origin-right -rotate-[22deg] bg-gradient-to-l from-white/20 to-white/5"
        />

        <span
          class="learning-node absolute left-[61%] top-[19%]"
        />
        <span
          class="learning-node learning-node-active absolute left-[61%] top-[43%]"
        />
        <span
          class="learning-node absolute left-[61%] top-[70%]"
        />
        <span
          class="learning-node absolute left-[74%] top-[54%]"
        />
        <span
          class="learning-node absolute left-[84%] top-[67%]"
        />
        <span
          class="learning-node absolute left-[38%] top-[69%]"
        />

        <div
          class="absolute left-[64%] top-[39%] rounded-xl border border-white/15 bg-black/10 px-4 py-3 backdrop-blur-sm"
        >
          <p class="text-[10px] font-bold uppercase tracking-[0.18em] text-white/55">Bài hiện tại</p>
          <p class="mt-1 text-sm font-semibold text-white">Tiếp tục học tập</p>
        </div>
      </div>

      <!-- Logo -->
      <div class="relative z-10 flex items-center gap-3">
        <div
          class="grid size-11 place-items-center overflow-hidden rounded-xl border border-white/20 bg-white shadow-lg shadow-black/10"
        >
          <img src="/favicon.ico" alt="" class="size-8 object-contain" />
        </div>
        <div>
          <p class="text-lg font-bold tracking-tight">E Leaning MindX</p>
          <p class="text-xs font-medium text-white/55">Learning Management System</p>
        </div>
      </div>

      <!-- Main copy -->
      <div class="relative z-10 mt-auto max-w-[570px] pb-9">
        <div
          class="mb-5 inline-flex items-center gap-2 rounded-full border border-white/15 bg-white/10 px-3.5 py-2 text-xs font-semibold text-white/85 backdrop-blur"
        >
          <span class="size-1.5 rounded-full bg-[#ffb3af]" />
          Nền tảng học tập
        </div>

        <h1 class="max-w-[560px] text-[46px] font-black leading-[1.13] tracking-[-0.035em]">
          Không gian học tập
          <span class="text-[#ffb3af]">của nhà trường</span>
        </h1>

        <p class="mt-5 max-w-[535px] text-[17px] leading-7 text-white/72">
          Học đúng lộ trình, hoàn thành từng bài và theo dõi tiến độ trong một không gian học tập
          hiện đại, trực quan và tập trung.
        </p>

        <div class="mt-9 flex flex-wrap gap-3">
          <div
            class="inline-flex items-center gap-2.5 rounded-full border border-white/15 bg-white/[0.08] px-4 py-2.5 text-sm font-semibold text-white/90 backdrop-blur-sm"
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
            Khóa học
          </div>

          <div
            class="inline-flex items-center gap-2.5 rounded-full border border-white/15 bg-white/[0.08] px-4 py-2.5 text-sm font-semibold text-white/90 backdrop-blur-sm"
          >
            <svg
              class="size-4.5"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="1.8"
              aria-hidden="true"
            >
              <path d="M5 4.5h11a3 3 0 0 1 3 3v12H8a3 3 0 0 1-3-3v-12Z" />
              <path d="M8 8h7M8 12h7" />
            </svg>
            Bài học
          </div>

          <div
            class="inline-flex items-center gap-2.5 rounded-full border border-white/15 bg-white/[0.08] px-4 py-2.5 text-sm font-semibold text-white/90 backdrop-blur-sm"
          >
            <svg
              class="size-4.5"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="1.8"
              aria-hidden="true"
            >
              <path d="M4 19V9M10 19V5M16 19v-7M22 19H2" />
            </svg>
            Tiến độ
          </div>
        </div>
      </div>

      <p class="relative z-10 text-xs font-medium text-white/38">
        © MindX · Hệ thống học tập
      </p>
    </section>

    <!-- Login panel -->
    <section class="relative flex min-h-screen items-center justify-center px-5 py-10 sm:px-8 lg:min-h-0">
      <div
        class="pointer-events-none absolute inset-x-0 top-0 h-40 bg-[radial-gradient(circle_at_50%_0%,rgba(201,21,43,0.06),transparent_70%)] lg:hidden"
      />

      <div class="relative w-full max-w-[430px]">
        <!-- Mobile logo -->
        <div class="mb-9 flex items-center justify-center gap-3 lg:hidden">
          <div
            class="grid size-11 place-items-center overflow-hidden rounded-xl border border-[#eadcdd] bg-white shadow-sm"
          >
            <img src="/favicon.ico" alt="" class="size-8 object-contain" />
          </div>
          <div>
            <p class="font-bold tracking-tight text-[#1c1b1b]">E Leaning MindX</p>
            <p class="text-[11px] font-medium text-[#806f6e]">Learning Management System</p>
          </div>
        </div>

        <header class="mb-9 text-center lg:text-left">
          <p class="mb-3 text-xs font-bold uppercase tracking-[0.2em] text-[#a0001c]">
            Chào mừng trở lại
          </p>
          <h2 class="text-[34px] font-black leading-tight tracking-[-0.025em] text-[#1c1b1b]">
            Đăng nhập
          </h2>
          <p class="mt-2.5 text-[15px] leading-6 text-[#6b5d5c]">
            Sử dụng tài khoản do nhà trường cung cấp để tiếp tục.
          </p>
        </header>

        <div
          v-if="errorMessage"
          class="mb-6 flex items-start gap-3 rounded-xl border border-[#f0b9bb] bg-[#fff0f0] px-4 py-3.5"
          role="alert"
          aria-live="polite"
        >
          <div class="mt-0.5 grid size-7 shrink-0 place-items-center rounded-full bg-[#ba1a1a] text-white">
            <svg
              class="size-4"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              aria-hidden="true"
            >
              <circle cx="12" cy="12" r="9" />
              <path d="M12 7.5v5.5M12 16.5h.01" />
            </svg>
          </div>
          <div class="min-w-0">
            <p class="text-sm font-bold text-[#8f1018]">{{ errorTitle }}</p>
            <p class="mt-0.5 text-sm leading-5 text-[#8f3136]">{{ errorMessage }}</p>
          </div>
        </div>

        <form class="space-y-5" novalidate @submit.prevent="submit">
          <label class="block">
            <span class="mb-2 block text-sm font-bold text-[#342d2d]">Email</span>
            <span class="relative block">
              <span
                class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-4 text-[#806f6e]"
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
                  <path d="m5 7 7 6 7-6" />
                </svg>
              </span>
              <input
                v-model.trim="email"
                name="email"
                type="email"
                inputmode="email"
                autocomplete="username"
                required
                maxlength="256"
                placeholder="email@school.edu.vn"
                :aria-invalid="problem ? 'true' : 'false'"
                class="h-12 w-full rounded-xl border border-[#ded4d3] bg-white pl-12 pr-4 text-[15px] text-[#1c1b1b] shadow-[0_1px_2px_rgba(30,20,20,0.03)] outline-none placeholder:text-[#a69593] focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
              />
            </span>
          </label>

          <label class="block">
            <span class="mb-2 block text-sm font-bold text-[#342d2d]">Mật khẩu</span>
            <span class="relative block">
              <span
                class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-4 text-[#806f6e]"
              >
                <svg
                  class="size-5"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="1.8"
                  aria-hidden="true"
                >
                  <rect x="4" y="10" width="16" height="10" rx="3" />
                  <path d="M8 10V7a4 4 0 0 1 8 0v3" />
                </svg>
              </span>

              <input
                v-model="password"
                name="password"
                :type="showPassword ? 'text' : 'password'"
                autocomplete="current-password"
                required
                placeholder="Nhập mật khẩu"
                :aria-invalid="problem ? 'true' : 'false'"
                class="h-12 w-full rounded-xl border border-[#ded4d3] bg-white pl-12 pr-12 text-[15px] text-[#1c1b1b] shadow-[0_1px_2px_rgba(30,20,20,0.03)] outline-none placeholder:text-[#a69593] focus:border-[#c9152b] focus:ring-4 focus:ring-[#c9152b]/10"
              />

              <button
                type="button"
                class="absolute inset-y-0 right-0 grid w-12 place-items-center rounded-r-xl text-[#806f6e] hover:text-[#a0001c] focus:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[#c9152b]"
                :aria-label="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
                @click="togglePassword"
              >
                <svg
                  v-if="!showPassword"
                  class="size-5"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="1.8"
                  aria-hidden="true"
                >
                  <path d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6Z" />
                  <circle cx="12" cy="12" r="2.8" />
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
                  <path d="m3 3 18 18" />
                  <path d="M10.6 6.2A10 10 0 0 1 12 6c6 0 9.5 6 9.5 6a15 15 0 0 1-2.1 2.7M6.2 6.2C3.8 7.7 2.5 12 2.5 12s3.5 6 9.5 6a9 9 0 0 0 3.2-.6" />
                  <path d="M9.8 9.8a3 3 0 0 0 4.4 4.4" />
                </svg>
              </button>
            </span>
          </label>

          <label class="inline-flex cursor-pointer select-none items-center gap-2.5">
            <input
              v-model="rememberEmail"
              type="checkbox"
              class="size-4.5 rounded border-[#cbbdbc] text-[#c9152b] accent-[#c9152b] focus:ring-[#c9152b]/20"
            />
            <span class="text-sm font-medium text-[#6b5d5c]">Ghi nhớ email</span>
          </label>

          <button
            type="submit"
            :disabled="loading"
            class="group relative mt-1 flex h-12 w-full items-center justify-center overflow-hidden rounded-xl bg-[#c9152b] px-5 text-sm font-bold text-white shadow-[0_10px_24px_rgba(160,0,28,0.18)] transition hover:bg-[#a0001c] focus:outline-none focus-visible:ring-4 focus-visible:ring-[#c9152b]/20 disabled:cursor-not-allowed disabled:opacity-65"
          >
            <span v-if="loading" class="inline-flex items-center gap-2.5">
              <svg
                class="size-4.5 animate-spin"
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
              Đang đăng nhập…
            </span>
            <span v-else class="inline-flex items-center gap-2">
              Đăng nhập
              <svg
                class="size-4 transition-transform group-hover:translate-x-0.5"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                aria-hidden="true"
              >
                <path d="M5 12h14M14 7l5 5-5 5" />
              </svg>
            </span>
          </button>
        </form>

        <footer class="mt-8 border-t border-[#ebe4e3] pt-6 text-center">
          <p class="text-xs leading-5 text-[#887977]">
            Hệ thống quản lý học tập nội bộ.<br />
            Mọi vấn đề về tài khoản vui lòng liên hệ nhà trường.
          </p>
        </footer>
      </div>
    </section>
  </main>
</template>

<style scoped>
.academic-grid {
  background-image:
    linear-gradient(to right, rgb(255 255 255 / 0.045) 1px, transparent 1px),
    linear-gradient(to bottom, rgb(255 255 255 / 0.045) 1px, transparent 1px);
  background-size: 40px 40px;
}

.learning-node {
  width: 13px;
  height: 13px;
  border: 3px solid #7f1020;
  border-radius: 9999px;
  background: #f8f7f5;
  box-shadow: 0 0 0 1px rgb(255 255 255 / 0.28);
  transform: translate(-50%, -50%);
}

.learning-node-active {
  width: 18px;
  height: 18px;
  border: 3px solid #fff;
  background: #c9152b;
  box-shadow:
    0 0 0 6px rgb(255 255 255 / 0.08),
    0 0 24px rgb(201 21 43 / 0.5);
}

@media (prefers-reduced-motion: reduce) {
  .animate-spin {
    animation: none;
  }
}
</style>
