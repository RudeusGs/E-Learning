import {
  AxiosError,
  AxiosHeaders,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
} from 'axios'
import { afterEach, describe, expect, it, vi } from 'vitest'

import { authHttp } from '@/api/http'

import { refresh } from './auth.api'
import type { AuthSessionResponse } from './models/AuthSessionResponse'

const session: AuthSessionResponse = {
  user: {
    id: 9,
    fullName: 'Concurrent Student',
    email: 'concurrent@example.test',
    role: 'STUDENT',
  },
  accessToken: 'rotated-access-token',
  accessTokenExpiresAtUtc: '2026-08-25T13:10:00Z',
}

describe('auth api refresh', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    vi.useRealTimers()
  })

  it('retries once when another tab rotated the shared refresh cookie first', async () => {
    vi.useFakeTimers()
    const post = vi
      .spyOn(authHttp, 'post')
      .mockRejectedValueOnce(createConcurrentRefreshConflict())
      .mockResolvedValueOnce({ data: session } as AxiosResponse<AuthSessionResponse>)

    const resultPromise = refresh()
    await vi.advanceTimersByTimeAsync(100)

    await expect(resultPromise).resolves.toEqual(session)
    expect(post).toHaveBeenCalledTimes(2)
  })

  it('does not retry ordinary unauthorized refresh failures', async () => {
    const error = createHttpError(401, { code: 'INVALID_REFRESH_TOKEN' })
    const post = vi.spyOn(authHttp, 'post').mockRejectedValue(error)

    await expect(refresh()).rejects.toBe(error)
    expect(post).toHaveBeenCalledTimes(1)
  })
})

function createConcurrentRefreshConflict(): AxiosError {
  return createHttpError(409, { code: 'REFRESH_RETRY_REQUIRED' })
}

function createHttpError(status: number, data: unknown): AxiosError {
  const config = { headers: new AxiosHeaders() } as InternalAxiosRequestConfig
  const response: AxiosResponse = {
    data,
    status,
    statusText: status === 409 ? 'Conflict' : 'Unauthorized',
    headers: new AxiosHeaders(),
    config,
  }
  return new AxiosError('Request failed', 'ERR_BAD_REQUEST', config, undefined, response)
}
