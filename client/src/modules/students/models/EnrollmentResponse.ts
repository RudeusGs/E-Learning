export interface EnrollmentResponse {
  id: number
  studentId: number
  courseId: number
  status: 'ACTIVE' | 'INACTIVE'
}
