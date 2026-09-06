import type { CursorPage } from '@/api/CursorPage'
import { http } from '@/api/http'
import type { AccountStatus } from '@/types/AccountStatus'

import type { EnrollmentResponse } from './models/EnrollmentResponse'
import type { StudentCreateRequest } from './models/StudentCreateRequest'
import type { StudentDetail } from './models/StudentDetail'
import type { StudentListItem } from './models/StudentListItem'

export async function getStudents(
  cursor?: string | null,
  search?: string,
  signal?: AbortSignal,
  limit = 20,
  status?: AccountStatus | null,
): Promise<CursorPage<StudentListItem>> {
  const response = await http.get<CursorPage<StudentListItem>>('/admin/students', {
    params: {
      limit,
      cursor: cursor || undefined,
      search: search || undefined,
      status: status || undefined,
    },
    signal,
  })
  return response.data
}

export async function getStudent(id: number): Promise<StudentDetail> {
  const response = await http.get<StudentDetail>(`/admin/students/${id}`)
  return response.data
}

export async function createStudent(request: StudentCreateRequest): Promise<StudentDetail> {
  const response = await http.post<StudentDetail>('/admin/students', request)
  return response.data
}

export async function updateStudent(id: number, fullName: string): Promise<StudentDetail> {
  const response = await http.put<StudentDetail>(`/admin/students/${id}`, { fullName })
  return response.data
}

export async function disableStudent(id: number): Promise<void> {
  await http.post(`/admin/students/${id}/disable`)
}

export async function enrollStudent(
  studentId: number,
  courseId: number,
): Promise<EnrollmentResponse> {
  const response = await http.post<EnrollmentResponse>('/admin/enrollments', {
    studentId,
    courseId,
  })
  return response.data
}
