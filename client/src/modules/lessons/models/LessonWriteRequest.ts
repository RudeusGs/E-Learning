import type { LessonStatus } from '@/types/LessonStatus'

export interface LessonWriteRequest {
  title: string
  description: string | null
  contentHtml: string | null
  videoUrl: string | null
  videoDurationSeconds?: number | null
  sortOrder: number
  status: LessonStatus
  version?: number
}
