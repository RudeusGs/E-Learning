export interface LessonCompletionState {
  videoRequired: boolean
  videoConfigured: boolean
  videoCompleted: boolean
  reinforcementUnlocked: boolean
  checkpointTotal: number
  checkpointPassed: number
  reinforcementTotal: number
  reinforcementPassed: number
  reinforcementScorePercent: number
  requiredScorePercent: number
  canComplete: boolean
}
