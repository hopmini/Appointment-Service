import { defineStore } from 'pinia'
import api from '@/services/api'

export const useDoctorStore = defineStore('doctor', {
  state: () => ({
    doctors: [] as any[],
    loading: false,
  }),
  actions: {
    async fetchDoctors () {
      this.loading = true
      try {
        const response = await api.get('/Doctors')
        this.doctors = response.data
        console.log('Hút data thành công m ơi:', this.doctors)
      } catch (error) {
        console.error('Lỗi méo hút được data:', error)
      } finally {
        this.loading = false
      }
    },
  },
})
