import type { QuestionType } from '@/types/QuestionType'

import type { QuestionOptionWriteRequest } from './QuestionOptionWriteRequest'

export interface QuestionWriteRequest {
  text: string
  type: QuestionType
  explanation: string | null
  sortOrder: number
  options: QuestionOptionWriteRequest[]
  version?: number
}
