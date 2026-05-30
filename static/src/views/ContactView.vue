<template>
  <div class="public-page contact-page">
    <Navbar />

    <div class="page-header">
      <div class="container">
        <h1>Liên hệ với chúng tôi</h1>
        <p>Chúng tôi luôn lắng nghe và sẵn sàng giải đáp mọi thắc mắc của bạn.</p>
      </div>
    </div>

    <section class="contact-content container">
      <div class="contact-grid">
        <!-- Form -->
        <div class="contact-form-box">
          <h3>Gửi tin nhắn</h3>

          <form @submit.prevent="sendMessage">
            <div class="form-group">
              <label>Họ và tên</label>
              <input placeholder="Nguyễn Văn A" required type="text">
            </div>

            <div class="form-group">
              <label>Email / Số điện thoại</label>
              <input placeholder="example@gmail.com" required type="text">
            </div>

            <div class="form-group">
              <label>Nội dung</label>
              <textarea placeholder="Bạn cần hỗ trợ điều gì?" rows="5" />
            </div>

            <button class="btn-cta btn-cta--block" type="submit">Gửi thông tin</button>
          </form>
        </div>

        <!-- Info -->
        <div class="contact-info-box">
          <div v-for="info in contactInfo" :key="info.label" class="info-item">
            <div class="info-icon"><i :class="info.icon" /></div>

            <div class="info-text">
              <p class="label">{{ info.label }}</p>
              <p class="value">{{ info.value }}</p>
            </div>
          </div>

          <div class="working-hours">
            <h4>Giờ làm việc</h4>
            <div class="hour-row"><span>Thứ 2 - Thứ 6:</span> <span>07:30 - 17:00</span></div>
            <div class="hour-row"><span>Thứ 7 - Chủ nhật:</span> <span>07:30 - 11:30</span></div>
            <p class="emergency">Cấp cứu: 24/7</p>
          </div>
        </div>
      </div>
    </section>

    <section class="map-section">
      <iframe
        allowfullscreen="true"
        height="500"
        loading="lazy"
        src="https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3724.6468453472334!2d105.83842107596853!3d21.006790988531773!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x3135ac76ccab6dd7%3A0x22136a6e76df450d!2zQuG7h25oIHZp4buHbiBC4bqhY2ggTWFp!5e0!3m2!1svi!2s!4v1715498800000!5m2!1svi!2s"
        style="border:0;"
        width="100%"
      />
    </section>
  </div>
</template>

<script setup lang="ts">
  import Navbar from '@/components/Navbar.vue'

  const contactInfo = [
    { icon: 'fas fa-map-marker-alt', label: 'Địa chỉ', value: '78 Giải Phóng, Phương Mai, Đống Đa, Hà Nội' },
    { icon: 'fas fa-phone-alt', label: 'Hotline đặt lịch', value: '1900 6789' },
    { icon: 'fas fa-envelope', label: 'Email hỗ trợ', value: 'support@medicare.vn' },
    { icon: 'fas fa-globe', label: 'Website', value: 'www.medicare.vn' },
  ]

  function sendMessage () {
    alert('Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất có thể.')
  }
</script>

<style scoped>
@import '@/styles/public-pages.css';
</style>
