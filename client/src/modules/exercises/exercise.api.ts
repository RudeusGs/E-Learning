import { http } from '@/api/http'

import type { AnswerResult } from './models/AnswerResult'

export async function submitAnswer(questionId: number, optionId: number): Promise<AnswerResult> {
  const response = await http.post<AnswerResult>(`/student/questions/${questionId}/answer`, {
    optionId,
  })
  return response.data
}
