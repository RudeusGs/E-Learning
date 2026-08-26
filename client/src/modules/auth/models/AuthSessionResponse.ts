import type { SessionUser } from './SessionUser'

export interface AuthSessionResponse {
  user: SessionUser
  accessToken: string
  accessTokenExpiresAtUtc: string
}
