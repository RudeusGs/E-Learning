import type { LessonStatus } from '@/types/LessonStatus'

export interface AdminLessonListItem {
  id: number
  courseId: number
  title: string
  description: string | null
  sortOrder: number
  status: LessonStatus
  version: number
}
