<template>
  <div class="receptionist-layout">
    <!-- SIDEBAR -->
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
        <div class="nav-item" :class="{ 'nav-item--active': activeTab === 'dashboard' }" @click="activeTab = 'dashboard'">
          <span>📊</span>
          <span>Dashboard</span>
        </div>

        <div class="nav-item" :class="{ 'nav-item--active': activeTab === 'appointments' }" @click="activeTab = 'appointments'">
          <span>📅</span>
          <span>Lịch hẹn</span>
        </div>

        <div class="sidebar__divider">LIÊN THÔNG</div>

        <div class="nav-item logout-btn" @click="goBackToHub">
          <span class="nav-icon"><i class="fas fa-home" /></span>
          <span>Về Cổng Trung Tâm</span>
        </div>
      </nav>

      <div class="sidebar__footer">
        <div class="nav-item" @click="authStore.logout()">
          <span>🚪</span>
          <span>Đăng xuất</span>
        </div>
      </div>
    </aside>

    <!-- MAIN CONTENT -->
    <main class="main-content">
      <!-- TOP BAR -->
      <header class="top-bar">
        <div class="search-box">
          <svg
            fill="none"
            height="18"
            stroke="currentColor"
            stroke-width="2"
            viewBox="0 0 24 24"
            width="18"
            xmlns="http://www.w3.org/2000/svg"
          >
            <circle cx="11" cy="11" r="8" />
            <path d="M21 21l-4.35-4.35" />
          </svg>

          <input v-model="searchQuery" placeholder="Tìm kiếm bệnh nhân..." type="text">
        </div>

        <div class="user-profile">
          <div class="user-info" style="text-align: right;">
            <p style="font-weight: 600; font-size: 0.9rem;">{{ authStore.user?.fullName || 'Tiếp Tân' }}</p>
            <p style="font-size: 0.75rem; color: var(--gray-500);">Tiếp tân</p>
          </div>

          <div class="user-avatar">{{ (authStore.user?.username || authStore.user?.fullName || 'AD').substring(0, 2).toUpperCase() }}</div>
        </div>
      </header>

      <!-- PAGE CONTAINER -->
      <div class="page-container">
        <div class="page-header">
          <h1 class="page-title">Cổng Tiếp Tân</h1>
          <p class="page-subtitle">Chào buổi sáng! Hệ thống đã sẵn sàng xử lý dữ liệu thực.</p>
        </div>

        <!-- STATS GRID -->
        <div class="stats-grid">
          <div class="stat-card">
            <span class="stat-card__label">Tổng lịch hẹn</span>
            <span class="stat-card__value">{{ stats.total }}</span>
            <span class="stat-card__trend trend--up">Cập nhật thời gian thực</span>
          </div>

          <div class="stat-card">
            <span class="stat-card__label">Chờ duyệt</span>
            <span class="stat-card__value" style="color: #c2410c;">{{ stats.pending }}</span>
            <span class="stat-card__trend">Cần xử lý ngay</span>
          </div>

          <div class="stat-card">
            <span class="stat-card__label">Đã xác nhận</span>
            <span class="stat-card__value" style="color: #047857;">{{ stats.confirmed }}</span>
            <span class="stat-card__trend trend--up">Lịch hẹn hợp lệ</span>
          </div>

          <div class="stat-card">
            <span class="stat-card__label">Vai trò hiện tại</span>
            <span class="stat-card__value" style="font-size: 1.5rem;">{{ authStore.user?.role }}</span>
            <span class="stat-card__trend">Quyền hạn hệ thống</span>
          </div>
        </div>

        <!-- RECENT APPOINTMENTS -->
        <section class="content-section">
          <div class="section-header">
            <h2 class="section-title">{{ activeTab === 'dashboard' ? 'Danh sách chờ duyệt' : 'Tất cả lịch hẹn' }}</h2>
            <button class="btn-ghost" style="padding: 0.4rem 0.875rem; font-size: 0.85rem; border: 1px solid var(--gray-200); background: white; border-radius: 4px; cursor: pointer;" @click="fetchData">Làm mới</button>
          </div>

          <div v-if="loading" class="empty-state">Đang tải dữ liệu...</div>
          <div v-else-if="appointments.length === 0" class="empty-state">Không có lịch hẹn nào.</div>

          <table v-else class="data-table">
            <thead>
              <tr>
                <th>Mã đơn</th>
                <th>Bệnh nhân</th>
                <th>Bác sĩ</th>
                <th>Thời gian</th>
                <th>Mã số / STT</th>
                <th>Trạng thái</th>
                <th>Thao tác</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="app in filteredAppointments" :key="app.id">
                <td>
                  <code style="font-family: monospace; background: #f1f5f9; padding: 2px 6px; border-radius: 4px; font-size: 0.8rem;">#{{ String(app.id).substring(0,8).toUpperCase() }}</code>
                </td>

                <td>
                  <div style="display: flex; align-items: center; gap: 0.75rem;">
                    <div style="width: 32px; height: 32px; border-radius: 50%; display: flex; align-items: center; justify-content: center; color: white; font-weight: 600; font-size: 0.8rem; background: #0047AB;">
                      {{ app.patientName?.charAt(0) || '?' }}
                    </div>

                    <div>
                      <p style="font-weight: 600;">{{ app.patientName }}</p>
                      <p style="font-size: 0.75rem; color: var(--gray-500);">ID: {{ app.patientId || String(app.id).substring(0,8) }}</p>
                    </div>
                  </div>
                </td>

                <td>{{ app.doctorName || 'Chưa phân công' }}</td>

                <td>
                  <p style="font-weight: 500;">{{ formatTime(app.time) }}</p>
                  <p style="font-size: 0.75rem; color: var(--gray-500);">{{ formatDate(app.date) }}</p>
                </td>

                <td>
                  <span v-if="app.queueNumber" style="font-weight: 800; color: var(--cobalt);">#{{ app.queueNumber }}</span>
                  <span v-else style="color: var(--gray-400);">Chưa cấp</span>
                </td>

                <td>
                  <span class="badge" :class="getStatusClass(app.status)">
                    {{ getStatusText(app.status) }}
                  </span>
                </td>

                <td>
                  <div class="action-dropdown-container">
                    <button class="btn-icon-cog" @click="toggleDropdown(app.id)">
                      <i class="fas fa-cog" />
                    </button>

                    <div v-if="activeDropdown === app.id" class="dropdown-menu">
                      <button v-if="app.status === 0" class="dropdown-item approve-item" @click="approve(app.id); activeDropdown = null">
                        <i class="fas fa-check" /> Duyệt đơn
                      </button>

                      <button v-if="app.status === 0" class="dropdown-item cancel-item" @click="cancel(app.id); activeDropdown = null">
                        <i class="fas fa-times" /> Hủy đơn
                      </button>

                      <div v-if="app.status !== 0" class="dropdown-item-info">Không có thao tác</div>
                    </div>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </section>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
  import { computed, onMounted, ref } from 'vue'
  import { appointmentService } from '@/services/appointmentService'
  import { useAuthStore } from '@/stores/authStore'

  const authStore = useAuthStore()
  const activeTab = ref('dashboard')
  const loading = ref(false)
  const searchQuery = ref('')
  const activeDropdown = ref<string | null>(null)
  
  const PORTAL_URL = import.meta.env.VITE_PORTAL_URL || `${window.location.protocol}//${window.location.hostname}:3000`

  function goBackToHub () {
    window.location.href = `${PORTAL_URL}/dashboard`
  }

  function toggleDropdown (id: string) {
    activeDropdown.value = activeDropdown.value === id ? null : id
  }

  const appointments = ref<any[]>([])

  const stats = computed(() => {
    return {
      total: appointments.value.length,
      pending: appointments.value.filter(a => a.status === 0).length,
      confirmed: appointments.value.filter(a => a.status === 1).length,
    }
  })

  const filteredAppointments = computed(() => {
    let list = [...appointments.value]
    if (activeTab.value === 'dashboard') {
      list = list.filter(a => a.status === 0)
    }

    if (searchQuery.value) {
      const q = searchQuery.value.toLowerCase()
      list = list.filter(a => 
        a.patientName?.toLowerCase().includes(q) || 
        a.doctorName?.toLowerCase().includes(q)
      )
    }

    return list.sort((a, b) => {
      if (a.status === 0 && b.status !== 0) return -1
      if (a.status !== 0 && b.status === 0) return 1
      return new Date(b.date).getTime() - new Date(a.date).getTime()
    })
  })

  async function fetchData () {
    loading.value = true
    try {
      appointments.value = await appointmentService.getAllAppointments()
    } catch (error) {
      console.error('Failed to fetch appointments:', error)
    } finally {
      loading.value = false
    }
  }

  async function approve (id: string) {
    if (!confirm('Xác nhận duyệt lịch hẹn và cấp số thứ tự?')) return
    try {
      await appointmentService.approveAppointment(id)
      await fetchData()
      alert('Đã duyệt và cấp số thứ tự thành công!')
    } catch (error) {
      alert('Lỗi khi duyệt đơn')
    }
  }

  async function cancel (id: string) {
    if (!confirm('Bạn có chắc chắn muốn hủy lịch hẹn này?')) return
    try {
      await appointmentService.cancelAppointment(id)
      await fetchData()
      alert('Đã hủy lịch hẹn.')
    } catch {
      alert('Lỗi khi hủy đơn.')
    }
  }

  function getStatusText (status: number) {
    switch (status) {
      case 0: return 'Chờ duyệt'
      case 1: return 'Đã duyệt'
      case 2: return 'Đã hủy'
      default: return 'Không xác định'
    }
  }

  function getStatusClass (status: number) {
    switch (status) {
      case 0: return 'badge--pending'
      case 1: return 'badge--confirmed'
      case 2: return 'badge--cancelled'
      default: return ''
    }
  }

  function formatDate (dateStr: string) {
    if (!dateStr) return ''
    return new Date(dateStr).toLocaleDateString('vi-VN')
  }

  function formatTime (timeStr: string) {
    if (!timeStr) return '--:--'
    return timeStr.slice(0, 5)
  }

  onMounted(() => {
    fetchData()
  })
</script>

<style scoped>
@import '@/styles/receptionist.css';
</style>
