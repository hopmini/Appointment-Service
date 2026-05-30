<template>
  <div class="dashboard-layout">
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
        <div class="sidebar__divider">QUẢN TRỊ CHIẾN LƯỢC</div>

        <div class="nav-item" :class="{ 'nav-item--active': activeTab === 'overview' }" @click="activeTab = 'overview'">
          <span class="nav-icon"><i class="fas fa-chart-line" /></span>
          <span>Dashboard Lịch Khám</span>
        </div>

        <div class="sidebar__divider">QUẢN LÝ VẬN HÀNH</div>

        <div class="nav-item" :class="{ 'nav-item--active': activeTab === 'appointments' }" @click="activeTab = 'appointments'">
          <span class="nav-icon"><i class="fas fa-calendar-check" /></span>
          <span>Lịch hẹn</span>
        </div>

        <div class="nav-item" :class="{ 'nav-item--active': activeTab === 'duty-schedule' }" @click="activeTab = 'duty-schedule'">
          <span class="nav-icon"><i class="fas fa-clock" /></span>
          <span>Lịch trực</span>
        </div>

        <template v-if="authStore.isAdmin">
          <div class="sidebar__divider">CẤU HÌNH HỆ THỐNG</div>

          <div class="nav-item" :class="{ 'nav-item--active': activeTab === 'manage-doctors' }" @click="activeTab = 'manage-doctors'">
            <span class="nav-icon"><i class="fas fa-hospital-user" /></span>
            <span>Bác sĩ</span>
          </div>

          <div class="nav-item" :class="{ 'nav-item--active': activeTab === 'manage-services' }" @click="activeTab = 'manage-services'">
            <span class="nav-icon"><i class="fas fa-tools" /></span>
            <span>Dịch vụ</span>
          </div>
        </template>

        <div class="sidebar__divider">LIÊN THÔNG HỆ THỐNG</div>

        <div class="nav-item back-to-hub-btn" @click="goBackToHub">
          <span class="nav-icon"><i class="fas fa-home" /></span>
          <span>Về Cổng Trung Tâm</span>
        </div>
      </nav>

      <div class="sidebar__footer">
        <div v-if="authStore.user" class="user-brief">
          <div class="avatar-small">{{ (authStore.user.username || authStore.user.fullName || 'U').substring(0, 2).toUpperCase() }}</div>

          <div class="user-brief-info">
            <p class="user-brief-name">{{ authStore.user.fullName }}</p>
            <p class="user-brief-role">{{ authStore.user.role }}</p>
          </div>
        </div>

        <div class="nav-item logout-btn" @click="authStore.logout()">
          <span class="nav-icon"><i class="fas fa-sign-out-alt" /></span>
          <span>Đăng xuất</span>
        </div>
      </div>
    </aside>

    <!-- MAIN CONTENT -->
    <main class="main-content">
      <header class="top-bar">
        <div class="page-context">
          <h1 class="page-title">{{ pageTitle }}</h1>
        </div>

        <div class="top-bar-actions">
          <div class="search-box">
            <i class="fas fa-search" style="margin-left: 1rem; color: #94a3b8;" />
            <input v-model="searchQuery" placeholder="Tìm kiếm nhanh..." type="text">
          </div>
        </div>
      </header>

      <div class="page-container">
        <!-- TAB: OVERVIEW (STRATEGIC) -->
        <section v-if="activeTab === 'overview'" class="dashboard-content dashboard-content--overview">
          <div class="section-header" style="margin-bottom: 0;">
            <h2 class="section-title">Phân tích hệ thống real-time</h2>

            <button class="btn-primary" :disabled="loading" @click="fetchData">
              <i class="fas fa-sync-alt" :class="{ 'fa-spin': loading }" /> Cập nhật số liệu
            </button>
          </div>

          <!-- KPI STATS -->
          <div class="stats-grid animate-fade-in">
            <div class="stat-card">
              <div class="stat-card__info">
                <p class="stat-card__label">Tổng Lượt Khám</p>
                <h3 class="stat-card__value">{{ strategicStats?.kpis?.totalAppointments || 0 }}</h3>
                <p class="stat-card__trend up">Tích lũy từ hệ thống</p>
              </div>
              <div class="stat-card__icon"><i class="fas fa-notes-medical" /></div>
            </div>

            <div class="stat-card">
              <div class="stat-card__info">
                <p class="stat-card__label">Đơn Chờ Duyệt</p>
                <h3 class="stat-card__value">{{ strategicStats?.kpis?.pendingAppointments || 0 }}</h3>
                <p class="stat-card__trend warning">Cần lễ tân phê duyệt</p>
              </div>
              <div class="stat-card__icon" style="background:#fef3c7; color:#d97706"><i class="fas fa-hourglass-half" /></div>
            </div>

            <div class="stat-card">
              <div class="stat-card__info">
                <p class="stat-card__label">Bác sĩ Hoạt Động</p>
                <h3 class="stat-card__value">{{ strategicStats?.kpis?.activeDoctors || 0 }}</h3>
                <p class="stat-card__trend up">Có lịch trực trong tuần</p>
              </div>
              <div class="stat-card__icon" style="background:#ecfdf5; color:#059669"><i class="fas fa-user-md" /></div>
            </div>

            <div class="stat-card">
              <div class="stat-card__info">
                <p class="stat-card__label">Doanh Thu Viện Phí</p>
                <h3 class="stat-card__value">{{ formatCurrency(strategicStats?.kpis?.totalRevenue || 0) }}</h3>
                <p class="stat-card__trend up">Giao dịch thành công</p>
              </div>
              <div class="stat-card__icon" style="background:#fef2f2; color:#dc2626"><i class="fas fa-wallet" /></div>
            </div>
          </div>

          <!-- CHARTS GRID -->
          <div class="dashboard-main-grid">
            <div class="chart-container main-chart">
              <div class="chart-header">
                <h3>Xu Hướng Đặt Lịch (7 ngày qua)</h3>
                <div class="chart-actions">
                  <span class="dot trend-dot" /> <small>Lịch hẹn</small>
                </div>
              </div>

              <div class="canvas-wrapper">
                <canvas id="trendChart" />
              </div>
            </div>

            <div class="side-panels">
              <div class="top-services-panel">
                <div class="panel-header">
                  <h3>Chuyên Khoa Hot</h3>
                </div>

                <div class="service-list">
                  <div v-for="svc in strategicStats?.topServices || []" :key="svc.serviceName" class="service-stat-item">
                    <div class="svc-info">
                      <span class="svc-name">{{ svc.serviceName }}</span>
                      <span class="svc-count">{{ svc.bookingCount }} lượt</span>
                    </div>

                    <div class="svc-progress-bg">
                      <div class="svc-progress-bar" :style="{ width: `${(svc.bookingCount / (strategicStats?.kpis?.totalAppointments || 1)) * 100}%` }" />
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- TAB: APPOINTMENTS -->
        <section v-else-if="activeTab === 'appointments'" class="dashboard-content">
          <div class="filter-bar animate-fade-in">
            <div class="filter-group">
              <i class="fas fa-filter" style="color: #64748b;" />

              <select v-model="statusFilter" class="filter-select">
                <option value="all">Tất cả trạng thái</option>
                <option :value="0">Chờ duyệt</option>
                <option :value="1">Đã duyệt</option>
                <option :value="2">Đã hủy</option>
              </select>
            </div>

            <div class="filter-group">
              <i class="fas fa-calendar-day" style="color: #64748b;" />
              <input v-model="dateFilter" class="filter-input" type="date">
            </div>

            <button v-if="dateFilter || statusFilter !== 'all' || searchQuery" class="btn-text" @click="clearFilters">
              Xóa lọc
            </button>

            <div class="filter-stats">
              Tìm thấy <b>{{ filteredAppointments.length }}</b> kết quả
            </div>
          </div>

          <table class="data-table animate-zoom-in">
            <thead>
              <tr>
                <th>Mã đơn</th>
                <th>Bệnh nhân</th>
                <th>Bác sĩ</th>
                <th>Dịch vụ</th>
                <th>Ngày khám</th>
                <th style="text-align: center;">STT</th>
                <th>Trạng thái</th>
                <th style="text-align: center;">Thao tác</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="app in filteredAppointments" :key="app.id">
                <td><code style="font-weight: 700;">#{{ String(app.id).padStart(5, '0') }}</code></td>
                <td>
                  <div class="user-cell">
                    <div class="avatar-cell">{{ app.patientName?.substring(0,2).toUpperCase() }}</div>

                    <div>
                      <div style="font-weight: 600;">{{ app.patientName }}</div>
                      <small style="color: #64748b;">{{ app.patientPhone }}</small>
                    </div>
                  </div>
                </td>

                <td>
                  <div class="doctor-name">{{ app.doctorName }}</div>
                </td>

                <td>
                  <span class="table-text-truncate">{{ app.serviceName }}</span>
                </td>

                <td>
                  <div>{{ formatDate(app.appointmentDate) }}</div>
                  <small style="color: #0047AB; font-weight: 600;">{{ formatTime(app.startTime) }} - {{ formatTime(app.endTime) }}</small>
                </td>

                <td style="text-align: center;">
                  <span class="queue-number">#{{ app.queueNumber }}</span>
                </td>

                <td>
                  <span class="badge" :class="getStatusClass(app.status)">
                    {{ getStatusText(app.status) }}
                  </span>
                </td>

                <td style="text-align: center;">
                  <div class="action-btns" style="justify-content: center;">
                    <button
                      v-if="app.status === 0"
                      class="btn-icon btn-icon--check"
                      title="Duyệt lịch hẹn"
                      @click="approveAppointment(app.id)"
                    >
                      <i class="fas fa-check" />
                    </button>

                    <button
                      v-if="app.status !== 2"
                      class="btn-icon btn-icon--close"
                      title="Hủy lịch hẹn"
                      @click="cancelAppointment(app.id)"
                    >
                      <i class="fas fa-times" />
                    </button>
                  </div>
                </td>
              </tr>

              <tr v-if="filteredAppointments.length === 0">
                <td colspan="8" style="text-align: center; padding: 3rem; color: #94a3b8;">
                  <i class="fas fa-calendar-times" style="font-size: 2rem; margin-bottom: 1rem; display: block;" />
                  Không có lịch hẹn nào khớp với điều kiện lọc
                </td>
              </tr>
            </tbody>
          </table>
        </section>

        <!-- TAB: DUTY SCHEDULE -->
        <section v-else-if="activeTab === 'duty-schedule'" class="dashboard-content">
          <div class="section-header" style="margin-bottom: 1.5rem;">
            <h2 class="section-title">Phân ca trực và tạo Slot khám</h2>

            <button class="btn-primary" @click="openScheduleModal">
              <i class="fas fa-plus" /> Phân ca trực mới
            </button>
          </div>

          <div class="grid-list animate-zoom-in">
            <div v-for="sch in dutySchedules" :key="sch.doctorId" class="user-card">
              <div class="user-card-avatar" style="color: #0047AB; background: #eff6ff;">
                <i class="fas fa-user-md" />
              </div>

              <h3 style="font-weight: 700; color: #0f172a; margin-bottom: 0.25rem;">BS. {{ sch.doctorName }}</h3>
              <p style="color: #64748b; font-size: 0.85rem; margin-bottom: 1rem;">{{ sch.specialty }}</p>

              <div style="width: 100%; border-top: 1px solid #f1f5f9; padding-top: 1rem;">
                <div style="display: flex; justify-content: space-between; font-size: 0.85rem; margin-bottom: 0.5rem;">
                  <span style="color:#64748b">Ca trực hôm nay:</span>
                  <strong style="color:#0f172a">{{ sch.slots?.length || 0 }} khung giờ</strong>
                </div>

                <div style="display: flex; justify-content: space-between; font-size: 0.85rem;">
                  <span style="color:#64748b">Trạng thái:</span>
                  <strong style="color: #047857;">Đang hoạt động</strong>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- TAB: MANAGE DOCTORS -->
        <section v-else-if="activeTab === 'manage-doctors' && authStore.isAdmin" class="dashboard-content">
          <div class="section-header" style="margin-bottom: 1.5rem;">
            <h2 class="section-title">Danh sách Bác sĩ chuyên khoa</h2>

            <button class="btn-primary" @click="openDoctorModal()">
              <i class="fas fa-plus" /> Thêm bác sĩ
            </button>
          </div>

          <div class="grid-list animate-zoom-in">
            <div v-for="doc in systemDoctors" :key="doc.id" class="user-card">
              <div class="user-card-avatar">
                <i class="fas fa-user-md" />
              </div>

              <h3 style="font-weight: 700; color: #0f172a; margin-bottom: 0.25rem;">{{ doc.fullName }}</h3>
              <p style="color: #0047AB; font-size: 0.85rem; font-weight: 600; margin-bottom: 0.25rem;">{{ doc.specialty }}</p>
              <p style="color: #64748b; font-size: 0.8rem; margin-bottom: 1rem;">Học vị: {{ doc.degree }}</p>

              <div style="width: 100%; border-top: 1px solid #f1f5f9; padding-top: 1rem; margin-bottom: 1rem;">
                <div style="display: flex; justify-content: space-between; font-size: 0.85rem;">
                  <span style="color:#64748b">Phí khám:</span>
                  <strong style="color:#e53935">{{ formatCurrency(doc.consultationFee) }}</strong>
                </div>
              </div>

              <div class="action-btns" style="width: 100%; justify-content: center;">
                <button class="btn-icon" title="Chỉnh sửa" @click="openDoctorModal(doc)">
                  <i class="fas fa-edit" />
                </button>

                <button class="btn-icon btn-icon--close" title="Xóa bác sĩ" @click="deleteDoctor(doc.id)">
                  <i class="fas fa-trash-alt" />
                </button>
              </div>
            </div>
          </div>
        </section>

        <!-- TAB: MANAGE SERVICES -->
        <section v-else-if="activeTab === 'manage-services' && authStore.isAdmin" class="dashboard-content">
          <div class="section-header" style="margin-bottom: 1.5rem;">
            <h2 class="section-title">Danh mục dịch vụ khám y khoa</h2>

            <button class="btn-primary" @click="openServiceModal()">
              <i class="fas fa-plus" /> Thêm dịch vụ
            </button>
          </div>

          <table class="data-table animate-zoom-in">
            <thead>
              <tr>
                <th>Mã DV</th>
                <th>Tên dịch vụ y khoa</th>
                <th>Nhóm danh mục</th>
                <th>Mô tả chuyên khoa</th>
                <th>Giá niêm yết</th>
                <th style="text-align: center;">Thao tác</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="svc in medicalServices" :key="svc.id">
                <td><code style="font-weight: 700;">#{{ String(svc.id).padStart(3, '0') }}</code></td>
                <td><div style="font-weight: 700; color: #0f172a;">{{ svc.name }}</div></td>
                <td><span class="badge badge--confirmed">{{ svc.category || 'Khám bệnh' }}</span></td>
                <td><div class="table-text-truncate" style="max-width: 320px;">{{ svc.description }}</div></td>
                <td><strong style="color: #e53935;">{{ formatCurrency(svc.price) }}</strong></td>
                <td style="text-align: center;">
                  <div class="action-btns" style="justify-content: center;">
                    <button class="btn-icon" title="Chỉnh sửa" @click="openServiceModal(svc)">
                      <i class="fas fa-edit" />
                    </button>

                    <button class="btn-icon btn-icon--close" title="Xóa dịch vụ" @click="deleteService(svc.id)">
                      <i class="fas fa-trash-alt" />
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </section>
      </div>
    </main>

    <!-- SCHEDULE MODAL -->
    <div v-if="showScheduleModal" class="modal-overlay">
      <div class="modal-box">
        <div class="modal-header">
          <h3>Phân ca trực và sinh Slot khám tự động</h3>
          <button class="modal-close" @click="showScheduleModal = false">&times;</button>
        </div>

        <div class="modal-body">
          <div class="form-group">
            <label>Chọn Bác sĩ trực</label>
            <select v-model="scheduleForm.doctorId" class="form-input">
              <option value="">-- Chọn bác sĩ --</option>
              <option v-for="d in systemDoctors" :key="d.id" :value="d.id">{{ d.fullName }} ({{ d.specialty }})</option>
            </select>
          </div>

          <div class="form-group">
            <label>Chọn Ngày trực</label>
            <input v-model="scheduleForm.date" class="form-input" type="date" :min="todayIso">
          </div>
        </div>

        <div class="modal-footer">
          <button class="btn-secondary" @click="showScheduleModal = false">Hủy</button>
          <button class="btn-primary" :disabled="loadingSchedule" @click="saveSchedule">
            <i class="fas fa-magic" /> {{ loadingSchedule ? 'Đang tạo ca trực...' : 'Xác nhận và Tạo slot khám' }}
          </button>
        </div>
      </div>
    </div>

    <!-- DOCTOR MODAL -->
    <div v-if="showDoctorModal" class="modal-overlay">
      <div class="modal-box">
        <div class="modal-header">
          <h3>{{ editingDoctor ? 'Cập nhật thông tin bác sĩ' : 'Đăng ký Bác sĩ mới' }}</h3>
          <button class="modal-close" @click="showDoctorModal = false">&times;</button>
        </div>

        <div class="modal-body">

          <div class="form-group">
            <label>Họ và tên bác sĩ</label>
            <input v-model="doctorForm.fullName" class="form-input" type="text" placeholder="Nhập tên bác sĩ...">
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>Chuyên khoa chính</label>
              <select v-model="doctorForm.specialty" class="form-input">
                <option value="Nội tổng quát">Nội tổng quát</option>
                <option value="Tim mạch">Tim mạch</option>
                <option value="Tai Mũi Họng">Tai Mũi Họng</option>
                <option value="Nhãn khoa">Nhãn khoa</option>
                <option value="Thần kinh">Thần kinh</option>
              </select>
            </div>

            <div class="form-group">
              <label>Học hàm / Học vị</label>
              <input v-model="doctorForm.degree" class="form-input" type="text" placeholder="ThS. BS / PGS. TS...">
            </div>
          </div>

          <div class="form-group">
            <label>Phí khám tư vấn (VNĐ)</label>
            <input v-model="doctorForm.consultationFee" class="form-input" type="number">
          </div>
        </div>

        <div class="modal-footer">
          <button class="btn-secondary" @click="showDoctorModal = false">Hủy</button>
          <button class="btn-primary" @click="saveDoctor">Lưu thông tin</button>
        </div>
      </div>
    </div>

    <!-- SERVICE MODAL -->
    <div v-if="showServiceModal" class="modal-overlay">
      <div class="modal-box">
        <div class="modal-header">
          <h3>{{ editingService ? 'Cập nhật dịch vụ y khoa' : 'Thêm dịch vụ khám mới' }}</h3>
          <button class="modal-close" @click="showServiceModal = false">&times;</button>
        </div>

        <div class="modal-body">
          <div class="form-group">
            <label>Tên dịch vụ y khoa</label>
            <input v-model="serviceForm.name" class="form-input" type="text" placeholder="Nhập tên dịch vụ...">
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>Nhóm chuyên môn</label>
              <select v-model="serviceForm.category" class="form-input">
                <option value="Khám bệnh">Khám bệnh</option>
                <option value="Xét nghiệm">Xét nghiệm</option>
                <option value="Chẩn đoán hình ảnh">Chẩn đoán hình ảnh</option>
                <option value="Nha khoa">Nha khoa</option>
              </select>
            </div>

            <div class="form-group">
              <label>Mô tả tóm tắt</label>
              <input v-model="serviceForm.description" class="form-input" type="text" placeholder="Mô tả dịch vụ...">
            </div>
          </div>

          <div class="form-group">
            <label>Giá niêm yết (VNĐ)</label>
            <input v-model="serviceForm.price" class="form-input" type="number">
          </div>
        </div>

        <div class="modal-footer">
          <button class="btn-secondary" @click="showServiceModal = false">Hủy</button>
          <button class="btn-primary" @click="saveService">Lưu thông tin</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
  import api from '@/services/api'
  import { appointmentService } from '@/services/appointmentService'
  import { doctorService } from '@/services/doctorService'
  import { medicalServiceApi } from '@/services/medicalService'
  import { useAuthStore } from '@/stores/authStore'

  const authStore = useAuthStore()
  const activeTab = ref('overview')
  const loading = ref(false)
  const searchQuery = ref('')
  const statusFilter = ref<number | string>('all')
  const dateFilter = ref('')
  const appointments = ref<any[]>([])
  const doctorUsers = ref<any[]>([])
  const systemDoctors = ref<any[]>([])
  const dutySchedules = ref<any[]>([])
  const medicalServices = ref<any[]>([])
  const scheduleForm = ref({ doctorId: '', date: '' })
  const loadingSchedule = ref(false)
  const now = new Date()
  const todayIso = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`

  const PORTAL_URL = import.meta.env.VITE_PORTAL_URL || `${window.location.protocol}//${window.location.hostname}:3000`

  // Strategic Stats State
  const strategicStats = ref<any>(null)
  let trendChartInstance: any = null

  const showScheduleModal = ref(false)
  const showDoctorModal = ref(false)
  const showServiceModal = ref(false)

  const editingDoctor = ref<any>(null)
  const doctorForm = ref({ userId: '', fullName: '', specialty: 'Nội tổng quát', degree: '', consultationFee: 0 })

  const editingService = ref<any>(null)
  const serviceForm = ref({ name: '', description: '', price: 0, category: 'Khám bệnh' })

  const pageTitle = computed(() => {
    const titles: Record<string, string> = {
      'overview': 'Dashboard Lịch Khám',
      'appointments': 'Quản lý Lịch hẹn',
      'duty-schedule': 'Lịch trực hôm nay',
      'manage-doctors': 'Cài đặt Bác sĩ',
      'manage-services': 'Cài đặt Dịch vụ',
    }
    return titles[activeTab.value] || 'Medicare Administration'
  })

  function goBackToHub () {
    window.location.href = `${PORTAL_URL}/dashboard`
  }

  const filteredAppointments = computed(() => {
    return appointments.value.filter(app => {
      const matchStatus = statusFilter.value === 'all' || app.status === Number(statusFilter.value)
      const matchDate = !dateFilter.value || app.appointmentDate.split('T')[0] === dateFilter.value
      const matchSearch = !searchQuery.value ||
        app.patientName?.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
        app.patientPhone?.includes(searchQuery.value) ||
        app.doctorName?.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
        app.serviceName?.toLowerCase().includes(searchQuery.value.toLowerCase())
      return matchStatus && matchDate && matchSearch
    })
  })

  function clearFilters () {
    statusFilter.value = 'all'
    dateFilter.value = ''
    searchQuery.value = ''
  }

  async function fetchData () {
    loading.value = true
    try {
      // 1. Load Strategic Stats
      const statsRes = await api.get('/Statistics/overview')
      strategicStats.value = statsRes.data
      nextTick(() => initCharts())

      // 2. Load Appointments
      appointments.value = await appointmentService.getAllAppointments()

      // 3. Load Doctors and Services
      systemDoctors.value = await doctorService.getAllDoctors()
      medicalServices.value = await medicalServiceApi.getAllServices()

      // 4. Load Duty Schedules
      const dutyRes = await api.get('/Doctors/duty-schedule')
      dutySchedules.value = dutyRes.data

      // 5. Load Available Doctor Accounts (roles containing Doctor and not linked yet)
      const usersRes = await api.get('/Users/doctors')
      doctorUsers.value = usersRes.data.filter((u: any) => !systemDoctors.value.some(d => d.userId === u.id))
    } catch (error) {
      console.error('Lỗi tải dữ liệu admin:', error)
    } finally {
      loading.value = false
    }
  }

  function initCharts () {
    if (!strategicStats.value) return

    const trendCtx = document.querySelector('#trendChart') as HTMLCanvasElement
    if (trendCtx) {
      if (trendChartInstance) trendChartInstance.destroy()
      trendChartInstance = new (window as any).Chart(trendCtx, {
        type: 'line',
        data: {
          labels: strategicStats.value.trends.map((t: any) => t.label),
          datasets: [{
            label: 'Lượt hoạt động',
            data: strategicStats.value.trends.map((t: any) => t.value),
            borderColor: '#0047AB',
            backgroundColor: 'rgba(0, 71, 171, 0.1)',
            fill: true,
            tension: 0.4,
            borderWidth: 3,
            pointBackgroundColor: '#0047AB',
            pointRadius: 4,
          }],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { display: false } },
          scales: {
            y: { beginAtZero: true, grid: { display: false } },
            x: { grid: { display: false } },
          },
        },
      })
    }
  }

  // Operation Actions
  async function approveAppointment (id: string) {
    if (confirm('Xác nhận duyệt lịch hẹn khám này? Bệnh nhân sẽ nhận được Số thứ tự hàng đợi.')) {
      try {
        await appointmentService.approveAppointment(id)
        await fetchData()
      } catch (err) {
        alert('Lỗi phê duyệt lịch hẹn')
      }
    }
  }

  async function cancelAppointment (id: string) {
    if (confirm('Bạn có chắc chắn muốn hủy lịch hẹn khám này không? Hành động này không thể hoàn tác.')) {
      try {
        await appointmentService.cancelAppointment(id)
        await fetchData()
      } catch (err) {
        alert('Lỗi hủy lịch hẹn')
      }
    }
  }

  function openScheduleModal () {
    scheduleForm.value = { doctorId: '', date: '' }
    showScheduleModal.value = true
  }

  async function saveSchedule () {
    if (!scheduleForm.value.doctorId || !scheduleForm.value.date) {
      alert('Vui lòng chọn đầy đủ bác sĩ và ngày trực')
      return
    }
    loadingSchedule.value = true
    try {
      await appointmentService.generateSlots(scheduleForm.value.doctorId, scheduleForm.value.date)
      showScheduleModal.value = false
      await fetchData()
      alert('Đã phân ca trực và sinh slots thành công!')
    } catch {
      alert('Lỗi phân lịch trực & sinh slots')
    } finally {
      loadingSchedule.value = false
    }
  }

  function openDoctorModal (doc?: any) {
    if (doc) {
      editingDoctor.value = doc
      doctorForm.value = {
        userId: doc.userId || '',
        fullName: doc.fullName,
        specialty: doc.specialty,
        degree: doc.degree,
        consultationFee: doc.consultationFee
      }
    } else {
      editingDoctor.value = null
      doctorForm.value = { userId: '', fullName: '', specialty: 'Nội tổng quát', degree: '', consultationFee: 150000 }
    }
    showDoctorModal.value = true
  }

  async function saveDoctor () {
    if (!doctorForm.value.fullName || !doctorForm.value.degree) {
      alert('Vui lòng nhập đầy đủ thông tin bác sĩ')
      return
    }
    try {
      if (editingDoctor.value) {
        await doctorService.updateDoctor(editingDoctor.value.id, doctorForm.value)
      } else {
        await doctorService.createDoctor(doctorForm.value)
      }
      showDoctorModal.value = false
      await fetchData()
    } catch {
      alert('Lỗi lưu thông tin bác sĩ')
    }
  }

  async function deleteDoctor (id: string) {
    if (confirm('Xác nhận xóa bác sĩ này khỏi hệ thống?')) {
      try {
        await doctorService.deleteDoctor(id)
        await fetchData()
      } catch {
        alert('Lỗi xóa bác sĩ')
      }
    }
  }

  function openServiceModal (svc?: any) {
    if (svc) {
      editingService.value = svc
      serviceForm.value = {
        name: svc.name,
        description: svc.description,
        price: svc.price,
        category: svc.category || 'Khám bệnh'
      }
    } else {
      editingService.value = null
      serviceForm.value = { name: '', description: '', price: 100000, category: 'Khám bệnh' }
    }
    showServiceModal.value = true
  }

  async function saveService () {
    if (!serviceForm.value.name) {
      alert('Vui lòng nhập tên dịch vụ y khoa')
      return
    }
    try {
      if (editingService.value) {
        await medicalServiceApi.updateService(editingService.value.id, serviceForm.value)
      } else {
        await medicalServiceApi.createService(serviceForm.value)
      }
      showServiceModal.value = false
      await fetchData()
    } catch {
      alert('Lỗi lưu dịch vụ')
    }
  }

  async function deleteService (id: string) {
    if (confirm('Xác nhận xóa dịch vụ y khoa này?')) {
      try {
        await medicalServiceApi.deleteService(id)
        await fetchData()
      } catch {
        alert('Lỗi xóa dịch vụ')
      }
    }
  }

  const formatCurrency = (v: number) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(v)
  function formatDate (d: string | Date) {
    const dateObj = typeof d === 'string' ? new Date(d) : d
    return dateObj.toLocaleDateString('vi-VN')
  }
  const formatTime = (t: string) => t?.slice(0, 5) || '--:--'
  const getStatusText = (s: number) => s === 0 ? 'Chờ duyệt' : (s === 1 ? 'Đã duyệt' : 'Đã hủy')
  const getStatusClass = (s: number) => s === 0 ? 'badge--pending' : (s === 1 ? 'badge--confirmed' : 'badge--cancelled')

  watch(activeTab, (newTab) => {
    if (newTab === 'overview') {
      nextTick(() => initCharts())
    }
  })

  onMounted(() => {
    fetchData()
  })
