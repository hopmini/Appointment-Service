import { defineStore } from 'pinia'
import { publicApi } from '@/services/api'

interface User {
  id: number
  username: string
  fullName: string
  email?: string
  role: 'Patient' | 'Receptionist' | 'Doctor' | 'Admin'
}

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: JSON.parse(localStorage.getItem('user') || 'null') as User | null,
    token: localStorage.getItem('token') || null,
  }),

  getters: {
    isAuthenticated: state => !!state.token,
    isPatient: state => state.user?.role === 'Patient',
    isReceptionist: state => state.user?.role === 'Receptionist',
    isDoctor: state => state.user?.role === 'Doctor',
    isAdmin: state => state.user?.role === 'Admin',
    canAccessDashboard: state => state.user?.role === 'Admin' || state.user?.role === 'Receptionist',
    canAccessDoctorDashboard: state => state.user?.role === 'Doctor',
  },

  actions: {
    async login (credentials: any) {
      try {
        const response = await publicApi.post('/auth/login', credentials)
        const { token, user } = response.data

        this.token = token
        this.user = user

        localStorage.setItem('token', token)
        localStorage.setItem('user', JSON.stringify(user))

        return true
      } catch (error) {
        console.error('Login failed:', error)
        throw error
      }
    },

    async register (userData: any) {
      try {
        await publicApi.post('/auth/register', userData)
        return true
      } catch (error) {
        console.error('Registration failed:', error)
        throw error
      }
    },

    logout () {
      this.token = null
      this.user = null
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      const portalUrl = import.meta.env.VITE_PORTAL_URL || `${window.location.protocol}//${window.location.hostname}:3000`
      window.location.href = `${portalUrl}/login?logout=true`
    },
  },
})
