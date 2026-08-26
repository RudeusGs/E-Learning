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
        return auth.user?.role === 'ADMIN' ? '/admin' : '/learn'
      },
    },
    {
      path: '/admin',
      component: () => import('@/layouts/AppLayout.vue'),
      meta: { requiresAuth: true, role: 'ADMIN' },
      children: [
        {
          path: '',
          redirect: '/admin/courses',
        },
        {
          path: 'courses',
          name: 'admin-courses',
          component: () => import('@/modules/courses/AdminCoursesView.vue'),
        },
        {
          path: 'courses/:id',
          name: 'admin-course-detail',
          component: () => import('@/modules/courses/AdminCourseDetailView.vue'),
        },
      ],
    },
    {
      path: '/student',
      redirect: '/learn',
    },
    {
      path: '/learn',
      component: () => import('@/layouts/AppLayout.vue'),
      meta: { requiresAuth: true, role: 'STUDENT' },
      children: [
        {
          path: '',
          name: 'student-courses',
          component: () => import('@/modules/courses/StudentCoursesView.vue'),
        },
        {
          path: ':courseId',
          name: 'student-course-detail',
          component: () => import('@/modules/courses/StudentCourseDetailView.vue'),
        },
        {
          path: 'lesson/:lessonId',
          name: 'student-lesson',
          component: () => import('@/modules/lessons/StudentLessonView.vue'),
        },
      ],
    },
    {
      path: '/:pathMatch(.*)*',
      redirect: '/',
    },
  ],
})

router.beforeEach(async (to, from, next) => {
  const auth = useAuthStore()
  await auth.initialize()

  if (to.meta.guestOnly && auth.user) {
    return next(auth.user.role === 'ADMIN' ? '/admin' : '/learn')
  }

  if (to.meta.requiresAuth) {
    if (!auth.user) {
      return next({ path: '/login', query: { redirect: to.fullPath } })
    }
    if (to.meta.role && auth.user.role !== to.meta.role) {
      return next(auth.user.role === 'ADMIN' ? '/admin' : '/learn')
    }
  }

  next()
})

export default router
