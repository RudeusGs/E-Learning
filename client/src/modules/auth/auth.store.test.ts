import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import * as authApi from './auth.api'
import { useAuthStore } from './auth.store'
import type { AuthSessionResponse } from './models/AuthSessionResponse'

vi.mock('./auth.api', () => ({
  login: vi.fn(),
  refresh: vi.fn(),
  logout: vi.fn(),
}))

const session: AuthSessionResponse = {
  user: {
    id: 7,
    fullName: 'Student Test',
    email: 'student@example.test',
    role: 'STUDENT',
  },
  accessToken: 'access-token-for-test',
  accessTokenExpiresAtUtc: '2026-08-25T13:00:00Z',
}

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('keeps access token in memory and signs in without browser storage', async () => {
    vi.mocked(authApi.login).mockResolvedValue(session)
    const store = useAuthStore()

    await store.signIn('student@example.test', 'correct-password')

    expect(store.user).toEqual(session.user)
    expect(store.initialized).toBe(true)
    expect(authApi.login).toHaveBeenCalledWith('student@example.test', 'correct-password')
  })

  it('restores a browser session through the HttpOnly refresh-cookie endpoint', async () => {
    vi.mocked(authApi.refresh).mockResolvedValue(session)
    const store = useAuthStore()

    await store.initialize()

    expect(authApi.refresh).toHaveBeenCalledTimes(1)
    expect(store.user).toEqual(session.user)
    expect(store.initialized).toBe(true)
  })

  it('passes only the in-memory access token to logout and then clears session state', async () => {
    vi.mocked(authApi.login).mockResolvedValue(session)
    vi.mocked(authApi.logout).mockResolvedValue()
    const store = useAuthStore()
    await store.signIn('student@example.test', 'correct-password')

    await store.signOut()

    expect(authApi.logout).toHaveBeenCalledWith(session.accessToken)
    expect(store.user).toBeNull()
    expect(store.initialized).toBe(true)
  })
})
