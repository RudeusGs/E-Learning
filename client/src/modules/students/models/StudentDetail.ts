import type { AccountStatus } from '@/types/AccountStatus'

import type { StudentEnrollment } from './StudentEnrollment'

export interface StudentDetail {
  id: number
  fullName: string
  email: string
  status: AccountStatus
  enrollments: StudentEnrollment[]
}
