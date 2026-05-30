<template>
  <div class="patient-booking">
    <div class="booking-layout">
      <!-- SIDEBAR PROGRESS -->
      <aside class="booking-sidebar">
        <div class="sidebar-brand">
          <h1>Medicare<span>.</span></h1>

          <button class="btn-back-v btn-main" style="margin-top: 1rem; padding: 0.75rem 1.5rem; font-size: 0.9rem;" @click="$router.push('/')">
            <i class="fas fa-home" /> Trang chủ
          </button>
        </div>

        <nav class="progress-steps-v">
          <div class="step-v" :class="{ 'step-v--active': currentStep === 1, 'step-v--completed': currentStep > 1 }">
            <div class="step-v__num">1</div>

            <div class="step-v__content">
              <span class="step-v__label">Dịch vụ</span>
              <span class="step-v__desc">Chọn gói khám</span>
            </div>
          </div>

          <div class="step-v" :class="{ 'step-v--active': currentStep === 2, 'step-v--completed': currentStep > 2 }">
            <div class="step-v__num">2</div>

            <div class="step-v__content">
              <span class="step-v__label">Bác sĩ</span>
              <span class="step-v__desc">Chuyên gia giỏi</span>
            </div>
          </div>

          <div class="step-v" :class="{ 'step-v--active': currentStep === 3, 'step-v--completed': currentStep > 3 }">
            <div class="step-v__num">3</div>

            <div class="step-v__content">
              <span class="step-v__label">Thời gian</span>
              <span class="step-v__desc">Ngày & giờ khám</span>
            </div>
          </div>

          <div class="step-v" :class="{ 'step-v--active': currentStep === 4, 'step-v--completed': currentStep > 4 }">
            <div class="step-v__num">4</div>

            <div class="step-v__content">
              <span class="step-v__label">Xác nhận</span>
              <span class="step-v__desc">Hoàn tất lịch hẹn</span>
            </div>
          </div>
        </nav>
      </aside>

      <!-- MAIN CONTENT -->
      <main class="booking-main">
        <div class="booking-content-card">
          <!-- STEP 1: SELECT SERVICE -->
          <section v-if="currentStep === 1">
            <div class="section-title">
              <h2>Chọn dịch vụ khám</h2>
              <p>Hệ thống tự động sắp xếp bác sĩ phù hợp</p>
            </div>

            <div v-if="loadingServices" class="loading-state">
              <i class="fas fa-circle-notch fa-spin fa-2x" style="color: #0047AB; margin-bottom: 1rem;" />
              <p>Đang tải danh sách dịch vụ...</p>
            </div>

            <div v-else class="grid-container">
              <div
                v-for="service in medicalServices"
                :key="service.id"
                class="card-item"
                :class="{ 'card-item--active': selectedService?.id === service.id }"
                @click="selectService(service)"
              >
                <div class="card-item__icon"><i class="fas fa-stethoscope" /></div>

                <div class="card-item__info">
                  <h3>{{ service.name }}</h3>
                  <p>{{ service.description }}</p>
                  <div class="card-item__price">{{ formatPrice(service.price) }}</div>
                </div>
              </div>
            </div>
          </section>

          <!-- STEP 2: SELECT DOCTOR -->
          <section v-if="currentStep === 2">
            <div class="section-title">
              <h2>Chọn bác sĩ chuyên khoa</h2>
              <p>Dịch vụ: <strong>{{ selectedService?.name }}</strong></p>
            </div>

            <div v-if="loadingDoctors" class="loading-state">
              <i class="fas fa-circle-notch fa-spin fa-2x" style="color: #0047AB; margin-bottom: 1rem;" />
              <p>Đang tìm đội ngũ bác sĩ...</p>
            </div>

            <div v-else class="grid-container">
              <div
                v-for="doc in filteredDoctors"
                :key="doc.id"
                class="card-item"
                :class="{ 'card-item--active': selectedDoctor?.id === doc.id }"
                @click="selectDoctor(doc)"
              >
                <div class="card-item__icon"><i class="fas fa-user-md" /></div>

                <div class="card-item__info">
                  <h3>{{ doc.fullName }}</h3>
                  <p class="specialty" style="color: #0047AB; font-weight: 600;">{{ doc.specialty }}</p>
                  <div class="card-item__price">Phí: {{ formatPrice(doc.consultationFee) }}</div>
                </div>
              </div>
            </div>

            <div class="action-footer">
              <button class="btn-main btn-back-v" @click="currentStep = 1"><i class="fas fa-chevron-left" /> Quay lại</button>
              <button class="btn-main btn-primary-v" :disabled="!selectedDoctor" @click="currentStep = 3">Tiếp tục <i class="fas fa-chevron-right" /></button>
            </div>
          </section>

          <!-- STEP 3: SELECT TIME -->
          <section v-if="currentStep === 3">
            <div class="section-title">
              <h2>Chọn thời gian khám</h2>
              <p>Bác sĩ: <strong>{{ selectedDoctor?.fullName }}</strong></p>
            </div>

            <div class="date-scroll-v">
              <div
                v-for="date in nextSevenDays"
                :key="date.iso"
                class="date-card-v"
                :class="{ 'date-card-v--active': selectedDate === date.iso }"
                @click="selectDate(date.iso)"
              >
                <div style="font-size: 0.8rem; font-weight: 700;">{{ date.dayName }}</div>
                <div style="font-size: 1.5rem; font-weight: 900;">{{ date.dayNum }}</div>
                <div style="font-size: 0.75rem;">Tháng {{ date.month }}</div>
              </div>
            </div>

            <div v-if="selectedDate" class="slot-container">
              <div v-if="loadingSlots" class="loading-state">
                <i class="fas fa-spinner fa-spin fa-2x" style="color: #0047AB; margin-bottom: 1rem;" />
                <p>Đang kiểm tra lịch...</p>
              </div>

              <div v-else-if="slots.length === 0" class="empty-slots">
                <p>Bác sĩ không có lịch trong ngày này.</p>
              </div>

              <div v-else class="slot-grid-v">
                <div
                  v-for="slot in slots"
                  :key="slot.id"
                  class="slot-v"
                  :class="{ 'slot-v--active': selectedSlot?.id === slot.id, 'slot-v--disabled': slot.isBooked }"
                  @click="!slot.isBooked && (selectedSlot = slot)"
                >
                  <i class="far fa-clock" /> {{ formatTime(slot.startTime) }}
                </div>
              </div>
            </div>

            <div class="action-footer">
              <button class="btn-main btn-back-v" @click="currentStep = 2"><i class="fas fa-chevron-left" /> Quay lại</button>
              <button class="btn-main btn-primary-v" :disabled="!selectedSlot" @click="currentStep = 4">Xác nhận <i class="fas fa-chevron-right" /></button>
            </div>
          </section>

          <!-- STEP 4: FINAL -->
          <section v-if="currentStep === 4" class="step-final">
            <div class="section-title">
              <h2>Xác nhận thông tin</h2>
              <p>Vui lòng kiểm tra kỹ trước khi đặt lịch</p>
            </div>

            <div class="final-grid">
              <!-- Cột trái: Form thông tin -->
              <div class="final-form-col">
                <div class="summary-section">
                  <h3><i class="fas fa-user-circle" /> Thông tin bệnh nhân</h3>
                  <div class="final-form-fields">
                    <div class="form-field">
                      <label>Họ và tên</label>
                      <div class="field-value">{{ authStore.user?.fullName }}</div>
                    </div>
                    <div class="form-field">
                      <label>Tên đăng nhập</label>
                      <div class="field-value">{{ authStore.user?.username }}</div>
                    </div>
                    <div class="form-field">
                      <label>Email</label>
                      <div class="field-value">{{ authStore.user?.email }}</div>
                    </div>
                    <div class="form-field">
                      <label>Số điện thoại</label>
                      <input v-model="patientForm.phone" placeholder="Số điện thoại liên hệ" type="tel" class="final-input" />
                    </div>
                    <div class="form-field">
                      <label>Địa chỉ</label>
                      <input v-model="patientForm.address" placeholder="Địa chỉ của bạn" type="text" class="final-input" />
                    </div>
                    <div class="form-field full-width">
                      <label>Lý do khám</label>
                      <textarea v-model="reason" placeholder="Triệu chứng hiện tại..." rows="2" class="final-textarea" />
                    </div>
                  </div>
                </div>
              </div>

              <!-- Cột phải: Tóm tắt lịch hẹn -->
              <div class="final-summary-col">
                <div class="summary-card">
                  <h3><i class="fas fa-calendar-check" /> Lịch hẹn</h3>
                  <div class="summary-row">
                    <span class="summary-label">Dịch vụ</span>
                    <span class="summary-value">{{ selectedService?.name }}</span>
                  </div>
                  <div class="summary-row">
                    <span class="summary-label">Bác sĩ</span>
                    <span class="summary-value">BS. {{ selectedDoctor?.fullName }}</span>
                  </div>
                  <div class="summary-row">
                    <span class="summary-label">Thời gian</span>
                    <span class="summary-value">{{ formatDateFull(selectedDate) }} - {{ formatTime(selectedSlot?.startTime) }}</span>
                  </div>
                  <div class="summary-row total-row">
                    <span class="summary-label">Phí khám</span>
                    <span class="summary-value total-value">{{ formatPrice((selectedService?.price || 0) + (selectedDoctor?.consultationFee || 0)) }}</span>
                  </div>
                </div>

                <div class="confirm-box">
                  <p class="confirm-note">Sau khi đặt, vui lòng chờ tiếp tân duyệt. Email xác nhận sẽ được gửi đến bạn.</p>
                  <button class="btn-book-final" :disabled="submitting" @click="bookAppointment">
                    {{ submitting ? 'ĐANG XỬ LÝ...' : 'XÁC NHẬN ĐẶT LỊCH' }}
                  </button>
                </div>
              </div>
            </div>
          </section>
        </div>
      </main>
    </div>

    <!-- TOASTS -->
    <div class="toast-container">
      <div v-for="toast in toasts" :key="toast.id" class="toast" :class="'toast--' + toast.type">
        <div class="toast-body">
          <span class="toast-title">{{ toast.title }}</span>
          <p class="toast-message">{{ toast.message }}</p>
        </div>

        <button class="toast-close" @click="removeToast(toast.id)"><i class="fas fa-times" /></button>
        <div class="toast-progress" :style="{ animationDuration: '4s' }" />
      </div>
    </div>

    <!-- SUCCESS MODAL -->
    <div v-if="successData" class="success-overlay">
      <div class="success-card">
        <div class="success-icon"><i class="fas fa-check-circle" /></div>
        <h2>Thành công!</h2>
        <div class="appointment-id">#{{ successData.id?.slice(0,8).toUpperCase() }}</div>
        <p style="margin: 1rem 0; color: #64748b;">Vui lòng kiểm tra Email <b>{{ authStore.user?.email }}</b> để nhận kết quả khi được duyệt.</p>
        <button class="btn-primary" @click="$router.push('/')">VỀ TRANG CHỦ</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import type { BookAppointmentRequest } from '@/types/appointment'
  import { computed, onMounted, ref } from 'vue'
  import api from '@/services/api'
  import { appointmentService } from '@/services/appointmentService'
  import { useAuthStore } from '@/stores/authStore'
  import { useDoctorStore } from '@/stores/doctorStore'

  const authStore = useAuthStore()
  const doctorStore = useDoctorStore()

  if (!authStore.isAuthenticated || !authStore.isPatient) {
    window.location.href = '/login'
  }

  const currentStep = ref(1)
  const medicalServices = ref<any[]>([])
  const loadingServices = ref(false)
  const selectedService = ref<any>(null)
  const selectedDoctor = ref<any>(null)
  const selectedDate = ref('')
  const selectedSlot = ref<any>(null)
  const reason = ref('')
  const loadingSlots = ref(false)
  const submitting = ref(false)
  const successData = ref<any>(null)
  const slots = ref<any[]>([])
  const toasts = ref<any[]>([])

  const patientForm = ref({
    phone: '',
    address: '',
  })

  function addToast (title: string, message: string, type: 'success' | 'error' | 'warning' = 'success') {
    const id = Date.now()
    toasts.value.push({ id, title, message, type })
    setTimeout(() => removeToast(id), 4000)
  }

  function removeToast (id: number) {
    toasts.value = toasts.value.filter(t => t.id !== id)
  }

  const loadingDoctors = computed(() => doctorStore.loading)
  const filteredDoctors = computed(() => doctorStore.doctors)

  const nextSevenDays = computed(() => {
    const days = []
    const today = new Date()
    const dayNames = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7']
    for (let i = 0; i < 7; i++) {
      const d = new Date()
      d.setDate(today.getDate() + i)
      const year = d.getFullYear()
      const month = String(d.getMonth() + 1).padStart(2, '0')
      const day = String(d.getDate()).padStart(2, '0')
      days.push({
        iso: `${year}-${month}-${day}`,
        dayNum: d.getDate(),
        month: d.getMonth() + 1,
        dayName: i === 0 ? 'Hôm nay' : dayNames[d.getDay()],
      })
    }
    return days
  })

  async function fetchServices () {
    loadingServices.value = true
    try {
      const res = await api.get('/MedicalServices')
      medicalServices.value = res.data
    } catch {
      addToast('Lỗi', 'Không thể tải dịch vụ', 'error')
    } finally {
      loadingServices.value = false
    }
  }

  function selectService (service: any) {
    selectedService.value = service
    currentStep.value = 2
    addToast('Đã chọn', service.name)
  }

  function selectDoctor (doc: any) {
    selectedDoctor.value = doc
    addToast('Đã chọn', `BS. ${doc.fullName}`)
  }

  async function selectDate (dateIso: string) {
    selectedDate.value = dateIso
    selectedSlot.value = null
    fetchSlots()
  }

  async function fetchSlots () {
    if (!selectedDoctor.value || !selectedDate.value) return
    loadingSlots.value = true
    try {
      slots.value = await appointmentService.getAvailableSlots(selectedDoctor.value.id, selectedDate.value)
    } catch {
      addToast('Lỗi', 'Không thể tải lịch khám', 'error')
    } finally {
      loadingSlots.value = false
    }
  }

  async function bookAppointment () {
    submitting.value = true
    try {
      const data: BookAppointmentRequest = {
        patientId: authStore.user!.id,
        slotId: selectedSlot.value?.id || '',
        medicalServiceId: selectedService.value?.id || '',
        reason: reason.value,
      }
      const response = await appointmentService.bookAppointment(data)
      successData.value = { ...response, patientName: authStore.user?.fullName }
    } catch (error: any) {
      addToast('Thất bại', error.response?.data?.message || 'Lỗi đặt lịch', 'error')
    } finally {
      submitting.value = false
    }
  }

  const formatPrice = (p: number) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(p)
  const formatTime = (t: string) => t?.slice(0, 5) || ''
  function formatDateFull (iso: string) {
    if (!iso) return ''
    const d = new Date(iso)
    return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
  }

  onMounted(() => {
    fetchServices()
    doctorStore.fetchDoctors()
  })
</script>

<style src="@/styles/patient.css"></style>
