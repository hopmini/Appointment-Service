<template>
  <nav class="navbar" :class="{ 'navbar--scrolled': scrolled }">
    <div class="nav-container">
      <div class="logo" @click="$router.push('/')">
        <div class="logo__icon">
          <svg fill="none" viewBox="0 0 32 32" xmlns="http://www.w3.org/2000/svg" style="height: 32px; width: 32px;">
            <rect fill="#0047AB" height="32" rx="8" width="32" />
            <path d="M16 6v20M6 16h20" stroke="white" stroke-linecap="round" stroke-width="4" />
          </svg>
        </div>
        <div class="logo__text">Medicare<span class="logo__dot">.</span></div>
      </div>

      <ul class="nav-links">
        <li><router-link to="/">Trang chủ</router-link></li>
        <li>
          <span class="dropdown">
            <span class="dropdown-toggle">Dịch vụ <i class="fas fa-chevron-down" /></span>
            <div class="dropdown-menu">
              <router-link to="/patient"><i class="fas fa-calendar-check" /> Đặt lịch khám</router-link>
              <a :href="MEDICAL_RECORD_URL" target="_blank"><i class="fas fa-file-medical" /> Hồ sơ Bệnh án</a>
              <a :href="PHARMACY_URL" target="_blank"><i class="fas fa-pills" /> Tra cứu Hóa đơn & Thuốc</a>
            </div>
          </span>
        </li>
        <li><router-link to="/track">Tra cứu lịch hẹn</router-link></li>
        <li><router-link to="/contact">Liên hệ</router-link></li>
      </ul>

      <div class="nav-actions">
        <template v-if="authStore.isAuthenticated">
          <span class="user-badge-nav">{{ authStore.user?.fullName || authStore.user?.username }}</span>
          <button v-if="authStore.canAccessDashboard" class="btn-outline-nav" @click="goToPortal('/dashboard')">
            <i class="fas fa-tachometer-alt" /> Dashboard
          </button>
          <button v-else-if="authStore.canAccessDoctorDashboard" class="btn-outline-nav" @click="goToPortal('/doctor')">
            <i class="fas fa-user-md" /> Bác sĩ
          </button>
          <button class="btn-logout" @click="authStore.logout()">
            <i class="fas fa-sign-out-alt" /> Đăng xuất
          </button>
        </template>
        <template v-else>
          <button class="btn-outline-nav" @click="goToPortal('/login')">
            <i class="fas fa-sign-in-alt" /> Đăng nhập
          </button>
          <button class="btn-outline-nav" @click="goToPortal('/register')">
            <i class="fas fa-user-plus" /> Đăng ký
          </button>
        </template>
      </div>

      <button class="hamburger" :class="{ 'hamburger--open': mobileOpen }" @click="mobileOpen = !mobileOpen">
        <span /><span /><span />
      </button>
    </div>

    <div class="mobile-menu" :class="{ 'mobile-menu--open': mobileOpen }">
      <router-link to="/" @click="mobileOpen = false">Trang chủ</router-link>
      <router-link to="/patient" @click="mobileOpen = false"><i class="fas fa-calendar-check" /> Đặt lịch khám</router-link>
      <a :href="MEDICAL_RECORD_URL" target="_blank" @click="mobileOpen = false"><i class="fas fa-file-medical" /> Bệnh án điện tử</a>
      <a :href="PHARMACY_URL" target="_blank" @click="mobileOpen = false"><i class="fas fa-pills" /> Hóa đơn & Thuốc</a>
      <router-link to="/track" @click="mobileOpen = false">Tra cứu lịch hẹn</router-link>
      <router-link to="/contact" @click="mobileOpen = false">Liên hệ</router-link>

      <div class="mobile-menu__actions">
        <template v-if="authStore.isAuthenticated">
          <span class="mobile-user-name">{{ authStore.user?.fullName }}</span>
          <button v-if="authStore.canAccessDashboard" class="btn-mobile-login" @click="goToPortal('/dashboard'); mobileOpen = false">
            <i class="fas fa-tachometer-alt" /> Dashboard
          </button>
          <button v-else-if="authStore.canAccessDoctorDashboard" class="btn-mobile-login" @click="goToPortal('/doctor'); mobileOpen = false">
            <i class="fas fa-user-md" /> Bác sĩ
          </button>
          <button class="btn-logout mobile-logout" @click="authStore.logout(); mobileOpen = false">
            <i class="fas fa-sign-out-alt" /> Đăng xuất
          </button>
        </template>
        <template v-else>
          <button class="btn-mobile-login" @click="goToPortal('/login'); mobileOpen = false">
            <i class="fas fa-sign-in-alt" /> Đăng nhập
          </button>
          <button class="btn-mobile-register" @click="goToPortal('/register'); mobileOpen = false">
            <i class="fas fa-user-plus" /> Đăng ký
          </button>
        </template>
      </div>
    </div>
  </nav>
</template>

<script setup lang="ts">
  import { onMounted, onUnmounted, ref } from 'vue'
  import { useAuthStore } from '@/stores/authStore'

  const authStore = useAuthStore()
  const mobileOpen = ref(false)
  const scrolled = ref(false)
  const PORTAL_URL = import.meta.env.VITE_PORTAL_URL || `${window.location.protocol}//${window.location.hostname}:3000`
  const MEDICAL_RECORD_URL = import.meta.env.VITE_MEDICAL_RECORD_URL || `http://${window.location.hostname}:8001`
  const PHARMACY_URL = import.meta.env.VITE_PHARMACY_URL || `http://${window.location.hostname}:8002`

  function goToPortal (path: string) {
    window.location.href = `${PORTAL_URL}${path}`
  }

  function handleScroll () {
    scrolled.value = window.scrollY > 50
  }

  onMounted(() => {
    window.addEventListener('scroll', handleScroll)
  })

  onUnmounted(() => {
    window.removeEventListener('scroll', handleScroll)
  })
</script>

<style scoped>
@import '@/styles/navbar.css';

.btn-outline-nav {
  padding: 0.5rem 1.1rem;
  border: 1.5px solid #0047AB;
  border-radius: 10px;
  color: #0047AB;
  background: transparent;
  font-size: 0.85rem;
  font-weight: 700;
  cursor: pointer;
  transition: 0.2s;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  white-space: nowrap;
}

.btn-outline-nav:hover {
  background: #0047AB;
  color: white;
}

.btn-logout {
  padding: 0.5rem 1.1rem;
  border: 1.5px solid #e2e8f0;
  border-radius: 10px;
  color: #64748b;
  background: transparent;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: 0.2s;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  white-space: nowrap;
}

.btn-logout:hover {
  border-color: #fecaca;
  color: #ef4444;
  background: #fef2f2;
}

.user-badge-nav {
  font-size: 0.85rem;
  font-weight: 600;
  color: #0f172a;
  padding: 0.4rem 0.8rem;
  background: #f1f5f9;
  border-radius: 8px;
}

.nav-actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.mobile-user-name {
  font-weight: 700;
  color: #0f172a;
  padding: 0.5rem 0;
  display: block;
}

.mobile-logout {
  width: 100%;
  justify-content: center;
  padding: 0.8rem;
}

.btn-mobile-login,
.btn-mobile-register {
  width: 100%;
  padding: 0.8rem;
  border-radius: 12px;
  font-weight: 700;
  font-size: 0.95rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  border: none;
}

.btn-mobile-login {
  background: #0047AB;
  color: white;
}

.btn-mobile-register {
  border: 1.5px solid #0047AB;
  color: #0047AB;
  background: transparent;
}
</style>
