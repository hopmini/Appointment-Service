# Appointment Management System

Hệ thống quản lý lịch hẹn toàn bộ stack (Backend .NET + Frontend Vue.js)

## 📁 Cấu trúc dự án

```
FullStack/
├── application/          # Backend (C# .NET 8)
│   ├── Controllers/
│   ├── Models/
│   ├── DTOs/
│   ├── Data/
│   ├── Migrations/
│   ├── Program.cs
│   └── appsettings.json
│
├── static/              # Frontend (Vue.js + Vuetify)
│   ├── src/
│   │   ├── components/
│   │   ├── stores/
│   │   ├── plugins/
│   │   └── App.vue
│   ├── package.json
│   └── vite.config.mts
│
├── database/            # Database migrations & schemas
│   └── migrations/
│
├── services/            # Shared services (tùy chọn)
│
├── .gitignore
├── README.md
└── manage.py            # Script quản lý dự án
```

## 🚀 Hướng dẫn cài đặt

### Yêu cầu
- **.NET 8** (Backend)
- **Node.js 20+** (Frontend)
- **npm** hoặc **pnpm** (Frontend package manager)
- **SQL Server** hoặc **SQLite** (Database)

### Thiết lập nhanh (1 lệnh)

```bash
# Cài đặt tất cả dependencies
python manage.py setup

# Chạy toàn bộ dự án (Backend + Frontend)
python manage.py run
```

Vậy là xong! Backend sẽ chạy tại `https://localhost:7001` và Frontend tại `http://localhost:5173`

### Thiết lập Backend

```bash
# Di chuyển vào folder application
cd application

# Khôi phục packages
dotnet restore

# Tạo database
dotnet ef database update

# Chạy backend (mặc định: https://localhost:7000)
dotnet run
```

## ▶️ Chạy dự án

### Cách nhanh nhất - 1 lệnh Python
```bash
python manage.py run
```
Điều này sẽ chạy cả Backend + Frontend cùng lúc trong background

### Hoặc chạy riêng lẻ

**Backend:**
```bash
cd application
dotnet run
```

**Frontend (trong terminal khác):**
```bash
cd static
npm run dev
```

## 🔧 Cấu hình

### Backend (appsettings.json)
- Thay đổi connection string database
- Cấu hình CORS nếu cần
- Thay đổi port mặc định

### Frontend (.env.local)
```
VITE_API_URL=https://localhost:7000/api
```

## 📦 API Endpoints

### Doctors
- `GET /api/doctors` - Lấy danh sách bác sĩ
- `GET /api/doctors/{id}` - Lấy chi tiết bác sĩ
- `POST /api/doctors` - Tạo bác sĩ mới
- `PUT /api/doctors/{id}` - Cập nhật bác sĩ
- `DELETE /api/doctors/{id}` - Xóa bác sĩ

### Appointments
- `GET /api/appointments` - Lấy danh sách lịch hẹn
- `GET /api/appointments/{id}` - Lấy chi tiết lịch hẹn
- `POST /api/appointments` - Tạo lịch hẹn mới
- `PUT /api/appointments/{id}` - Cập nhật lịch hẹn
- `DELETE /api/appointments/{id}` - Xóa lịch hẹn

## 🛠️ Công cụ phát triển

### Backend
- Framework: **ASP.NET Core 8**
- ORM: **Entity Framework Core**
- Database: **SQL Server / SQLite**

### Frontend
- Framework: **Vue 3**
- UI Library: **Vuetify 3**
- Build Tool: **Vite**
- State Management: **Pinia**
- HTTP Client: **Axios**

## 📝 Ghi chú

- Backend chạy trên port `7000` (HTTP) / `7001` (HTTPS)
- Frontend chạy trên port `5173` (development)
- Đảm bảo CORS được kích hoạt trên backend
- Xác thực được thiết lập nếu cần

## 🔐 Bảo mật

- Không commit `.env` files
- Sử dụng secrets để lưu trữ sensitive data
- Bật CORS chỉ cho domains được phép

## 📞 Hỗ trợ

Nếu gặp vấn đề, kiểm tra:
1. Các port không bị chiếm dụng
2. Database connection string
3. CORS settings
4. Node.js và .NET versions
