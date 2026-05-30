<template>
  <div class="public-page doctors-page">
    <Navbar />

    <div class="page-header">
      <div class="container">
        <h1>Đội ngũ chuyên gia</h1>
        <p>Hội tụ những bác sĩ hàng đầu, tận tâm và giàu kinh nghiệm từ các bệnh viện tuyến đầu.</p>
      </div>
    </div>

    <section class="doctors-filters container">
      <div class="filter-bar">
        <div class="search-box">
          <i class="fas fa-search" />
          <input v-model="searchQuery" placeholder="Tìm tên bác sĩ hoặc chuyên khoa..." type="text">
        </div>

        <div class="tabs">
          <button v-for="cat in categories" :key="cat" :class="{ active: activeCat === cat }" @click="activeCat = cat">
            {{ cat }}
          </button>
        </div>
      </div>
    </section>

    <section class="doctors-list container">
      <div class="doctors-grid">
        <div v-for="doc in filteredDoctors" :key="doc.id" class="doctor-card">
          <div class="doc-image">
            <i class="fas fa-user-md" />
          </div>

          <div class="doc-info">
            <span class="specialty">{{ doc.specialty }}</span>
            <h3>BS. {{ doc.fullName }}</h3>
            <p class="description">Chuyên gia chuyên khoa {{ doc.specialty }}. Tận tâm và giàu kinh nghiệm trong lĩnh vực y tế.</p>

            <div class="meta">
              <span><i class="fas fa-star" /> 4.9 (200+ đánh giá)</span>
              <span><i class="fas fa-money-bill-wave" /> {{ doc.consultationFee.toLocaleString('vi-VN') }}đ</span>
            </div>

            <button class="btn-cta btn-cta--block" @click="$router.push('/patient')">Xem lịch & Đặt hẹn</button>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
  import { computed, onMounted, ref } from 'vue'
  import Navbar from '@/components/Navbar.vue'
  import api from '@/services/api'

  const searchQuery = ref('')
  const activeCat = ref('Tất cả')
  const categories = ref(['Tất cả'])
  const doctors = ref<any[]>([])

  async function fetchDoctors () {
    try {
      const res = await api.get('/Doctors')
      doctors.value = res.data

      // Trích xuất danh sách chuyên khoa duy nhất
      const specs = [...new Set(res.data.map((d: any) => d.specialty))]
      categories.value = ['Tất cả', ...specs as string[]]
    } catch (error) {
      console.error('Lỗi khi tải danh sách bác sĩ:', error)
    }
  }

  onMounted(() => {
    fetchDoctors()
  })

  const filteredDoctors = computed(() => {
    return doctors.value.filter(doc => {
      const matchSearch = doc.fullName.toLowerCase().includes(searchQuery.value.toLowerCase())
        || doc.specialty.toLowerCase().includes(searchQuery.value.toLowerCase())
      const matchCat = activeCat.value === 'Tất cả' || doc.specialty === activeCat.value
      return matchSearch && matchCat
    })
  })
</script>

<style scoped>
@import '@/styles/public-pages.css';
</style>
