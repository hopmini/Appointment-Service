using Microsoft.EntityFrameworkCore;
using AppointmentService.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AppointmentService.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;


AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Nối cáp Database nè m
var connectionString = builder.Configuration["CONNECTION_STRING"] 
    ?? builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Port=5432;Database=AppointmentDB;Username=postgres;Password=Medicare@2024";

builder.Services.AddDbContext<AppointmentDbContext>(options =>
    options.UseNpgsql(connectionString));

// Đăng ký HttpClient để gọi API sang các service khác
builder.Services.AddHttpClient();
// Đăng ký Service giao tiếp liên dịch vụ
builder.Services.AddScoped<AppointmentService.Services.IExternalSyncService, AppointmentService.Services.ExternalSyncService>();
builder.Services.AddScoped<AppointmentService.Services.IEmailService, AppointmentService.Services.EmailService>();
builder.Services.AddScoped<AppointmentService.Services.IScheduleService, AppointmentService.Services.ScheduleService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
// --- ĐOẠN CẤU HÌNH JWT BẮT ĐẦU ---
var jwtSecret = builder.Configuration["JWT_SECRET"] ?? builder.Configuration["Jwt:Key"] ?? "CaiNayLaBiMatQuocGiaSieuCapVipPro123!";
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? builder.Configuration["Jwt:Issuer"] ?? "ClinicAuthService";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? builder.Configuration["Jwt:Audience"] ?? "ClinicUsers";

