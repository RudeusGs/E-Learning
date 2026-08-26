import type { UserRole } from '@/types/UserRole'

export interface SessionUser {
  id: number
  fullName: string
  email: string
  role: UserRole
}
