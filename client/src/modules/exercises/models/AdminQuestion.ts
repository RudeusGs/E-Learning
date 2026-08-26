import type { QuestionType } from '@/types/QuestionType'

import type { AdminQuestionOption } from './AdminQuestionOption'

export interface AdminQuestion {
  id: number
  lessonId: number
  text: string
  type: QuestionType
  explanation: string | null
  sortOrder: number
  version: number
  options: AdminQuestionOption[]
}
