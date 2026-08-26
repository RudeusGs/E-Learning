export interface ProblemDetails {
  title: string
  status: number
  detail?: string
  code: string
  traceId?: string
  errors?: Record<string, string[]>
}
