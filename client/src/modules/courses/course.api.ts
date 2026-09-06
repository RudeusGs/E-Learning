import type { CursorPage } from '@/api/CursorPage'
import { http } from '@/api/http'
import type { CourseStatus } from '@/types/CourseStatus'

import type { AdminCourseDetail } from './models/AdminCourseDetail'
import type { AdminCourseListItem } from './models/AdminCourseListItem'
import type { CourseWriteRequest } from './models/CourseWriteRequest'
import type { StudentCourse } from './models/StudentCourse'
import type { StudentCourseDetail } from './models/StudentCourseDetail'
import type { StudentCourseProgressFilter } from './models/StudentCourseProgressFilter'

export async function getAdminCourses(
  cursor?: string | null,
  search?: string,
  signal?: AbortSignal,
  limit = 20,
  status?: CourseStatus | null,
  sort?: 'sortOrder' | '-sortOrder',
): Promise<CursorPage<AdminCourseListItem>> {
  const response = await http.get<CursorPage<AdminCourseListItem>>('/admin/courses', {
    params: {
      limit,
      cursor: cursor || undefined,
      search: search || undefined,
      status: status || undefined,
      sort: sort || undefined,
    },
    signal,
  })

  return response.data
}

export async function getAdminCourse(id: number): Promise<AdminCourseDetail> {
  const response = await http.get<AdminCourseDetail>(`/admin/courses/${id}`)
  return response.data
}

export async function createCourse(request: CourseWriteRequest): Promise<AdminCourseDetail> {
  const response = await http.post<AdminCourseDetail>('/admin/courses', request)
  return response.data
}

export async function updateCourse(
  id: number,
  request: CourseWriteRequest,
): Promise<AdminCourseDetail> {
  const response = await http.put<AdminCourseDetail>(`/admin/courses/${id}`, request)
  return response.data
}

export async function archiveCourse(id: number): Promise<void> {
  await http.delete(`/admin/courses/${id}`)
}

export async function getStudentCourses(
  cursor?: string | null,
  progress?: StudentCourseProgressFilter | null,
  signal?: AbortSignal,
  limit = 20,
): Promise<CursorPage<StudentCourse>> {
  const response = await http.get<CursorPage<StudentCourse>>('/student/courses', {
    params: {
      limit,
      cursor: cursor || undefined,
      progress: progress || undefined,
    },
    signal,
  })

  return response.data
}

export async function getStudentCourse(id: number): Promise<StudentCourseDetail> {
  const response = await http.get<StudentCourseDetail>(`/student/courses/${id}`)
  return response.data
}