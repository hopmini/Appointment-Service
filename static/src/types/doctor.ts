export interface DoctorDto {
  id: string
  userId: string
  fullName: string
  specialty: string
  degree: string
  consultationFee: number
}

export interface DoctorSlotDto {
  id: string
  doctorId: string
  date: string
  startTime: string
  endTime: string
  isBooked: boolean
}
