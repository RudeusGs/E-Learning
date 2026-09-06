export interface LessonProgressDetail {
  lessonId: number
  title: string
  status: string
  startedAtUtc: string | null
  completedAtUtc: string | null
}
