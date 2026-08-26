import { AxiosError } from 'axios'

import { authHttp } from '@/api/http'
import type { ProblemDetails } from '@/api/ProblemDetails'

import type { AuthSessionResponse } from './models/AuthSessionResponse'

export async function login(email: string, password: string): Promise<AuthSessionResponse> {
  const response = await authHttp.post<AuthSessionResponse>('/auth/login', { email, password })
  return response.data
}

export async function refresh(): Promise<AuthSessionResponse> {
  for (let attempt = 0; attempt < 2; attempt += 1) {
    try {
      const response = await authHttp.post<AuthSessionResponse>('/auth/refresh')
      return response.data
    } catch (error) {
      if (!isConcurrentRefreshRetry(error) || attempt === 1) {
        throw error
      }

      // Another tab/request rotated the shared HttpOnly cookie first.
      // Give the browser cookie jar a moment to observe Set-Cookie, then retry once.
      await new Promise((resolve) => globalThis.setTimeout(resolve, 100))
    }
  }

  throw new Error('Refresh retry exhausted')
}

export async function logout(accessToken: string | null): Promise<void> {
  await authHttp.post('/auth/logout', undefined, {
    headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : undefined,
  })
}

function isConcurrentRefreshRetry(error: unknown): boolean {
  if (!(error instanceof AxiosError) || error.response?.status !== 409) {
    return false
  }

  const problem = error.response.data as Partial<ProblemDetails> | undefined
  return problem?.code === 'REFRESH_RETRY_REQUIRED'
}
