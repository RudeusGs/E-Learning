import type { AccountStatus } from '@/types/AccountStatus'

export interface StudentListItem {
  id: number
  fullName: string
  email: string
  courseCount: number
  status: AccountStatus
}
