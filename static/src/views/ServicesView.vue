<template>
  <div class="public-page services-page">
    <Navbar />

    <div class="page-header">
      <div class="container">
        <h1>Dịch vụ y tế</h1>
        <p>Chúng tôi cung cấp các giải pháp chăm sóc sức khỏe toàn diện với công nghệ hiện đại nhất.</p>
      </div>
    </div>

    <section class="services-content container">
      <div v-if="loading" class="loading-state">Đang tải dịch vụ...</div>

      <div v-else class="services-grid">
        <div v-for="(svc, i) in services" :key="svc.id" class="service-card">
          <div class="service-card__icon"><i :class="getIcon(svc.category)" /></div>
          <h3>{{ svc.name }}</h3>
          <p>{{ svc.description }}</p>

          <div class="service-card__footer">
            <span class="price">Từ {{ svc.price.toLocaleString('vi-VN') }}đ</span>
            <button class="btn-text" @click="$router.push('/patient')">Đặt lịch <i class="fas fa-arrow-right" /></button>
          </div>
        </div>
      </div>
    </section>

    <section class="special-packages container">
      <h2 class="section-title">Gói khám ưu đãi</h2>

      <div class="package-grid">
        <div class="package-card featured">
          <div class="badge">Phổ biến nhất</div>
          <h3>Khám Tổng Quát Premium</h3>

          <ul>
            <li>Xét nghiệm máu 24 chỉ số</li>
            <li>Siêu âm ổ bụng, tim mạch</li>
            <li>Chụp X-Quang phổi kỹ thuật số</li>
            <li>Tư vấn dinh dưỡng chuyên sâu</li>
          </ul>

          <div class="price-box">
            <span class="old-price">2.500.000đ</span>
            <span class="new-price">1.890.000đ</span>
          </div>

          <button class="btn-cta btn-cta--block" @click="$router.push('/patient')">Đăng ký ngay</button>
        </div>

        <div class="package-card">
          <h3>Tầm Soát Ung Thư</h3>

          <ul>
            <li>Xét nghiệm chỉ dấu ung thư</li>
            <li>Nội soi tiêu hóa không đau</li>
            <li>Chụp CT liều thấp</li>
          </ul>

          <div class="price-box">
            <span class="new-price">4.200.000đ</span>
          </div>

          <button class="btn-outline btn-outline--block" @click="$router.push('/patient')">Xem chi tiết</button>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
  import { onMounted, ref } from 'vue'
  import Navbar from '@/components/Navbar.vue'
  import api from '@/services/api'

  const services = ref<any[]>([])
  const loading = ref(true)

  async function fetchServices () {
    try {
      const res = await api.get('/MedicalServices')
      services.value = res.data
    } catch (error) {
      console.error('Lỗi khi tải danh sách dịch vụ:', error)
    } finally {
      loading.value = false
    }
  }

  onMounted(() => {
    fetchServices()
  })

  // Các icon mặc định dựa trên chuyên khoa
  function getIcon (category: string) {
    if (category?.includes('Nhi')) return 'fas fa-baby'
    if (category?.includes('Tim')) return 'fas fa-heartbeat'
    if (category?.includes('Mắt')) return 'fas fa-eye'
    if (category?.includes('Nha')) return 'fas fa-tooth'
    if (category?.includes('Xét nghiệm')) return 'fas fa-microscope'
    return 'fas fa-stethoscope'
  }
</script>

<style scoped>
@import '@/styles/public-pages.css';
</style>
