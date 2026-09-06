export function hasPassedReinforcement(
  passed: number,
  total: number,
  requiredScorePercent: number,
): boolean {
  if (total === 0) return true
  if (total < 0 || passed < 0 || passed > total) return false
  return passed * 100 > total * requiredScorePercent
}
