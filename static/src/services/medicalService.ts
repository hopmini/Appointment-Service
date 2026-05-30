import api from './api'

export const medicalServiceApi = {
  async getAllServices () {
    const response = await api.get('/MedicalServices')
    return response.data
  },

  async createService (data: any) {
    const response = await api.post('/MedicalServices', data)
    return response.data
  },

  async updateService (id: string, data: any) {
    const response = await api.put(`/MedicalServices/${id}`, data)
    return response.data
  },

  async deleteService (id: string) {
    const response = await api.delete(`/MedicalServices/${id}`)
    return response.data
  },
}
