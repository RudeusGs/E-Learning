import type { LessonCompletionState } from './LessonCompletionState'
import type { StudentQuestion } from './StudentQuestion'
import type { VideoProgressState } from './VideoProgressState'
import type { VideoReference } from './VideoReference'

export interface StudentLesson {
  id: number
  courseId: number
  title: string
  description: string | null
  contentHtml: string | null
  video: VideoReference | null
  progressStatus: string
  videoProgress: VideoProgressState
  completion: LessonCompletionState
  previousLessonId: number | null
  nextLessonId: number | null
  questions: StudentQuestion[]
}
