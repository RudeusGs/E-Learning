import type { CursorPage } from '@/api/CursorPage'
import { http } from '@/api/http'

import type { ProgressRow } from './models/ProgressRow'
import type { StudentCourseProgress } from './models/StudentCourseProgress'

export async function getAdminProgress(
  cursor?: string | null,
  search?: string,
  studentId?: number,
  courseId?: number,
  signal?: AbortSignal,
  limit = 20,
): Promise<CursorPage<ProgressRow>> {
  const response = await http.get<CursorPage<ProgressRow>>('/admin/progress', {
    params: {
      limit,
      cursor: cursor || undefined,
      search: search || undefined,
      studentId,
      courseId,
    },
    signal,
  })
  return response.data
}

export async function getStudentProgress(courseId: number): Promise<StudentCourseProgress> {
  const response = await http.get<StudentCourseProgress>(`/student/courses/${courseId}/progress`)
  return response.data
}
