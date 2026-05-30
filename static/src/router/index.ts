import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'

const PORTAL_URL = import.meta.env.VITE_PORTAL_URL || `${window.location.protocol}//${window.location.hostname}:3000`

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      beforeEnter: () => {
        window.location.href = PORTAL_URL
        return false
      },
      component: () => import('@/views/PatientView.vue'),
    },
    {
      path: '/services',
      name: 'services',
      component: () => import('@/views/ServicesView.vue'),
    },
    {
      path: '/doctors',
      name: 'doctors',
      component: () => import('@/views/DoctorsView.vue'),
    },
    {
      path: '/guide',
      name: 'guide',
      component: () => import('@/views/GuideView.vue'),
    },
    {
      path: '/contact',
      name: 'contact',
      component: () => import('@/views/ContactView.vue'),
    },
    {
      path: '/track',
      name: 'track',
      component: () => import('@/views/TrackAppointmentView.vue'),
    },
    {
      path: '/patient',
      name: 'patient',
      component: () => import('@/views/PatientView.vue'),
      meta: { requiresAuth: true, roles: ['Patient'] },
    },
    {
      path: '/my-appointments',
      name: 'my-appointments',
      component: () => import('@/views/MyAppointmentsView.vue'),
      meta: { requiresAuth: true, roles: ['Patient'] },
    },
    {
      path: '/login',
      name: 'login',
      beforeEnter: () => {
        const redirect = `${window.location.origin}/auth-callback`
        window.location.href = `${PORTAL_URL}/login?redirect=${encodeURIComponent(redirect)}`
        return false
      },
      component: () => import('@/views/AuthCallbackView.vue'),
    },
    {
      path: '/admin',
      name: 'admin-login',
      beforeEnter: () => {
        window.location.href = `${PORTAL_URL}/login`
        return false
      },
      component: () => import('@/views/AuthCallbackView.vue'),
    },
    {
      path: '/register',
      name: 'register',
      beforeEnter: () => {
        window.location.href = `${PORTAL_URL}/register`
        return false
      },
      component: () => import('@/views/AuthCallbackView.vue'),
    },
    {
      path: '/auth-callback',
      name: 'auth-callback',
      component: () => import('@/views/AuthCallbackView.vue'),
    },
    {
      path: '/dashboard',
      name: 'dashboard',
      component: () => import('@/views/DashboardView.vue'),
      meta: { requiresAuth: true, roles: ['Admin', 'Receptionist'] },
    },
    {
      path: '/doctor',
      name: 'doctor',
      component: () => import('@/views/DoctorView.vue'),
      meta: { requiresAuth: true, roles: ['Doctor'] },
    },
    {
      path: '/receptionist',
      name: 'receptionist',
      component: () => import('@/views/ReceptionistView.vue'),
      meta: { requiresAuth: true, roles: ['Receptionist'] },
    },
  ],
})

router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()
  const requiresAuth = to.matched.some(record => record.meta.requiresAuth)
  const allowedRoles = to.meta.roles as string[]

  if (requiresAuth && !authStore.isAuthenticated) {
    const redirect = `${window.location.origin}/auth-callback`
    window.location.href = `${PORTAL_URL}/login?redirect=${encodeURIComponent(redirect)}`
  } else if (requiresAuth && allowedRoles && !allowedRoles.includes(authStore.user?.role || '')) {
    next('/')
  } else {
    next()
  }
})

export default router
