import type { QuestionType } from '@/types/QuestionType'

import type { StudentQuestionOption } from './StudentQuestionOption'

export interface StudentQuestion {
  id: number
  text: string
  type: QuestionType
  options: StudentQuestionOption[]
}