var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- TỰ ĐỘNG TẠO TÀI KHOẢN ADMIN/STAFF NẾU CHƯA CÓ (VỚI VÒNG LẶP RETRY CHO DOCKER) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppointmentDbContext>();
    int maxRetries = 15;
    int delaySeconds = 3;
    bool connected = false;

    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            // Đảm bảo Database và các bảng được tạo mới chuẩn chỉnh kể cả khi DB đã được PostgreSQL tự tạo trước
            var databaseCreator = db.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
            if (databaseCreator != null)
            {
                if (!databaseCreator.Exists()) databaseCreator.Create();
                if (!databaseCreator.HasTables()) databaseCreator.CreateTables();
            }

            // LỆNH CƯỠNG ÉP TẠO BẢNG NOTIFICATIONS NẾU CHƯA CÓ (Cú pháp PostgreSQL)
            string createTableSql = @"
                CREATE TABLE IF NOT EXISTS ""Notifications"" (
                    ""Id"" uuid NOT NULL PRIMARY KEY,
                    ""UserId"" int NULL,
                    ""Title"" text NOT NULL,
                    ""Message"" text NOT NULL,
                    ""Type"" text NOT NULL,
                    ""IsRead"" boolean NOT NULL,
                    ""CreatedAt"" timestamp NOT NULL
                );";
            db.Database.ExecuteSqlRaw(createTableSql);

            // Tự động sửa lỗi không khớp DB: xóa cột RowVersion cũ nếu tồn tại trong bảng DoctorSlots để khớp 100% với C# Model
            try 
            {
                db.Database.ExecuteSqlRaw(@"ALTER TABLE ""DoctorSlots"" DROP COLUMN IF EXISTS ""RowVersion"";");
                Console.WriteLine("🛡️ [Database Self-Heal] Đã kiểm tra và loại bỏ cột RowVersion không khớp trong bảng DoctorSlots.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Database Self-Heal Warning] {ex.Message}");
            }

            // Tự động thêm cột ExaminationDuration nếu chưa có
            try 
            {
                db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""ExaminationDuration"" integer NOT NULL DEFAULT 30;");
                Console.WriteLine("🛡️ [Database Self-Heal] Đã kiểm tra và thêm cột ExaminationDuration vào bảng Appointments.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Database Self-Heal Warning for ExaminationDuration] {ex.Message}");
            }

            connected = true;
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Appointment-Service Retry {i}/{maxRetries}] Failed to connect to DB: {ex.Message}. Retrying in {delaySeconds}s...");
            System.Threading.Thread.Sleep(delaySeconds * 1000);
        }
    }

    if (connected)
    {
        try
        {
            if (!db.MedicalServices.Any())
            {
                db.MedicalServices.AddRange(new List<MedicalService>
                {
                    new MedicalService { Id = Guid.NewGuid(), Name = "Khám Nội Tổng Quát", Description = "Kiểm tra sức khỏe định kỳ, tư vấn bệnh lý nội khoa.", Price = 200000, Category = "Khám bệnh", IsActive = true },
                    new MedicalService { Id = Guid.NewGuid(), Name = "Khám Nhi Khoa", Description = "Chăm sóc sức khỏe toàn diện cho trẻ em.", Price = 150000, Category = "Khám bệnh", IsActive = true },
                    new MedicalService { Id = Guid.NewGuid(), Name = "Khám Mắt Chuyên Sâu", Description = "Đo thị lực, kiểm tra các bệnh lý về mắt.", Price = 300000, Category = "Chuyên khoa", IsActive = true },
                    new MedicalService { Id = Guid.NewGuid(), Name = "Nội Soi Dạ Dày", Description = "Tầm soát và chẩn đoán bệnh lý dạ dày.", Price = 800000, Category = "Cận lâm sàng", IsActive = true },
                    new MedicalService { Id = Guid.NewGuid(), Name = "Siêu Âm Tim Doppler", Description = "Kiểm tra chức năng tim mạch.", Price = 500000, Category = "Cận lâm sàng", IsActive = true },
                    new MedicalService { Id = Guid.NewGuid(), Name = "Xét Nghiệm Máu Tổng Quát", Description = "Kiểm tra các chỉ số sinh hóa máu.", Price = 450000, Category = "Xét nghiệm", IsActive = true },
                    new MedicalService { Id = Guid.NewGuid(), Name = "Khám Tai Mũi Họng", Description = "Nội soi và điều trị các bệnh Tai Mũi Họng.", Price = 250000, Category = "Chuyên khoa", IsActive = true },
                    new MedicalService { Id = Guid.NewGuid(), Name = "Khám Phụ Khoa", Description = "Chăm sóc sức khỏe phụ nữ và tầm soát ung thư.", Price = 350000, Category = "Chuyên khoa", IsActive = true },
                    new MedicalService { Id = Guid.NewGuid(), Name = "Chụp X-Quang Phổi", Description = "Kiểm tra hình ảnh lồng ngực.", Price = 120000, Category = "Cận lâm sàng", IsActive = true },
                    new MedicalService { Id = Guid.NewGuid(), Name = "Tiêm Chủng Vaccine", Description = "Tư vấn và tiêm chủng các loại vaccine.", Price = 100000, Category = "Phòng bệnh", IsActive = true }
                });
                db.SaveChanges();
            }

            // Upsert users: tạo mới hoặc cập nhật nếu đã tồn tại (dùng username để check)
            var defaultUsers = new List<User>
            {
                new User {
                    Username = "admin",
                    PasswordHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92",
                    FullName = "Admin Medicare",
                    Role = "Admin",
                    Email = "admin@medicare.vn"
                },
                new User {
                    Username = "receptionist",
                    PasswordHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92",
                    FullName = "Lễ tân Medicare",
                    Role = "Receptionist",
                    Email = "receptionist@medicare.vn"
                }
            };
            foreach (var u in defaultUsers)
            {
                var existing = db.Users.FirstOrDefault(x => x.Username == u.Username);
                if (existing == null)
                {
                    db.Users.Add(u);
                    Console.WriteLine($"✅ Đã tạo user: {u.Username}");
                }
                else
                {
                    existing.PasswordHash = u.PasswordHash;
                    existing.FullName = u.FullName;
                    existing.Role = u.Role;
                    existing.Email = u.Email;
                    Console.WriteLine($"🔄 Đã cập nhật user: {u.Username}");
                }
            }
            db.SaveChanges();
            Console.WriteLine("✅ Users seeded successfully.");

            // Tự động phân lịch cho bác sĩ (7 ngày tới)
            var scheduler = scope.ServiceProvider.GetRequiredService<AppointmentService.Services.IScheduleService>();
            var slotsCreatedTask = scheduler.AutoGenerateSlotsAsync(7);
            slotsCreatedTask.Wait();
            int slotsCreated = slotsCreatedTask.Result;
            if (slotsCreated > 0)
            {
                Console.WriteLine($"📅 Tự động tạo {slotsCreated} khung giờ khám cho 7 ngày tới.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ LỖI SEEDING HOẶC PHÂN LỊCH: " + ex.Message);
        }
    }
    else
    {
        Console.WriteLine("❌ LỖI KẾT NỐI DATABASE: Không thể kết nối tới Postgres database sau 15 lần thử.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll"); // Nằm trên Authorization nhé
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();