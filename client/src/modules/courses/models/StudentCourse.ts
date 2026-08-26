export interface StudentCourse {
  id: number
  title: string
  description: string | null
  thumbnailUrl: string | null
  totalLessons: number
  completedLessons: number
  percentage: number
}
