import api from './api'

export const userService = {
  async getPatients () {
    const response = await api.get('/Users/patients')
    return response.data
  },
  async getDoctors () {
    const response = await api.get('/Users/doctors')
    return response.data
  },
  async getAllUsers () {
    const response = await api.get('/Users/all')
    return response.data
  },
}
