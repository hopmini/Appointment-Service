<template>
  <div class="public-page guide-page">
    <Navbar />

    <div class="page-header">
      <div class="container">
        <h1>Hướng dẫn đặt lịch</h1>
        <p>Quy trình đơn giản giúp bạn tiết kiệm thời gian và công sức khi đi khám bệnh.</p>
      </div>
    </div>

    <section class="steps-section container">
      <div v-for="(step, i) in steps" :key="i" class="step-card">
        <div class="step-num">{{ i + 1 }}</div>

        <div class="step-content">
          <div class="step-icon"><i :class="step.icon" /></div>
          <h3>{{ step.title }}</h3>
          <p>{{ step.desc }}</p>

          <ul class="step-features">
            <li v-for="f in step.features" :key="f">{{ f }}</li>
          </ul>
        </div>
      </div>
    </section>

    <section class="faq-section container">
      <h2 class="section-title">Câu hỏi thường gặp</h2>

      <div class="faq-grid">
        <div v-for="(faq, i) in faqs" :key="i" class="faq-item">
          <h4>{{ faq.q }}</h4>
          <p>{{ faq.a }}</p>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
  import Navbar from '@/components/Navbar.vue'

  const steps = [
    {
      icon: 'fas fa-user-md',
      title: 'Bước 1: Tìm kiếm bác sĩ',
      desc: 'Truy cập danh sách bác sĩ, lọc theo chuyên khoa hoặc tìm kiếm tên bác sĩ bạn mong muốn.',
      features: ['Xem hồ sơ năng lực', 'Kiểm tra phí khám', 'Xem đánh giá thực tế'],
    },
    {
      icon: 'fas fa-calendar-check',
      title: 'Bước 2: Chọn thời gian',
      desc: 'Hệ thống hiển thị các khung giờ còn trống (30 phút/slot). Bạn chỉ cần chọn giờ phù hợp.',
      features: ['Hiển thị thời gian thực', 'Không lo trùng lịch', 'Xác nhận ngay lập tức'],
    },
    {
      icon: 'fas fa-ticket-alt',
      title: 'Bước 3: Nhận số thứ tự',
      desc: 'Sau khi đặt thành công, bạn sẽ nhận được mã lịch hẹn và số thứ tự hàng đợi qua email.',
      features: ['Theo dõi hàng đợi online', 'Thông báo qua Email', 'Hỗ trợ hủy lịch dễ dàng'],
    },
  ]

  const faqs = [
    { q: 'Tôi có cần đăng ký tài khoản không?', a: 'Không, bạn có thể đặt lịch nhanh chỉ với số điện thoại và thông tin cá nhân cơ bản.' },
    { q: 'Làm sao để tôi biết lịch hẹn đã được xác nhận?', a: 'Sau khi nhân viên tiếp tân duyệt, hệ thống sẽ gửi email xác nhận kèm số thứ tự cho bạn.' },
    { q: 'Tôi có thể đổi giờ khám sau khi đã đặt không?', a: 'Hiện tại hệ thống chỉ hỗ trợ hủy lịch cũ và đặt lại lịch mới để đảm bảo tính minh bạch của hàng đợi.' },
    { q: 'Chi phí khám bệnh nộp khi nào?', a: 'Bạn sẽ nộp phí trực tiếp tại quầy tiếp tân khi đến phòng khám theo lịch đã hẹn.' },
  ]
</script>

<style scoped>
@import '@/styles/public-pages.css';
</style>
