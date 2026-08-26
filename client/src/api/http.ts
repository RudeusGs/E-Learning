import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'
import type { ProblemDetails } from './ProblemDetails'

export type { ProblemDetails } from './ProblemDetails'

type UnauthorizedHandler = () => void
type TokenRefresher = () => Promise<string>
type AccessTokenProvider = () => string | null

type RetryableRequestConfig = InternalAxiosRequestConfig & { _retry?: boolean }

let unauthorizedHandler: UnauthorizedHandler | null = null
let tokenRefresher: TokenRefresher | null = null
let accessTokenProvider: AccessTokenProvider | null = null
let refreshPromise: Promise<string> | null = null

const clientConfig = {
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  timeout: 15_000,
  withCredentials: true,
}

export const http = axios.create(clientConfig)

// Authentication lifecycle calls intentionally bypass the normal 401 interceptor.
// Otherwise a failed /auth/refresh request can recursively try to refresh itself.
export const authHttp = axios.create(clientConfig)

export function setUnauthorizedHandler(handler: UnauthorizedHandler): void {
  unauthorizedHandler = handler
}

export function setTokenRefresher(refresher: TokenRefresher): void {
  tokenRefresher = refresher
}

export function setAccessTokenProvider(provider: AccessTokenProvider): void {
  accessTokenProvider = provider
}

export function toProblem(error: unknown): ProblemDetails {
  if (error instanceof AxiosError && error.response?.data) {
    const data = error.response.data as Partial<ProblemDetails>
    return {
      title: data.title ?? 'Request failed',
      status: data.status ?? error.response.status,
      detail: data.detail,
      code: data.code ?? 'HTTP_ERROR',
      traceId: data.traceId,
      errors: data.errors,
    }
  }

  return {
    title: error instanceof Error ? error.message : 'Unexpected error',
    status: 0,
    code: 'NETWORK_ERROR',
  }
}

http.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = accessTokenProvider?.()
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`)
  }
  return config
})

http.interceptors.response.use(
  (response) => response,
  async (error: unknown) => {
    if (!(error instanceof AxiosError) || error.response?.status !== 401 || !error.config) {
      return Promise.reject(error)
    }

    const originalRequest = error.config as RetryableRequestConfig
    if (originalRequest._retry) {
      unauthorizedHandler?.()
      return Promise.reject(error)
    }

    originalRequest._retry = true

    try {
      if (!tokenRefresher) {
        throw new Error('Token refresher not configured')
      }

      if (!refreshPromise) {
        refreshPromise = tokenRefresher().finally(() => {
          refreshPromise = null
        })
      }

      const newToken = await refreshPromise
      originalRequest.headers.set('Authorization', `Bearer ${newToken}`)
      return http(originalRequest)
    } catch (refreshError) {
      unauthorizedHandler?.()
      return Promise.reject(refreshError)
    }
  },
)
