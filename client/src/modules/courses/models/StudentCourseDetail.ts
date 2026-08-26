import type { StudentLessonSummary } from './StudentLessonSummary'

export interface StudentCourseDetail {
  id: number
  title: string
  description: string | null
  completedLessons: number
  totalLessons: number
  percentage: number
  lessons: StudentLessonSummary[]
}
