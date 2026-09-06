export interface StudentEnrollment {
  id: number
  courseId: number
  courseTitle: string
  status: 'ACTIVE' | 'INACTIVE'
}
