<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, RouterView, useRouter } from 'vue-router'

import { useAuthStore } from '@/modules/auth/auth.store'

const auth = useAuthStore()
const router = useRouter()
const links = computed(() =>
  auth.user?.role === 'ADMIN'
    ? [
        { to: '/admin', label: 'Tổng quan' },
        { to: '/admin/courses', label: 'Khóa học' },
        { to: '/admin/students', label: 'Học viên' },
        { to: '/admin/progress', label: 'Tiến độ' },
      ]
    : [{ to: '/learn', label: 'Khóa học của tôi' }],
)

async function signOut(): Promise<void> {
  await auth.signOut()
  await router.replace('/login')
}
</script>

<template>
  <div class="min-h-screen bg-slate-50 text-slate-900">
    <header class="border-b border-slate-200 bg-white">
      <div class="mx-auto flex max-w-7xl items-center gap-6 px-5 py-4">
        <RouterLink
          class="text-lg font-black text-indigo-700"
          :to="auth.user?.role === 'ADMIN' ? '/admin' : '/learn'"
        >
          E-Learning
        </RouterLink>
        <nav class="flex flex-1 gap-4 text-sm font-semibold">
          <RouterLink
            v-for="link in links"
            :key="link.to"
            :to="link.to"
            class="hover:text-indigo-700"
          >
            {{ link.label }}
          </RouterLink>
        </nav>
        <div class="text-right text-sm">
          <p class="font-semibold">{{ auth.user?.fullName }}</p>
          <p class="text-xs text-slate-500">{{ auth.user?.role }}</p>
        </div>
        <button type="button" class="rounded-lg border px-3 py-2 text-sm" @click="signOut">
          Đăng xuất
        </button>
      </div>
    </header>
    <main class="mx-auto max-w-7xl px-5 py-8">
      <RouterView />
    </main>
  </div>
</template>
