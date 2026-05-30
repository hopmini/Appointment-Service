import { createPinia } from 'pinia'
import { createApp } from 'vue'
import App from './App.vue'
import { registerPlugins } from './plugins'
import '@/styles/base.css'

const app = createApp(App)
app.use(createPinia())
registerPlugins(app)
app.mount('#app')
