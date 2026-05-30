<template>
  <div class="callback-page">
    <div class="callback-card animate-zoom-in">
      <div class="spinner">
        <div class="double-bounce1"></div>
        <div class="double-bounce2"></div>
      </div>
      <h2>Đang đồng bộ phiên đăng nhập</h2>
      <p>Hệ thống đang kết nối và xác thực tài khoản của bạn từ Master Portal...</p>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { onMounted } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import { useAuthStore } from '@/stores/authStore'

  const route = useRoute()
  const router = useRouter()
  const authStore = useAuthStore()

  function mapRole (role: string): 'Patient' | 'Receptionist' | 'Doctor' | 'Admin' {
    const r = role.toLowerCase()
    if (r === 'doctor') return 'Doctor'
    if (r === 'nurse' || r === 'receptionist') return 'Receptionist'
    if (r === 'admin') return 'Admin'
    return 'Patient'
  }

  onMounted(() => {
    const token = route.query.token as string
    const userJson = route.query.user as string
    const queryRole = route.query.role as string

    if (token) {
      try {
        let userObj = null
        if (userJson) {
          userObj = JSON.parse(decodeURIComponent(userJson))
        }

        const role = mapRole(queryRole || userObj?.role || 'patient')
        const user = {
          id: userObj?.id || 1,
          username: userObj?.username || `user_${role.toLowerCase()}`,
          fullName: userObj?.fullName || `Medicare ${role}`,
          role: role
        }

        // Save to store
        authStore.token = token
        authStore.user = user

        // Save to localStorage
        localStorage.setItem('token', token)
        localStorage.setItem('user', JSON.stringify(user))

        // Small delay to make the premium animation visible
        setTimeout(() => {
          if (role === 'Patient') {
            router.push('/patient')
          } else if (role === 'Admin') {
            router.push('/dashboard')
          } else if (role === 'Receptionist') {
            router.push('/receptionist')
          } else if (role === 'Doctor') {
            router.push('/doctor')
          } else {
            router.push('/')
          }
        }, 1200)
      } catch (error) {
        console.error('Failed to sync auth callback:', error)
        router.push('/login')
      }
    } else {
      router.push('/login')
    }
  })
</script>

<style scoped>
.callback-page {
  min-height: 100vh;
  background: #f8fafc;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
  font-family: 'Roboto', sans-serif;
  color: #0f172a;
}

.callback-card {
  max-width: 450px;
  background: #ffffff;
  border: 1px solid #e2e8f0;
  padding: 40px;
  border-radius: 24px;
  text-align: center;
  box-shadow: 0 20px 40px rgba(0, 71, 171, 0.06);
}

h2 {
  font-size: 1.5rem;
  font-weight: 700;
  margin-top: 24px;
  margin-bottom: 12px;
  color: #0f172a;
}

p {
  color: #64748b;
  font-size: 0.95rem;
  line-height: 1.6;
}

/* Premium Spinner */
.spinner {
  width: 60px;
  height: 60px;
  position: relative;
  margin: 0 auto;
}

.double-bounce1, .double-bounce2 {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  background-color: #93c5fd;
  opacity: 0.6;
  position: absolute;
  top: 0;
  left: 0;
  animation: sk-bounce 2.0s infinite ease-in-out;
}

.double-bounce2 {
  animation-delay: -1.0s;
  background-color: #0047ab;
}

@keyframes sk-bounce {
  0%, 100% { 
    transform: scale(0.0);
  } 50% { 
    transform: scale(1.0);
  }
}

.animate-zoom-in {
  animation: zoomIn 0.5s cubic-bezier(0.16, 1, 0.3, 1) forwards;
}

@keyframes zoomIn {
  from { opacity: 0; transform: scale(0.95); }
  to { opacity: 1; transform: scale(1); }
}
</style>
