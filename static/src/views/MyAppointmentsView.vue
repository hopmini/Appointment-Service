<template>
  <div class="public-page">
    <Navbar />

    <div class="page-header">
      <div class="container">
        <h1>Lịch sử đặt lịch</h1>
        <p>Xem tất cả lịch hẹn của bạn tại Medicare.</p>
      </div>
    </div>

    <div class="container" style="padding-bottom: 4rem;">
      <div v-if="loading" class="loading-state">
        <i class="fas fa-circle-notch fa-spin fa-2x" style="color: #0047AB; margin-bottom: 1rem;" />
        <p>Đang tải lịch hẹn...</p>
      </div>

      <div v-else-if="appointments.length === 0" class="empty-state">
        <div class="empty-icon"><i class="fas fa-calendar-times" style="font-size: 3rem; color: #cbd5e1;" /></div>
        <h3>Chưa có lịch hẹn nào</h3>
        <p style="color: #64748b;">Hãy đặt lịch khám ngay để được chăm sóc sức khỏe tốt nhất.</p>
        <router-link class="btn-cta" style="display: inline-block; margin-top: 1rem; padding: 0.75rem 2rem; border-radius: 12px; text-decoration: none;" to="/patient">Đặt lịch ngay</router-link>
      </div>

      <div v-else class="appointments-list">
        <div v-for="app in appointments" :key="app.id" class="appointment-card">
          <div class="appointment-card__header">
            <span class="badge" :class="getStatusClass(app.status)">{{ getStatusText(app.status) }}</span>
            <span class="appointment-id">#{{ app.id.substring(0, 8).toUpperCase() }}</span>
          </div>

          <div class="appointment-card__body">
            <div class="appointment-info">
              <div class="info-row">
                <i class="fas fa-user-md" style="color: #0047AB; width: 20px;" />
                <span><strong>Bác sĩ:</strong> {{ app.doctorName }}</span>
              </div>

              <div class="info-row">
                <i class="fas fa-notes-medical" style="color: #0047AB; width: 20px;" />
                <span><strong>Dịch vụ:</strong> {{ app.serviceName }}</span>
              </div>

              <div class="info-row">
                <i class="fas fa-calendar-alt" style="color: #0047AB; width: 20px;" />
                <span><strong>Ngày:</strong> {{ formatDate(app.date) }}</span>
              </div>

              <div class="info-row">
                <i class="fas fa-clock" style="color: #0047AB; width: 20px;" />
                <span><strong>Giờ:</strong> {{ formatTime(app.time) }}</span>
              </div>

              <div v-if="app.queueNumber" class="info-row">
                <i class="fas fa-sort-numeric-up-alt" style="color: #0047AB; width: 20px;" />
                <span><strong>Số thứ tự:</strong> {{ app.queueNumber }}</span>
              </div>
            </div>

            <div class="appointment-card__actions">
              <router-link class="btn-outline" :to="'/track?code=' + app.id">
                <i class="fas fa-eye" /> Chi tiết
              </router-link>

              <button v-if="app.status === 0 || app.status === 1" class="btn-danger" @click="cancelAppointment(app.id)">
                <i class="fas fa-times" /> Hủy
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { onMounted, ref } from 'vue'
  import Navbar from '@/components/Navbar.vue'
  import api from '@/services/api'
  import { useAuthStore } from '@/stores/authStore'

  const authStore = useAuthStore()
  const loading = ref(false)
  const appointments = ref<any[]>([])

async function fetchAppointments () {
  loading.value = true
  try {
    const res = await api.get('/Appointments/my')
    appointments.value = res.data as any[]
  } catch (error) {
    console.error('Lỗi tải lịch hẹn:', error)
  } finally {
    loading.value = false
  }
}

  async function cancelAppointment (id: string) {
    if (!confirm('Bạn có chắc muốn hủy lịch hẹn này?')) return
    try {
      await api.put(`/Appointments/${id}/cancel`)
      alert('Đã hủy lịch hẹn thành công.')
      await fetchAppointments()
    } catch {
      alert('Lỗi khi hủy lịch hẹn.')
    }
  }

  const formatDate = (d: string) => new Date(d).toLocaleDateString('vi-VN')
  const formatTime = (t: string) => t?.slice(0, 5) || '--:--'
  function getStatusText (s: number) {
    const map: Record<number, string> = { 0: 'Chờ duyệt', 1: 'Đã duyệt', 2: 'Đã khám' }
    return map[s] || 'Đã hủy'
  }
  function getStatusClass (s: number) {
    const map: Record<number, string> = { 0: 'badge--pending', 1: 'badge--confirmed', 2: 'badge--completed' }
    return map[s] || 'badge--cancelled'
  }

  onMounted(() => {
    if (!authStore.isAuthenticated) {
      window.location.href = '/login'
      return
    }
    fetchAppointments()
  })
</script>

<style scoped>
@import '@/styles/public-pages.css';

.loading-state, .empty-state {
  text-align: center;
  padding: 4rem 2rem;
}

.appointments-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  max-width: 700px;
  margin: 0 auto;
}

.appointment-card {
  background: white;
  border-radius: 20px;
  overflow: hidden;
  border: 1px solid #e2e8f0;
  box-shadow: 0 2px 8px rgba(0,0,0,0.04);
}

.appointment-card__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem 1.5rem;
  background: #f8fafc;
  border-bottom: 1px solid #e2e8f0;
}

.appointment-id {
  font-weight: 700;
  color: #94a3b8;
  font-size: 0.85rem;
}

.appointment-card__body {
  padding: 1.5rem;
}

.appointment-info {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  margin-bottom: 1.5rem;
}

.info-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.95rem;
  color: #334155;
}

.appointment-card__actions {
  display: flex;
  gap: 0.75rem;
  padding-top: 1rem;
  border-top: 1px solid #f1f5f9;
}

.btn-outline {
  padding: 0.5rem 1.25rem;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  background: white;
  color: #0047AB;
  font-weight: 600;
  cursor: pointer;
  text-decoration: none;
  font-size: 0.85rem;
  transition: 0.2s;
}

.btn-outline:hover {
  background: #f8fafc;
}

.btn-danger {
  padding: 0.5rem 1.25rem;
  border: none;
  border-radius: 10px;
  background: #fef2f2;
  color: #dc2626;
  font-weight: 600;
  cursor: pointer;
  font-size: 0.85rem;
  transition: 0.2s;
}

.btn-danger:hover {
  background: #fee2e2;
}

.badge {
  padding: 0.3rem 0.75rem;
  border-radius: 20px;
  font-size: 0.75rem;
  font-weight: 700;
}

.badge--pending { background: #fff7ed; color: #ea580c; }
.badge--confirmed { background: #f0fdf4; color: #16a34a; }
.badge--completed { background: #f0f9ff; color: #0284c7; }
.badge--cancelled { background: #fef2f2; color: #dc2626; }
</style>
