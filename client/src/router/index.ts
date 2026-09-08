import { createRouter, createWebHistory } from 'vue-router'

import { useAuthStore } from '@/modules/auth/auth.store'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/modules/auth/LoginView.vue'),
      meta: { requiresAuth: false, guestOnly: true },
    },
    {
      path: '/',
      redirect: () => {
        const auth = useAuthStore()
        return auth.user?.role === 'ADMIN' ? '/admin/dashboard' : '/student/courses'
      },
    },
    {
      path: '/admin',
      component: () => import('@/layouts/AppLayout.vue'),
      meta: { requiresAuth: true, role: 'ADMIN' },
      children: [
        { path: '', redirect: '/admin/dashboard' },
        {
          path: 'dashboard',
          name: 'admin-dashboard',
          component: () => import('@/modules/dashboard/AdminDashboardView.vue'),
        },
        {
          path: 'courses',
          name: 'admin-courses',
          component: () => import('@/modules/courses/AdminCoursesView.vue'),
        },
        {
          path: 'courses/create',
          name: 'admin-course-create',
          component: () => import('@/modules/courses/AdminCourseCreateView.vue'),
        },
        {
          path: 'courses/:courseId',
          name: 'admin-course-detail',
          component: () => import('@/modules/courses/AdminCourseDetailView.vue'),
        },
        {
          path: 'courses/:courseId/edit',
          name: 'admin-course-edit',
          component: () => import('@/modules/courses/AdminCourseEditView.vue'),
        },
        {
          path: 'courses/:courseId/lessons/create',
          name: 'admin-lesson-create',
          component: () => import('@/modules/lessons/AdminLessonCreateView.vue'),
        },
        {
          path: 'lessons/:lessonId/edit',
          name: 'admin-lesson-edit',
          component: () => import('@/modules/lessons/AdminLessonEditView.vue'),
        },
        {
          path: 'lessons/:lessonId/exercises',
          name: 'admin-lesson-exercises',
          component: () => import('@/modules/exercises/AdminQuestionBuilderView.vue'),
        },
        {
          path: 'students',
          name: 'admin-students',
          component: () => import('@/modules/students/AdminStudentsView.vue'),
        },
        {
          path: 'students/:studentId',
          name: 'admin-student-detail',
          component: () => import('@/modules/students/AdminStudentDetailView.vue'),
        },
        {
          path: 'progress',
          name: 'admin-progress',
          component: () => import('@/modules/progress/AdminProgressView.vue'),
        },
      ],
    },
    {
      path: '/student',
      component: () => import('@/layouts/AppLayout.vue'),
      meta: { requiresAuth: true, role: 'STUDENT' },
      children: [
        { path: '', redirect: '/student/courses' },
        {
          path: 'courses',
          name: 'student-courses',
          component: () => import('@/modules/courses/StudentCoursesView.vue'),
        },
        {
          path: 'courses/:courseId',
          name: 'student-course-detail',
          component: () => import('@/modules/courses/StudentCourseDetailView.vue'),
        },
        {
          path: 'courses/:courseId/progress',
          name: 'student-progress',
          component: () => import('@/modules/progress/StudentProgressView.vue'),
        },
        {
          path: 'learn/:lessonId',
          name: 'student-lesson',
          component: () => import('@/modules/lessons/StudentLessonView.vue'),
        },
        {
          path: 'profile',
          name: 'student-profile',
          component: () => import('@/modules/students/StudentProfileView.vue'),
        },
      ],
    },
    { path: '/learn', redirect: '/student/courses' },
    { path: '/learn/courses', redirect: '/student/courses' },
    {
      path: '/learn/courses/:courseId',
      redirect: (to) => ({ name: 'student-course-detail', params: to.params }),
    },
    {
      path: '/learn/lessons/:lessonId',
      redirect: (to) => ({ name: 'student-lesson', params: to.params }),
    },
    { path: '/:pathMatch(.*)*', redirect: '/' },
  ],
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  await auth.initialize()

  if (to.meta.guestOnly && auth.user) {
    return auth.user.role === 'ADMIN' ? '/admin/dashboard' : '/student/courses'
  }

  if (to.meta.requiresAuth) {
    if (!auth.user) return { path: '/login', query: { redirect: to.fullPath } }
    if (to.meta.role && auth.user.role !== to.meta.role) {
      return auth.user.role === 'ADMIN' ? '/admin/dashboard' : '/student/courses'
    }
  }

  return true
})

export default router
