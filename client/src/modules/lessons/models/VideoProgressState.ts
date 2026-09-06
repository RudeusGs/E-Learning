export interface VideoProgressState {
  maxPositionSeconds: number
  durationSeconds: number | null
  completed: boolean
  blockedByQuestionId: number | null
}
