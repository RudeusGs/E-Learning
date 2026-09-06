import type { CourseStatus } from '@/types/CourseStatus'

export interface AdminCourseListItem {
  id: number
  title: string
  status: CourseStatus
  sortOrder: number
  lessonCount: number
  studentCount: number
  version: number
}
