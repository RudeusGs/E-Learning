import type { QuestionPlacement } from '@/types/QuestionPlacement'
import type { QuestionType } from '@/types/QuestionType'

import type { QuestionOptionWriteRequest } from './QuestionOptionWriteRequest'

export interface QuestionWriteRequest {
  text: string
  type: QuestionType
  placement?: QuestionPlacement
  videoTimestampSeconds?: number | null
  explanation: string | null
  sortOrder: number
  options: QuestionOptionWriteRequest[]
  version?: number
}
