import type { CourseStatus } from '@/types/CourseStatus'

export interface CourseWriteRequest {
  title: string
  description: string | null
  thumbnailUrl: string | null
  status: CourseStatus
  sortOrder: number
  version?: number
}
