import type { DoctorDto } from '@/types/doctor'
import api from './api'

export const doctorService = {
  // Get all doctors
  async getAllDoctors (): Promise<DoctorDto[]> {
    const response = await api.get('/Doctors')
    return response.data
  },

  // Get doctor by ID
  async getDoctorById (id: string): Promise<DoctorDto> {
    const response = await api.get(`/Doctors/${id}`)
    return response.data
  },

  // Get doctor's available slots
  async getDoctorSlots (doctorId: string, date?: string): Promise<any[]> {
    const params = date ? { date } : {}
    const response = await api.get(`/Doctors/${doctorId}/slots`, { params })
    return response.data
  },

  // Create new doctor
  async createDoctor (doctorData: Partial<DoctorDto>): Promise<DoctorDto> {
    const response = await api.post('/Doctors', doctorData)
    return response.data
  },

  // Update doctor
  async updateDoctor (id: string, doctorData: Partial<DoctorDto>): Promise<DoctorDto> {
    const response = await api.put(`/Doctors/${id}`, doctorData)
    return response.data
  },

  // Delete doctor
  async deleteDoctor (id: string): Promise<void> {
    await api.delete(`/Doctors/${id}`)
  },
}
