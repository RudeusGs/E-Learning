import type { QuestionPlacement } from '@/types/QuestionPlacement'
import type { QuestionType } from '@/types/QuestionType'

import type { StudentQuestionOption } from './StudentQuestionOption'

export interface StudentQuestion {
  id: number
  text: string
  type: QuestionType
  placement: QuestionPlacement
  videoTimestampSeconds: number | null
  passed: boolean
  options: StudentQuestionOption[]
}
