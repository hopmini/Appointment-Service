<template>
  <div id="app">
    <router-view />
    <!-- Medicare AI Assistant -->
    <!-- <ChatWidget /> -->

    <!-- GLOBAL TOAST NOTIFICATIONS (FOR REAL-TIME UPDATES) -->
    <div class="global-toast-container">
      <div v-for="toast in activeToasts" :key="toast.id" class="g-toast" :class="'g-toast--' + toast.type">
        <div class="g-toast__icon">
          <i :class="toast.type === 'success' ? 'fas fa-check-circle' : 'fas fa-info-circle'" />
        </div>

        <div class="g-toast__content">
          <b>{{ toast.title }}</b>
          <p>{{ toast.message }}</p>
        </div>

        <button class="g-toast__close" @click="removeToast(toast.id)">✕</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { onUnmounted, ref, watch } from 'vue'
  import api from '@/services/api'
  import { useAuthStore } from '@/stores/authStore'

  const authStore = useAuthStore()
  const activeToasts = ref<any[]>([])
  let notifInterval: any = null

  function addToast (title: string, message: string, type = 'success') {
    const id = Date.now()
    activeToasts.value.push({ id, title, message, type })
    setTimeout(() => removeToast(id), 5000)
  }

  function removeToast (id: number) {
    activeToasts.value = activeToasts.value.filter(t => t.id !== id)
  }

  async function checkUserNotifications () {
    if (!authStore.isAuthenticated) return

    try {
      const res = await api.get('/Notifications/my')
      const unread = res.data.filter((n: any) => !n.isRead)

      // Nếu có thông báo mới (chưa đọc), hiện toast
      for (const n of unread) {
        addToast(n.title, n.message, n.type)
        // Đánh dấu đã đọc ngay để không hiện lại lần sau
        api.put(`/Notifications/${n.id}/read`)
      }
    } catch {
      console.error('Failed to check notifications')
    }
  }

  watch(() => authStore.isAuthenticated, val => {
    if (val) {
      notifInterval = setInterval(checkUserNotifications, 20_000) // 20s check 1 lần
    } else {
      if (notifInterval) clearInterval(notifInterval)
    }
  }, { immediate: true })

  onUnmounted(() => {
    if (notifInterval) clearInterval(notifInterval)
  })
</script>

<style>
/* Global styles */
#app {
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

.global-toast-container {
  position: fixed;
  top: 2rem;
  right: 2rem;
  z-index: 99999;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  pointer-events: none; /* Cho phép click xuyên qua vùng trống */
}

.g-toast {
  pointer-events: auto; /* Chỉ bật click cho chính thông báo */
  background: white;
  min-width: 300px;
  max-width: 400px;
  padding: 1.25rem;
  border-radius: 16px;
  box-shadow: 0 15px 40px rgba(0,0,0,0.15);
  display: flex;
  align-items: flex-start;
  gap: 1rem;
  border-left: 6px solid #0047AB;
  animation: toastSlideIn 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
}

.g-toast--success { border-left-color: #10b981; }

.g-toast__icon { font-size: 1.5rem; color: #0047AB; margin-top: 0.2rem; }
.g-toast--success .g-toast__icon { color: #10b981; }

.g-toast__content b { display: block; font-size: 1rem; color: #0f172a; margin-bottom: 0.25rem; }
.g-toast__content p { font-size: 0.9rem; color: #64748b; margin: 0; line-height: 1.4; }

.g-toast__close { background: none; border: none; color: #94a3b8; cursor: pointer; font-size: 1rem; }

@keyframes toastSlideIn {
  from { transform: translateX(100%); opacity: 0; }
  to { transform: translateX(0); opacity: 1; }
}
</style>
