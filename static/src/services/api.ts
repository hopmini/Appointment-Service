import axios from 'axios'

const gatewayApi = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  timeout: Number(import.meta.env.VITE_API_TIMEOUT),
  headers: {
    'Content-Type': 'application/json',
  },
})

gatewayApi.interceptors.request.use(
  config => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  error => Promise.reject(error),
)

gatewayApi.interceptors.response.use(
  response => response,
  error => {
    console.error('API Error:', error.response?.data || error.message)
    return Promise.reject(error)
  },
)

// Appointment Service API (qua Gateway: /api/appointment/...)
const api = {
  get: (path: string, config?: any) => gatewayApi.get(`/api/appointment${path}`, config),
  post: (path: string, data?: any, config?: any) => gatewayApi.post(`/api/appointment${path}`, data, config),
  put: (path: string, data?: any, config?: any) => gatewayApi.put(`/api/appointment${path}`, data, config),
  delete: (path: string, config?: any) => gatewayApi.delete(`/api/appointment${path}`, config),
}

// Public API (không cần JWT: /api/appointment/public/...)
export const publicApi = {
  get: (path: string, config?: any) => gatewayApi.get(`/api/appointment/public${path}`, config),
  post: (path: string, data?: any, config?: any) => gatewayApi.post(`/api/appointment/public${path}`, data, config),
}

// Pharmacy Service API (qua Gateway: /api/pharmacy/...)
export const pharmacyApi = {
  get: (path: string, config?: any) => gatewayApi.get(`/api/pharmacy${path}`, config),
  post: (path: string, data?: any, config?: any) => gatewayApi.post(`/api/pharmacy${path}`, data, config),
  put: (path: string, data?: any, config?: any) => gatewayApi.put(`/api/pharmacy${path}`, data, config),
  delete: (path: string, config?: any) => gatewayApi.delete(`/api/pharmacy${path}`, config),
}

// Medical Record Service API (qua Gateway: /api/medical/...)
export const medicalApi = {
  get: (path: string, config?: any) => gatewayApi.get(`/api/medical${path}`, config),
  post: (path: string, data?: any, config?: any) => gatewayApi.post(`/api/medical${path}`, data, config),
  put: (path: string, data?: any, config?: any) => gatewayApi.put(`/api/medical${path}`, data, config),
  delete: (path: string, config?: any) => gatewayApi.delete(`/api/medical${path}`, config),
}

export default api
