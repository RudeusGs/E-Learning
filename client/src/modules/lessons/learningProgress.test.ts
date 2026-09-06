import { describe, expect, it } from 'vitest'

import { hasPassedReinforcement } from './learningProgress'

describe('hasPassedReinforcement', () => {
  it('uses the exact ratio instead of the rounded display percentage', () => {
    expect(hasPassedReinforcement(33, 41, 80)).toBe(true)
  })

  it('requires strictly more than the configured percentage', () => {
    expect(hasPassedReinforcement(4, 5, 80)).toBe(false)
    expect(hasPassedReinforcement(5, 6, 80)).toBe(true)
  })

  it('treats an empty reinforcement set as satisfied', () => {
    expect(hasPassedReinforcement(0, 0, 80)).toBe(true)
  })
})
