# 🚀 Hướng Dẫn Khởi Chạy Phân Hệ Đặt Lịch Hẹn (Appointment Service)

Tài liệu này hướng dẫn chi tiết các bước khởi chạy dịch vụ **Appointment Service** (nhóm của bạn) bao gồm: Cơ sở dữ liệu (PostgreSQL), API Gateway (Ocelot), ASP.NET Core Web API (Backend) và Vue 3 (Frontend) có tích hợp đồng bộ xác thực trung tâm.

---

## 📌 Cách 1: Khởi chạy nhanh bằng Docker Compose (Khuyên Dùng)

Đây là cách nhanh nhất và tự động cấu hình toàn bộ hạ tầng (PostgreSQL + Web API + API Gateway) chỉ bằng một câu lệnh:

1. **Khởi chạy hạ tầng:**
   Mở terminal tại thư mục `Appointment-Service` và chạy lệnh:
   ```bash
   docker-compose up -d --build
   ```
   *Lệnh này sẽ khởi chạy:*
   * Database PostgreSQL (`localhost:5432`)
   * Backend Web API (`localhost:5150`)
   * Ocelot API Gateway (`localhost:5000`)
   * Tự động tạo cơ sở dữ liệu `AppointmentDB` và nạp dữ liệu mẫu (Doctors, Slots, Services).

2. **Khởi chạy Frontend Đặt Lịch (Vite - Port 5173):**
   Mở một cửa sổ terminal mới tại `Appointment-Service/static` và chạy:
   ```bash
   npm install
   npm run dev
   ```
   *Truy cập cổng đặt lịch tại:* `http://localhost:5173`

---

## 📌 Cách 2: Khởi chạy Thủ công từng phần (Dành cho Lập trình / Debug)

Nếu bạn muốn chạy trực tiếp bằng IDE (Visual Studio, VS Code) để dễ dàng gỡ lỗi (Debug) mã nguồn:

### Bước 1: Khởi chạy cơ sở dữ liệu PostgreSQL
* Đảm bảo dịch vụ PostgreSQL đang chạy trên máy của bạn (`localhost:5432`).
* Tài khoản kết nối mặc định (được cấu hình trong `.env` và `appsettings.json`):
  * **User**: `postgres`
  * **Password**: `Medicare@2024`

*(Hoặc chạy nhanh container PostgreSQL độc lập qua Docker):*
```bash
docker run --name postgres-appointment -p 5432:5432 -e POSTGRES_PASSWORD=Medicare@2024 -e POSTGRES_DB=AppointmentDB -d postgres:15-alpine
```

### Bước 2: Chạy Backend ASP.NET Core API (Cổng 5150)
1. Mở terminal tại thư mục `Appointment-Service/application`.
2. Đảm bảo file `.env` tại thư mục gốc `Appointment-Service` có cấu hình:
   ```env
   CONNECTION_STRING=Host=localhost;Port=5432;Database=AppointmentDB;Username=postgres;Password=Medicare@2024
   ```
3. Chạy lệnh:
   ```bash
   dotnet restore
   dotnet run
   ```
   * API sẽ chạy tại `http://localhost:5150`.
   * Trang Swagger UI: `http://localhost:5150/swagger/index.html`.
   * **Cơ chế Seeding tự động**: Khi API chạy lần đầu, nó sẽ tự động tạo bảng, nạp 10 dịch vụ y tế mẫu, 2 tài khoản test và tự động phân lịch khám bác sĩ cho 7 ngày tới!

### Bước 3: Chạy API Gateway (Ocelot - Cổng 5000)
1. Mở terminal tại `Appointment-Service/gateway`.
2. Sửa tạm thời file `gateway/ocelot.json` dòng `Host: "appointment-service"` thành `Host: "localhost"` nếu bạn đang chạy local không dùng Docker network.
3. Chạy lệnh:
   ```bash
   dotnet restore
   dotnet run
   ```
   * Gateway sẽ chạy tại `http://localhost:5000`.

### Bước 4: Tạo tệp cấu hình Môi trường Frontend (.env)
Tạo file `.env` tại thư mục `Appointment-Service/static` với nội dung sau:
```env
VITE_API_URL=http://localhost:5000
VITE_API_TIMEOUT=10000
```

### Bước 5: Chạy Frontend Lịch hẹn (Cổng 5173)
1. Mở terminal tại `Appointment-Service/static`.
2. Chạy lệnh:
   ```bash
   npm install
   npm run dev
   ```
   * Giao diện đặt lịch chạy tại `http://localhost:5173`.

---

## 🔑 Tài Khoản Thử Nghiệm Hệ Thống (Seeded Accounts)

Sau khi cơ sở dữ liệu được khởi tạo thành công, hệ thống tự động nạp các tài khoản thử nghiệm sau:

| Vai Trò | Tên Đăng Nhập | Mật Khẩu | Mục Đích Thử Nghiệm |
| :--- | :--- | :--- | :--- |
| **Quản trị viên (Admin)** | `admin` | `123456` | Quản lý bác sĩ, dịch vụ, báo cáo tài chính |
| **Tiếp tân / Lễ tân** | `receptionist` | `123456` | Tiếp nhận bệnh nhân, duyệt hàng đợi số thứ tự |

---

## 🔗 Chạy Đồng Bộ Hóa Đăng Nhập Trung Tâm (SSO)

Để kiểm thử chức năng **Đồng bộ Đăng nhập / Đăng xuất** chéo nguồn (Cross-Host Auth Sync) mà bạn đã làm:

1. **Khởi chạy Master Portal (Cổng 3000):**
   ```bash
   cd medicare-portal
   npm install
   npm run dev
   ```
2. **Khởi chạy Cổng Đặt Lịch (Cổng 5173):**
   *(theo hướng dẫn phía trên)*

3. **Kiểm tra luồng hoạt động:**
   * Truy cập `http://localhost:5173` và click **Đặt lịch ngay**.
   * Hệ thống sẽ tự động chuyển hướng bạn sang cổng xác thực trung tâm tại `http://localhost:3000/login`.
   * Sử dụng chức năng **Đăng Nhập Nhanh** (click nút **👨‍⚕️ Bác sĩ** hoặc **👤 Bệnh nhân**) hoặc nhập tài khoản `admin`/`123456`.
   * Trình duyệt sẽ đồng bộ token và điều hướng mượt mà bạn quay lại trang `http://localhost:5173/auth-callback` rồi lưu trữ an toàn vào Pinia store local!
