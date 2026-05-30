<template>
  <div class="doctor-layout">
    <aside class="sidebar">
      <div class="sidebar__header">
        <div class="logo" style="cursor: pointer;" @click="goBackToHub">
          <div class="logo__icon">
            <svg fill="none" viewBox="0 0 32 32" xmlns="http://www.w3.org/2000/svg" style="height: 32px; width: 32px;">
              <rect fill="#0047AB" height="32" rx="8" width="32" />
              <path d="M16 6v20M6 16h20" stroke="white" stroke-linecap="round" stroke-width="4" />
            </svg>
          </div>

          <div class="logo__text" style="font-weight: 800; font-size: 1.2rem; margin-left: 8px;">
            Medicare<span style="color: #E53935;">.</span>
          </div>
        </div>
      </div>

      <nav class="sidebar__nav">
        <div class="sidebar__divider">BÁC SĨ</div>

        <div class="nav-item nav-item--active">
          <span class="nav-icon"><i class="fas fa-calendar-day" /></span>
          <span>Lịch hẹn hôm nay</span>
        </div>

        <div class="sidebar__divider">LIÊN THÔNG</div>

        <div class="nav-item logout-btn" @click="goBackToHub">
          <span class="nav-icon"><i class="fas fa-home" /></span>
          <span>Về Cổng Trung Tâm</span>
        </div>
      </nav>

      <div class="sidebar__footer">
        <div v-if="authStore.user" class="user-brief">
          <div class="avatar-small">{{ (authStore.user.username || authStore.user.fullName || 'U').substring(0, 2).toUpperCase() }}</div>

          <div class="user-brief-info">
            <p class="user-brief-name">{{ authStore.user.fullName }}</p>
            <p class="user-brief-role">Bác sĩ</p>
          </div>
        </div>

        <div class="nav-item logout-btn" @click="authStore.logout()">
          <span class="nav-icon"><i class="fas fa-sign-out-alt" /></span>
          <span>Đăng xuất</span>
        </div>
      </div>
    </aside>

    <main class="main-content">
      <header class="top-bar">
        <div class="page-context">
          <h1 class="page-title">Lịch hẹn hôm nay</h1>
          <p v-if="doctorData" class="page-subtitle">BS. {{ doctorData.doctorName }} - {{ formatDate(doctorData.date) }}</p>
        </div>

        <div class="top-bar-actions">
          <button class="btn-primary" @click="fetchData">
            <i class="fas fa-sync-alt" :class="{ 'fa-spin': loading }" /> Làm mới
          </button>
        </div>
      </header>

      <div class="page-container">
        <div v-if="loading" class="loading-state">
          <i class="fas fa-circle-notch fa-spin fa-2x" style="color: #0047AB; margin-bottom: 1rem;" />
          <p>Đang tải lịch hẹn...</p>
        </div>

        <template v-else-if="doctorData">
          <div class="stats-grid" style="margin-bottom: 2rem;">
            <div class="stat-card">
              <p class="stat-card__label">Tổng lịch hẹn</p>
              <h3 class="stat-card__value">{{ doctorData.appointments?.length || 0 }}</h3>
            </div>

            <div class="stat-card">
              <p class="stat-card__label">Chờ khám</p>
              <h3 class="stat-card__value" style="color: #f59e0b;">{{ doctorData.appointments?.filter((a: any) => a.status === 1).length || 0 }}</h3>
            </div>

            <div class="stat-card">
              <p class="stat-card__label">Đã khám</p>
              <h3 class="stat-card__value" style="color: #10b981;">{{ doctorData.appointments?.filter((a: any) => a.status === 2).length || 0 }}</h3>
            </div>
          </div>

          <div v-if="!doctorData.appointments || doctorData.appointments.length === 0" class="empty-state">
            <div class="empty-icon"><i class="fas fa-calendar-check" /></div>
            <p>Hôm nay không có lịch hẹn nào.</p>
          </div>

          <table v-else class="data-table">
            <thead>
              <tr>
                <th>STT</th>
                <th>Bệnh nhân</th>
                <th>Giờ</th>
                <th>Dịch vụ</th>
                <th>Lý do khám</th>
                <th>Trạng thái</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="app in doctorData.appointments" :key="app.id">
                <td>
                  <span class="queue-number-circle">{{ app.queueNumber || '--' }}</span>
                </td>

                <td>
                  <div style="display: flex; align-items: center; gap: 0.75rem;">
                    <div style="width: 36px; height: 36px; border-radius: 50%; background: #0047AB; display: flex; align-items: center; justify-content: center; color: white; font-weight: 600; font-size: 0.85rem;">
                      {{ app.patientName?.charAt(0) || '?' }}
                    </div>

                    <div>
                      <p style="font-weight: 600;">{{ app.patientName }}</p>
                    </div>
                  </div>
                </td>

                <td>{{ formatTime(app.startTime) }} - {{ formatTime(app.endTime) }}</td>
                <td><span class="service-badge">{{ app.serviceName }}</span></td>
                <td><span class="table-text-truncate" :title="app.reason">{{ app.reason || '--' }}</span></td>
                <td><span class="badge" :class="getStatusClass(app.status)">{{ getStatusText(app.status) }}</span></td>
              </tr>
            </tbody>
          </table>
        </template>

        <div v-else class="empty-state">
          <div class="empty-icon"><i class="fas fa-user-md" /></div>
          <p>Không tìm thấy thông tin bác sĩ. Vui lòng liên hệ Admin.</p>
        </div>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
  import { onMounted, ref } from 'vue'
  import api from '@/services/api'
  import { useAuthStore } from '@/stores/authStore'

  const authStore = useAuthStore()
  const loading = ref(false)
  const doctorData = ref<any>(null)

  const PORTAL_URL = import.meta.env.VITE_PORTAL_URL || `${window.location.protocol}//${window.location.hostname}:3000`

  function goBackToHub () {
    window.location.href = `${PORTAL_URL}/dashboard`
  }

  async function fetchData () {
    loading.value = true
    try {
      const res = await api.get('/Appointments/doctor-today')
      doctorData.value = res.data
    } catch (error) {
      console.error('Lỗi tải lịch hẹn:', error)
    } finally {
      loading.value = false
    }
  }

  function formatDate (d: string | Date) {
    if (!d) return ''
    const dateObj = typeof d === 'string' ? new Date(d) : d
    return dateObj.toLocaleDateString('vi-VN')
  }

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
    fetchData()
  })
</script>

<style scoped>
@import '@/styles/dashboard.css';

.doctor-layout {
  display: flex;
  min-height: 100vh;
  background: #f8fafc;
}

.main-content {
  flex: 1;
  margin-left: 260px;
  padding: 0;
}

.top-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.25rem 2rem;
  background: white;
  border-bottom: 1px solid #e2e8f0;
}

.page-context {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.page-title {
  font-size: 1.25rem;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
}

.page-subtitle {
  font-size: 0.85rem;
  color: #64748b;
  margin: 0;
}

.page-container {
  padding: 2rem;
}

.queue-number-circle {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: #f1f5f9;
  font-weight: 800;
  font-size: 0.9rem;
  color: #0047AB;
}

.loading-state {
  text-align: center;
  padding: 4rem;
  color: #64748b;
}

.empty-state {
  text-align: center;
  padding: 4rem;
  color: #64748b;
}

.empty-icon {
  font-size: 3rem;
  color: #cbd5e1;
  margin-bottom: 1rem;
}
</style>
