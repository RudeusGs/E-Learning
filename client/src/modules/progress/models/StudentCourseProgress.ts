import type { LessonProgressDetail } from './LessonProgressDetail'

export interface StudentCourseProgress {
  courseId: number
  completedLessons: number
  totalLessons: number
  percentage: number
  lessons: LessonProgressDetail[]
}
