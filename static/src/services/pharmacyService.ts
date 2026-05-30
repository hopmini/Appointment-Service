import { pharmacyApi } from './api'

export const pharmacyService = {
  // Medicines
  async getAllMedicines () {
    const res = await pharmacyApi.get('/medicines')
    return res.data
  },

  async getMedicineById (id: number) {
    const res = await pharmacyApi.get(`/medicines/${id}`)
    return res.data
  },

  async createMedicine (data: any) {
    const res = await pharmacyApi.post('/medicines', data)
    return res.data
  },

  async updateMedicine (id: number, data: any) {
    const res = await pharmacyApi.put(`/medicines/${id}`, data)
    return res.data
  },

  async deleteMedicine (id: number) {
    const res = await pharmacyApi.delete(`/medicines/${id}`)
    return res.data
  },

  // Bills
  async getAllBills () {
    const res = await pharmacyApi.get('/bills')
    return res.data
  },

  async getBillById (id: number) {
    const res = await pharmacyApi.get(`/bills/${id}`)
    return res.data
  },

  async createBill (data: any) {
    const res = await pharmacyApi.post('/bills', data)
    return res.data
  },

  async payBill (data: any) {
    const res = await pharmacyApi.post('/bills/pay', data)
    return res.data
  },

  async createBillFromPrescription (code: string) {
    const res = await pharmacyApi.post(`/bills/from-prescription/${code}`)
    return res.data
  },

  // Inventory
  async getInventoryLogs () {
    const res = await pharmacyApi.get('/inventory/logs')
    return res.data
  },

  async importMedicine (data: any) {
    const res = await pharmacyApi.post('/inventory/import', data)
    return res.data
  },

  async getLowStockAlerts (threshold = 100) {
    const res = await pharmacyApi.get(`/inventory/alerts/low-stock?threshold=${threshold}`)
    return res.data
  },

  async getExpiringSoonAlerts (months = 3) {
    const res = await pharmacyApi.get(`/inventory/alerts/expiring-soon?months=${months}`)
    return res.data
  },

  // Prescriptions
  async getPrescriptions () {
    const res = await pharmacyApi.get('/pharmacy/prescriptions')
    return res.data
  },

  async getPrescription (code: string) {
    const res = await pharmacyApi.get(`/pharmacy/prescriptions/${code}`)
    return res.data
  },
}
