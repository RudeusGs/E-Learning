import { http } from '@/api/http'

import type { AdminQuestion } from '@/modules/exercises/models/AdminQuestion'
import type { QuestionWriteRequest } from '@/modules/exercises/models/QuestionWriteRequest'

import type { AdminLesson } from './models/AdminLesson'
import type { LessonWriteRequest } from './models/LessonWriteRequest'
import type { StudentLesson } from './models/StudentLesson'

export async function getAdminLessons(courseId: number): Promise<AdminLesson[]> {
  const response = await http.get<AdminLesson[]>(`/admin/courses/${courseId}/lessons`)
  return response.data
}

export async function createLesson(
  courseId: number,
  request: LessonWriteRequest,
): Promise<AdminLesson> {
  const response = await http.post<AdminLesson>(`/admin/courses/${courseId}/lessons`, request)
  return response.data
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

export async function getStudentLesson(lessonId: number): Promise<StudentLesson> {
  const response = await http.get<StudentLesson>(`/student/lessons/${lessonId}`)
  return response.data
}

export async function startLesson(lessonId: number): Promise<void> {
  await http.post(`/student/lessons/${lessonId}/start`)
}

export async function completeLesson(
  lessonId: number,
): Promise<{ courseProgress: { percentage: number } }> {
  const response = await http.post<{ courseProgress: { percentage: number } }>(
    `/student/lessons/${lessonId}/complete`,
  )
  return response.data
}
