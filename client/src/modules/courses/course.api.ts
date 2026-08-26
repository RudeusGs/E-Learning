import { http } from '@/api/http'
import type { CursorPage } from '@/api/CursorPage'

import type { AdminCourse } from './models/AdminCourse'
import type { CourseWriteRequest } from './models/CourseWriteRequest'
import type { StudentCourse } from './models/StudentCourse'
import type { StudentCourseDetail } from './models/StudentCourseDetail'

export async function getAdminCourses(
  cursor?: string | null,
  search?: string,
): Promise<CursorPage<AdminCourse>> {
  const response = await http.get<CursorPage<AdminCourse>>('/admin/courses', {
    params: { limit: 20, cursor: cursor || undefined, search: search || undefined },
  })
  return response.data
}

export async function createCourse(request: CourseWriteRequest): Promise<AdminCourse> {
  const response = await http.post<AdminCourse>('/admin/courses', request)
  return response.data
}

export async function updateCourse(id: number, request: CourseWriteRequest): Promise<AdminCourse> {
  const response = await http.put<AdminCourse>(`/admin/courses/${id}`, request)
  return response.data
}

export async function archiveCourse(id: number): Promise<void> {
  await http.delete(`/admin/courses/${id}`)
}

export async function getStudentCourses(
  cursor?: string | null,
): Promise<CursorPage<StudentCourse>> {
  const response = await http.get<CursorPage<StudentCourse>>('/student/courses', {
    params: { limit: 20, cursor: cursor || undefined },
  })
  return response.data
}

export async function getStudentCourse(id: number): Promise<StudentCourseDetail> {
  const response = await http.get<StudentCourseDetail>(`/student/courses/${id}`)
  return response.data
}
