export interface AppointmentDto {
  id: string
  patientId: number
  doctorId: string
  slotId: string
  date: string
  time: string
  reason: string
  status: number // 0: Chờ duyệt, 1: Đã duyệt, 2: Xong, 3: Hủy
  queueNumber?: number
}

export interface BookAppointmentRequest {
  patientId: number
  slotId: string
  medicalServiceId: string
  reason: string
}
