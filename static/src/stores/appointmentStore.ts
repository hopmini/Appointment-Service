import type { AppointmentDto, BookAppointmentRequest } from '@/types/appointment'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { appointmentService } from '@/services/appointmentService'

export const useAppointmentStore = defineStore('appointment', () => {
  // State
  const appointments = ref<AppointmentDto[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  // Getters
  const getAppointmentsByPatient = computed(() => (patientId: number) => {
    return appointments.value.filter(apt => apt.patientId === patientId)
  })

  const getAppointmentsByDoctor = computed(() => (doctorId: string) => {
    return appointments.value.filter(apt => apt.doctorId === doctorId)
  })

  // Actions
  const fetchAppointments = async () => {
    loading.value = true
    error.value = null
    try {
      const data = await appointmentService.getAllAppointments()
      appointments.value = data
    } catch (error_) {
      error.value = error_ instanceof Error ? error_.message : 'Lỗi tải danh sách lịch hẹn'
    } finally {
      loading.value = false
    }
  }

  const bookAppointment = async (appointmentData: BookAppointmentRequest) => {
    loading.value = true
    error.value = null
    try {
      const newAppointment = await appointmentService.bookAppointment(appointmentData)
      appointments.value.push(newAppointment)
      return newAppointment
    } catch (error_) {
      error.value = error_ instanceof Error ? error_.message : 'Lỗi đặt lịch hẹn'
      throw error_
    } finally {
      loading.value = false
    }
  }

  const cancelAppointment = async (appointmentId: string) => {
    loading.value = true
    error.value = null
    try {
      await appointmentService.cancelAppointment(appointmentId)
      appointments.value = appointments.value.filter(apt => apt.id !== appointmentId)
    } catch (error_) {
      error.value = error_ instanceof Error ? error_.message : 'Lỗi hủy lịch hẹn'
      throw error_
    } finally {
      loading.value = false
    }
  }

  const updateAppointment = async (appointmentId: string, updateData: Partial<AppointmentDto>) => {
    loading.value = true
    error.value = null
    try {
      const updatedAppointment = await appointmentService.updateAppointment(appointmentId, updateData)
      const index = appointments.value.findIndex(apt => apt.id === appointmentId)
      if (index !== -1) {
        appointments.value[index] = updatedAppointment
      }
      return updatedAppointment
    } catch (error_) {
      error.value = error_ instanceof Error ? error_.message : 'Lỗi cập nhật lịch hẹn'
      throw error_
    } finally {
      loading.value = false
    }
  }

  return {
    // State
    appointments,
    loading,
    error,

    // Getters
    getAppointmentsByPatient,
    getAppointmentsByDoctor,

    // Actions
    fetchAppointments,
    bookAppointment,
    cancelAppointment,
    updateAppointment,
  }
})
