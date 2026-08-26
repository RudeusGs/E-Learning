import type { CourseStatus } from '@/types/CourseStatus'

export interface AdminCourse {
  id: number
  title: string
  description: string | null
  thumbnailUrl: string | null
  status: CourseStatus
  sortOrder: number
  lessonCount: number
  studentCount: number
  version: number
}
