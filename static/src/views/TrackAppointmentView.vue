<template>
  <div class="public-page track-page">
    <Navbar />

    <div class="page-header">
      <div class="container">
        <h1>Tra cứu lịch hẹn</h1>
        <p>Nhập mã đặt lịch được gửi trong email để kiểm tra trạng thái và quản lý thông tin.</p>
      </div>
    </div>

    <div class="container">
      <div class="search-section">
        <div class="search-card">
          <div class="input-group">
            <i class="fas fa-ticket-alt" />

            <input
              v-model="searchCode"
              placeholder="Nhập mã lịch hẹn (VD: 550E8400)..."
              type="text"
              @keyup.enter="handleSearch"
            >

            <button class="btn-cta" :disabled="loading" @click="handleSearch">
              <span v-if="loading">Đang tìm...</span>
              <span v-else>Tra cứu</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Kết quả tra cứu -->
      <transition name="fade">
        <div v-if="appointment" class="result-section">
          <div class="appointment-detail-card">
            <div class="card-header">
              <div class="status-badge" :class="getStatusClass(appointment.status)">
                {{ translateStatus(appointment.status) }}
              </div>

              <span class="appointment-id">Mã: #{{ appointment.id }}</span>
            </div>

            <div class="card-body">
              <div class="info-grid">
                <div class="info-item">
                  <label>Bệnh nhân</label>
                  <p>{{ appointment.patientName }}</p>
                </div>

                <div class="info-item">
                  <label>Bác sĩ khám</label>
                  <p>BS. {{ appointment.doctorName }}</p>
                </div>

                <div class="info-item">
                  <label>Ngày khám</label>
                  <p>{{ formatDate(appointment.appointmentDate) }}</p>
                </div>

                <div class="info-item">
                  <label>Giờ khám</label>
                  <p>{{ appointment.appointmentTime }}</p>
                </div>

                <div class="info-item full-width">
                  <label>Triệu chứng / Ghi chú</label>
                  <p>{{ appointment.reason || 'Không có ghi chú' }}</p>
                </div>
              </div>
            </div>

            <div v-if="canAction" class="card-footer">
              <button class="btn-outline" @click="showEditModal = true">
                <i class="fas fa-edit" /> Chỉnh sửa
              </button>

              <button class="btn-danger" @click="handleCancel">
                <i class="fas fa-times" /> Hủy lịch hẹn
              </button>
            </div>

            <div v-else class="card-footer locked">
              <i class="fas fa-lock" />
              <span>Lịch hẹn đã được bác sĩ duyệt hoặc đã hoàn thành. Vui lòng gọi 1900 6789 để được hỗ trợ.</span>
            </div>
          </div>
        </div>
      </transition>

      <div v-if="error" class="error-msg">
        <i class="fas fa-exclamation-circle" />
        {{ error }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { computed, onMounted, ref } from 'vue'
  import { useRoute } from 'vue-router'
  import Navbar from '@/components/Navbar.vue'
  import api from '@/services/api'

  const route = useRoute()
  const searchCode = ref('')
  const appointment = ref<any>(null)
  const loading = ref(false)
  const error = ref('')
  const showEditModal = ref(false)

  const canAction = computed(() => {
    if (!appointment.value) return false
    const status = appointment.value.status
    return status === 0 || status === 1
  })

  async function handleSearch () {
    const cleanCode = searchCode.value.trim().replace('#', '')
    if (!cleanCode) return

    loading.value = true
    error.value = ''
    appointment.value = null

    try {
      const res = await api.get(`/Appointments/track/${cleanCode}`)
      appointment.value = {
        ...res.data,
        appointmentDate: res.data.date,
        appointmentTime: res.data.time,
        reason: res.data.reason || 'Khám chuyên khoa',
      }
    } catch (error_: any) {
      error.value = error_.response?.data || 'Không tìm thấy thông tin lịch hẹn. Vui lòng kiểm tra lại mã.'
      console.error(error_)
    } finally {
      loading.value = false
    }
  }

  async function handleCancel () {
    if (!confirm('Bạn có chắc chắn muốn hủy lịch hẹn này không?')) return

    try {
      await api.put(`/Appointments/${appointment.value.id}/cancel`)
      alert('Đã hủy lịch hẹn thành công.')
      handleSearch() // Refresh data
    } catch {
      alert('Có lỗi xảy ra khi hủy lịch. Vui lòng liên hệ hotline.')
    }
  }

  function translateStatus (s: number) {
    const map: any = {
      0: 'Chờ duyệt',
      1: 'Đã xác nhận',
      2: 'Đã hoàn thành',
      3: 'Đã hủy',
    }
    return map[s] || 'Không xác định'
  }

  function getStatusClass (s: number) {
    const map: any = {
      0: 'pending',
      1: 'confirmed',
      2: 'completed',
      3: 'cancelled',
    }
    return map[s] || ''
  }

  function formatDate (dateStr: string) {
    return new Date(dateStr).toLocaleDateString('vi-VN')
  }

  onMounted(() => {
    const code = route.query.code as string
    if (code) {
      searchCode.value = code
      handleSearch()
    }
  })
</script>

<style scoped>
@import '@/styles/public-pages.css';

.track-page { min-height: 100vh; background: #f8fafc; padding-bottom: 5rem; }

.search-section { margin-top: -3rem; position: relative; z-index: 10; margin-bottom: 4rem; }
.search-card {
  max-width: 700px;
  margin: 0 auto;
  background: white;
  padding: 1.5rem;
  border-radius: 24px;
  box-shadow: 0 20px 40px rgba(0,0,0,0.08);
}

.input-group { display: flex; align-items: center; gap: 1rem; }
.input-group i { font-size: 1.25rem; color: var(--cobalt); margin-left: 0.5rem; }
.input-group input {
  flex: 1;
  border: none;
  font-size: 1.1rem;
  outline: none;
  font-weight: 500;
}
.input-group .btn-cta { padding: 0.8rem 2rem; border-radius: 16px; }

.appointment-detail-card {
  max-width: 800px;
  margin: 0 auto;
  background: white;
  border-radius: 32px;
  overflow: hidden;
  border: 1px solid var(--gray-100);
  box-shadow: 0 10px 30px rgba(0,0,0,0.03);
}

.card-header {
  padding: 2rem;
  background: #f8fafc;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid var(--gray-100);
}
.appointment-id { font-weight: 700; color: var(--gray-500); }

.status-badge {
  padding: 0.5rem 1.25rem;
  border-radius: 30px;
  font-size: 0.9rem;
  font-weight: 800;
  text-transform: uppercase;
}
.status-badge.pending { background: #fff7ed; color: #ea580c; }
.status-badge.confirmed, .status-badge.waiting { background: #f0fdf4; color: #16a34a; }
.status-badge.completed { background: #f0f9ff; color: #0284c7; }
.status-badge.cancelled { background: #fef2f2; color: #dc2626; }

.card-body { padding: 3rem; }
.info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 2.5rem; }
.info-item label { display: block; font-size: 0.8rem; text-transform: uppercase; color: var(--gray-500); font-weight: 700; margin-bottom: 0.5rem; letter-spacing: 1px; }
.info-item p { font-size: 1.15rem; font-weight: 600; color: var(--gray-900); }
.full-width { grid-column: span 2; }

.card-footer {
  padding: 2rem 3rem;
  background: #f8fafc;
  display: flex;
  gap: 1rem;
  border-top: 1px solid var(--gray-100);
}

.card-footer.locked {
  background: #fffbeb;
  color: #92400e;
  justify-content: center;
  font-weight: 600;
  font-size: 0.9rem;
}

.btn-danger {
  background: #fee2e2;
  color: #dc2626;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 12px;
  font-weight: 700;
  cursor: pointer;
  transition: 0.3s;
}
.btn-danger:hover { background: #fecaca; }

.error-msg {
  text-align: center;
  margin-top: 2rem;
  color: #dc2626;
  font-weight: 600;
}

.fade-enter-active, .fade-leave-active { transition: opacity 0.5s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>
