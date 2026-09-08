import { http } from '@/api/http'

import type { AdminQuestion } from '@/modules/exercises/models/AdminQuestion'
import type { QuestionWriteRequest } from '@/modules/exercises/models/QuestionWriteRequest'

import type { AdminLesson } from './models/AdminLesson'
import type { AdminLessonListItem } from './models/AdminLessonListItem'
import type { LessonWriteRequest } from './models/LessonWriteRequest'
import type { StudentLesson } from './models/StudentLesson'
import type { VideoProgressState } from './models/VideoProgressState'

export async function getAdminLessons(courseId: number): Promise<AdminLessonListItem[]> {
  const response = await http.get<AdminLessonListItem[]>(`/admin/courses/${courseId}/lessons`)
  return response.data
}

export async function createLesson(
  courseId: number,
  request: LessonWriteRequest,
): Promise<AdminLesson> {
  const response = await http.post<AdminLesson>(`/admin/courses/${courseId}/lessons`, request)
  return response.data
}

export async function getAdminLesson(id: number): Promise<AdminLesson> {
  const response = await http.get<AdminLesson>(`/admin/lessons/${id}`)
  return response.data
}

export async function updateLesson(id: number, request: LessonWriteRequest): Promise<AdminLesson> {
  const response = await http.put<AdminLesson>(`/admin/lessons/${id}`, request)
  return response.data
}

export async function archiveLesson(id: number): Promise<void> {
  await http.delete(`/admin/lessons/${id}`)
}

export async function getQuestions(lessonId: number): Promise<AdminQuestion[]> {
  const response = await http.get<AdminQuestion[]>(`/admin/lessons/${lessonId}/questions`)
  return response.data
}

export async function createQuestion(
  lessonId: number,
  request: QuestionWriteRequest,
): Promise<AdminQuestion> {
  const response = await http.post<AdminQuestion>(`/admin/lessons/${lessonId}/questions`, request)
  return response.data
}

export async function updateQuestion(
  id: number,
  request: QuestionWriteRequest,
): Promise<AdminQuestion> {
  const response = await http.put<AdminQuestion>(`/admin/questions/${id}`, request)
  return response.data
}

export async function deleteQuestion(id: number): Promise<void> {
  await http.delete(`/admin/questions/${id}`)
}

export async function getStudentLesson(lessonId: number): Promise<StudentLesson> {
  const response = await http.get<StudentLesson>(`/student/lessons/${lessonId}`)
  return response.data
}

export async function startLesson(lessonId: number): Promise<{ status: string }> {
  const response = await http.post<{ status: string }>(`/student/lessons/${lessonId}/start`)
  return response.data
}

export async function recordVideoHeartbeat(
  lessonId: number,
  positionSeconds: number,
): Promise<VideoProgressState> {
  const response = await http.post<VideoProgressState>(
    `/student/lessons/${lessonId}/video-heartbeat`,
    { positionSeconds },
  )
  return response.data
}

export async function completeLesson(
  lessonId: number,
): Promise<{ courseProgress: { percentage: number }; nextLessonId: number | null }> {
  const response = await http.post<{
    courseProgress: { percentage: number }
    nextLessonId: number | null
  }>(`/student/lessons/${lessonId}/complete`)
  return response.data
}
