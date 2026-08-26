import { defineStore } from 'pinia'
import { ref } from 'vue'

import { setAccessTokenProvider, setTokenRefresher, setUnauthorizedHandler } from '@/api/http'

import * as authApi from './auth.api'
import type { AuthSessionResponse } from './models/AuthSessionResponse'
import type { SessionUser } from './models/SessionUser'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<SessionUser | null>(null)
  const initialized = ref(false)
  const accessToken = ref<string | null>(null)
  let initialization: Promise<void> | null = null

  function applySession(session: AuthSessionResponse): void {
    user.value = session.user
    accessToken.value = session.accessToken
  }

  function clearSession(): void {
    user.value = null
    accessToken.value = null
  }

  async function initialize(): Promise<void> {
    if (initialized.value) return

    if (!initialization) {
      initialization = authApi
        .refresh()
        .then(applySession)
        .catch(clearSession)
        .finally(() => {
          initialized.value = true
          initialization = null
        })
    }

    await initialization
  }

  async function signIn(email: string, password: string): Promise<void> {
    applySession(await authApi.login(email, password))
    initialized.value = true
  }

  async function signOut(): Promise<void> {
    const currentAccessToken = accessToken.value
    try {
      await authApi.logout(currentAccessToken)
    } finally {
      clearSession()
      initialized.value = true
    }
  }

  setAccessTokenProvider(() => accessToken.value)
  setUnauthorizedHandler(() => {
    const redirect = `${window.location.pathname}${window.location.search}${window.location.hash}`
    clearSession()
    if (window.location.pathname !== '/login') {
      window.location.replace(`/login?redirect=${encodeURIComponent(redirect)}`)
    }
  })
  setTokenRefresher(async () => {
    const session = await authApi.refresh()
    applySession(session)
    return session.accessToken
  })

  return { user, initialized, initialize, signIn, signOut, clearSession }
})