</script>

<style src="@/styles/dashboard.css"></style>
<style src="@/styles/notif.css"></style>

<style scoped>
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(15, 23, 42, 0.4);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}
.modal-box {
  background: white;
  border-radius: 20px;
  width: 500px;
  max-width: 90%;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  border: 1px solid #e2e8f0;
}
.modal-header {
  padding: 1.25rem 1.5rem;
  border-bottom: 1px solid #f1f5f9;
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.modal-header h3 {
  font-weight: 700;
  color: #0f172a;
}
.modal-close {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: #94a3b8;
}
.modal-body {
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}
.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
.form-group label {
  font-size: 0.85rem;
  font-weight: 600;
  color: #475569;
}
.form-input {
  padding: 0.75rem 1rem;
  border: 1px solid #cbd5e1;
  border-radius: 10px;
  font-size: 0.9rem;
  outline: none;
}
.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}
.modal-footer {
  padding: 1rem 1.5rem;
  border-top: 1px solid #f1f5f9;
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  background: #f8fafc;
}
.btn-primary {
  background: #0047AB;
  color: white;
  border: none;
  padding: 0.75rem 1.25rem;
  border-radius: 10px;
  font-weight: 600;
  cursor: pointer;
}
.btn-primary:hover {
  background: #003580;
}
.btn-secondary {
  background: white;
  border: 1px solid #cbd5e1;
  padding: 0.75rem 1.25rem;
  border-radius: 10px;
  font-weight: 600;
  cursor: pointer;
}
.back-to-hub-btn {
  color: #0047AB;
  font-weight: 600;
}
.back-to-hub-btn:hover {
  background: #eff6ff;
  color: #1e40af;
}
</style>
