import type { QuestionPlacement } from '@/types/QuestionPlacement'
import type { QuestionType } from '@/types/QuestionType'

import type { AdminQuestionOption } from './AdminQuestionOption'

export interface AdminQuestion {
  id: number
  lessonId: number
  text: string
  type: QuestionType
  placement: QuestionPlacement
  videoTimestampSeconds: number | null
  explanation: string | null
  sortOrder: number
  version: number
  options: AdminQuestionOption[]
}
