import type { AppointmentDto, BookAppointmentRequest } from '@/types/appointment'
import api from './api'

export const appointmentService = {
  async getAllAppointments (): Promise<any[]> {
    const response = await api.get('/Appointments/all')
    return response.data
  },

  async getPendingAppointments (): Promise<any[]> {
    const response = await api.get('/Appointments/pending')
    return response.data
  },

  async bookAppointment (data: BookAppointmentRequest): Promise<any> {
    const response = await api.post('/Appointments/book', data)
    return response.data
  },

  async approveAppointment (id: string): Promise<any> {
    const response = await api.put(`/Appointments/${id}/approve`)
    return response.data
  },

  async generateSlots (doctorId: string, date: string): Promise<any> {
    const response = await api.post('/Appointments/generate-slots', { doctorId, date })
    return response.data
  },

  async getAvailableSlots (doctorId: string, date: string): Promise<any[]> {
    const response = await api.get(`/Doctors/${doctorId}/slots?date=${date}`)
    return response.data
  },

  async cancelAppointment (id: string): Promise<any> {
    const response = await api.put(`/Appointments/${id}/cancel`)
    return response.data
  },

  async updateAppointment (id: string, data: Partial<AppointmentDto>): Promise<AppointmentDto> {
    const response = await api.put(`/Appointments/${id}`, data)
    return response.data
  },
}
