import type { LessonStatus } from '@/types/LessonStatus'

import type { VideoReference } from './VideoReference'

export interface AdminLesson {
  id: number
  courseId: number
  title: string
  description: string | null
  contentHtml: string | null
  video: VideoReference | null
  sortOrder: number
  status: LessonStatus
  version: number
}
