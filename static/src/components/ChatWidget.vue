<template>
  <div class="chat-widget">
    <!-- NÚT BẤM NỔI (STYLE MỚI VỚI FONTAWESOME) -->
    <button class="chat-toggle" @click="isOpen = !isOpen">
      <i v-if="!isOpen" class="fas fa-comment-dots" />
      <i v-else class="fas fa-times" />
    </button>

    <!-- CỬA SỔ CHAT THEO MẪU VICHAT -->
    <div v-if="isOpen" class="chat-window">
      <!-- HEADER -->
      <header class="chat-header">
        <div class="header-left">
          <div class="header-avatar">
            <i class="fas fa-robot" style="color: #3b82f6;" />
          </div>

          <div class="header-info">
            <b>MEDICARE AI</b>
            <span><i class="status-dot" /> Hoạt động</span>
          </div>
        </div>

        <div class="header-actions">
          <button class="action-btn"><i class="fas fa-expand-alt" /></button>
          <button class="action-btn" @click="isOpen = false"><i class="fas fa-times" /></button>
        </div>
      </header>

      <!-- TIN NHẮN -->
      <main ref="messageContainer" class="chat-messages">
        <div
          v-for="(msg, index) in messages"
          :key="index"
          :class="['message-wrapper', msg.role === 'user' ? 'message-wrapper--user' : 'message--bot']"
        >

          <div v-if="msg.role === 'bot'" class="msg-avatar">
            <i class="fas fa-robot" />
          </div>

          <div class="msg-bubble">
            {{ msg.content }}
          </div>
        </div>

        <!-- GỢI Ý (SUGGESTION CHIP) -->
        <div class="suggestion-chip" @click="handleSuggest('Bạn làm được gì?')">
          <i class="fas fa-lightbulb" style="color: #f59e0b;" /> Bạn làm được gì?
        </div>

        <div v-if="isTyping" class="typing-indicator" style="padding-left: 3rem;">
          <i class="fas fa-spinner fa-spin" /> Medicare AI đang soạn văn bản...
        </div>
      </main>

      <!-- FOOTER (CAPSULE STYLE) -->
      <footer class="chat-footer">
        <i class="fas fa-paperclip footer-icon" />

        <div class="input-capsule">
          <input
            v-model="userInput"
            :disabled="isTyping"
            placeholder="Nhập tin nhắn..."
            @keyup.enter="sendMessage"
          >
        </div>

        <i class="fas fa-bars footer-icon menu-icon" />
      </footer>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { nextTick, ref, watch } from 'vue'
  import api from '@/services/api'

  const isOpen = ref(false)
  const userInput = ref('')
  const isTyping = ref(false)
  const messageContainer = ref<HTMLElement | null>(null)

  const messages = ref([
    { role: 'bot', content: 'Chào bạn! Medicare AI có thể giúp gì cho bạn? Nếu bạn cần tư vấn sức khỏe, tìm bác sĩ hoặc hỏi về doanh thu (Admin), hãy nhắn cho tôi nhé!' },
  ])

  async function scrollToBottom () {
    await nextTick()
    if (messageContainer.value) {
      messageContainer.value.scrollTop = messageContainer.value.scrollHeight
    }
  }

  async function handleSuggest (text: string) {
    userInput.value = text
    sendMessage()
  }

  async function sendMessage () {
    if (!userInput.value.trim() || isTyping.value) return

    const userMsg = userInput.value.trim()
    messages.value.push({ role: 'user', content: userMsg })
    userInput.value = ''
    isTyping.value = true
    scrollToBottom()

    try {
      const res = await api.post('/Chat', { message: userMsg })
      messages.value.push({ role: 'bot', content: res.data.response })
    } catch (error) {
      console.error('Chat error:', error)
      messages.value.push({ role: 'bot', content: 'Medicare AI hiện tại đang bảo trì bộ não một chút. Bạn vui lòng thử lại sau giây lát hoặc liên hệ Hotline 1900 6789 nhé!' })
    } finally {
      isTyping.value = false
      scrollToBottom()
    }
  }

  watch(isOpen, val => {
    if (val) scrollToBottom()
  })
</script>

<style src="@/styles/chat.css"></style>
