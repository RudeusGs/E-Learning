<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { toProblem, type ProblemDetails } from '@/api/http'
import ProblemAlert from '@/components/ProblemAlert.vue'

import { useAuthStore } from './auth.store'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()
const email = ref('')
const password = ref('')
const loading = ref(false)
const problem = ref<ProblemDetails | null>(null)

async function submit(): Promise<void> {
  loading.value = true
  problem.value = null
  try {
    await auth.signIn(email.value, password.value)
    const fallback = auth.user?.role === 'ADMIN' ? '/admin' : '/learn'
    const redirect = getSafeRedirect(route.query.redirect, fallback)
    await router.replace(redirect)
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    loading.value = false
  }
}
function getSafeRedirect(value: unknown, fallback: string): string {
  return typeof value === 'string' && value.startsWith('/') && !value.startsWith('//')
    ? value
    : fallback
}
</script>

<template>
  <main class="grid min-h-screen place-items-center bg-slate-950 px-5">
    <form class="w-full max-w-md rounded-2xl bg-white p-8 shadow-2xl" @submit.prevent="submit">
      <p class="text-sm font-bold uppercase tracking-[0.2em] text-indigo-600">E-Learning</p>
      <h1 class="mt-3 text-3xl font-bold text-slate-950">Đăng nhập</h1>
      <p class="mt-2 text-sm text-slate-600">Dùng tài khoản do nhà trường cung cấp.</p>

      <div class="mt-7 space-y-4">
        <label class="block text-sm font-medium">
          Email
          <input
            v-model.trim="email"
            name="email"
            type="email"
            autocomplete="username"
            required
            class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2"
          />
        </label>
        <label class="block text-sm font-medium">
          Mật khẩu
          <input
            v-model="password"
            name="password"
            type="password"
            autocomplete="current-password"
            required
            class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2"
          />
        </label>
      </div>

      <ProblemAlert class="mt-5" :problem="problem" />
      <button
        class="mt-6 w-full rounded-lg bg-indigo-600 px-4 py-3 font-semibold text-white hover:bg-indigo-500 disabled:opacity-60"
        :disabled="loading"
        type="submit"
      >
        {{ loading ? 'Đang đăng nhập…' : 'Đăng nhập' }}
      </button>
    </form>
  </main>
</template>
