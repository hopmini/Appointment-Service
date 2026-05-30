using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppointmentService.Data;
using AppointmentService.Models;
using AppointmentService.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace AppointmentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly AppointmentDbContext _context;
        private readonly AppointmentService.Services.IExternalSyncService _syncService;
        private readonly AppointmentService.Services.IEmailService _emailService;

        public AppointmentsController(
            AppointmentDbContext context, 
            AppointmentService.Services.IExternalSyncService syncService,
            AppointmentService.Services.IEmailService emailService)
        {
            _context = context;
            _syncService = syncService;
            _emailService = emailService;
        }

        // API 1: Tự động đẻ Slot (Ca sáng 8h-11h30, Ca chiều 13h-16h30)
        [HttpPost("generate-slots")]
        public async Task<IActionResult> GenerateSlots([FromBody] GenerateSlotDto request)
        {
            // 1. Kiểm tra bác sĩ có thật không
            var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == request.DoctorId);
            if (!doctorExists) return NotFound("Méo tìm thấy bác sĩ này m ơi!");

            // 2. Tránh đẻ trùng 2 lần trong 1 ngày
            var existingSlots = await _context.DoctorSlots
                .AnyAsync(s => s.DoctorId == request.DoctorId && s.Date.Date == request.Date.Date);
            if (existingSlots) return BadRequest("Ngày này đẻ slot rồi, định tạo đúp để clone bác sĩ à?");

            var slotsToCreate = new List<DoctorSlot>();

            // Khởi tạo ca SÁNG: 8:00 đến 11:30 (Mỗi ca 30p)
            var startTimeMorning = new TimeSpan(8, 0, 0);
            var endTimeMorning = new TimeSpan(11, 30, 0);
            while (startTimeMorning < endTimeMorning)
            {
                slotsToCreate.Add(new DoctorSlot
                {
                    Id = Guid.NewGuid(),
                    DoctorId = request.DoctorId,
                    Date = request.Date.Date,
                    StartTime = startTimeMorning,
                    EndTime = startTimeMorning.Add(TimeSpan.FromMinutes(30)),
                    IsBooked = false
                });
                startTimeMorning = startTimeMorning.Add(TimeSpan.FromMinutes(30));
            }

            // Khởi tạo ca CHIỀU: 13:00 đến 16:30 (Mỗi ca 30p)
            var startTimeAfternoon = new TimeSpan(13, 0, 0);
            var endTimeAfternoon = new TimeSpan(16, 30, 0);
            while (startTimeAfternoon < endTimeAfternoon)
            {
                slotsToCreate.Add(new DoctorSlot
                {
                    Id = Guid.NewGuid(),
                    DoctorId = request.DoctorId,
                    Date = request.Date.Date,
                    StartTime = startTimeAfternoon,
                    EndTime = startTimeAfternoon.Add(TimeSpan.FromMinutes(30)),
                    IsBooked = false
                });
                startTimeAfternoon = startTimeAfternoon.Add(TimeSpan.FromMinutes(30));
            }

            _context.DoctorSlots.AddRange(slotsToCreate);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Ảo thật đấy! Đã đẻ thành công {slotsToCreate.Count} slot cho ngày {request.Date:dd/MM/yyyy}!" });
        }

        // API 2: Bệnh nhân chốt đơn đặt lịch (Cho phép cả khách vãng lai)
        [HttpPost("book")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentDto request)
        {
            // 1. Kiểm tra bệnh nhân có tồn tại không (Đồng bộ động từ JWT Claims nếu chưa có)
            var patient = await _context.Users.FindAsync(request.PatientId);
            if (patient == null)
            {
                var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "patient";
                var fullName = User.FindFirst("FullName")?.Value ?? "Bệnh nhân";
                patient = new User
                {
                    Id = request.PatientId,
                    Username = userName,
                    PasswordHash = "",
                    FullName = fullName,
                    Email = userName + "@medicare.vn",
                    Role = "Patient"
                };
                _context.Users.Add(patient);
                await _context.SaveChangesAsync();
            }

            // 2. Tìm cái slot bệnh nhân đang nhắm tới
            var slot = await _context.DoctorSlots.FindAsync(request.SlotId);

            if (slot == null) return NotFound("Slot này bay màu rồi!");
            if (slot.IsBooked) return BadRequest("Thằng khác nhanh tay book mất rồi m ơi!");

            // 3. Chuyển trạng thái sang "Đã có chủ"
            slot.IsBooked = true;

            // 4. Viết đơn khám
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = request.PatientId,
                SlotId = request.SlotId,
                MedicalServiceId = request.MedicalServiceId,
                Status = 0, // Trạng thái 0: Chờ tiếp tân duyệt và phát số thứ tự
                Reason = request.Reason
            };

            _context.Appointments.Add(appointment);

            // 5. Lưu vào DB (VÀ BẮT LỖI NẾU CÓ 2 THẰNG BẤM CÙNG LÚC)
            try
            {
                await _context.SaveChangesAsync();

                // TẠO THÔNG BÁO CHO ADMIN/DASHBOARD
                var newNotif = new Notification
                {
                    Title = "Lịch hẹn mới",
                    Message = $"Bệnh nhân {patient.FullName} vừa đặt một lịch hẹn mới.",
                    Type = "info",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(newNotif);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Chốt đơn thành công! Đợi tiếp tân duyệt nhé m.", appointmentId = appointment.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                // MỘT TRONG NHỮNG CÁI ĐỈNH CỦA ĐỒ ÁN LÀ CHỖ NÀY
                // Nếu 2 người bấm vào 1 slot cùng mili-giây, thằng thứ 2 sẽ văng Exception này
                return StatusCode(409, new { message = "Nguy hiểm đó! Vừa có bệnh nhân khác giành mất slot này rồi. Vui lòng F5 chọn giờ khác!" });
            }
        }

        // ==========================================================
        // KHU VỰC CỦA TIẾP TÂN
        // ==========================================================

        // API 3: Tiếp tân lấy danh sách đơn đang chờ duyệt (Status = 0)
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingAppointments()
        {
            var pendingList = await _context.Appointments
                .Include(a => a.Slot) // Lấy thêm info từ bảng Slot
                .Where(a => a.Status == 0) // Chỉ lấy mấy đơn chưa duyệt
                .Select(a => new
                {
                    AppointmentId = a.Id,
                    PatientId = a.PatientId,
                    PatientName = _context.Users.Where(u => u.Id == a.PatientId).Select(u => u.FullName).FirstOrDefault(),
                    Date = a.Slot.Date,
                    Time = a.Slot.StartTime,
                    Reason = a.Reason
                })
                .ToListAsync();

            return Ok(pendingList);
        }

        // API 4: Tiếp tân duyệt đơn và tự động phát số thứ tự
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Receptionist,Admin")]
        public async Task<IActionResult> ApproveAppointment(Guid id)
        {
            // 1. Lôi cái đơn khám ra
            var appointment = await _context.Appointments
                .Include(a => a.Slot)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return NotFound("Méo thấy đơn khám này m ơi!");
            if (appointment.Status != 0) return BadRequest("Đơn này duyệt rồi hoặc bị hủy rồi, check lại đi!");

            // 2. Logic sinh số thứ tự (Đỉnh cao là ở đây)
            // Tìm xem trong ngày hôm đó, của ông bác sĩ đó, số thứ tự to nhất đang là bao nhiêu
            var maxQueueNum = await _context.Appointments
                .Include(a => a.Slot)
                .Where(a => a.Slot.DoctorId == appointment.Slot.DoctorId
                         && a.Slot.Date == appointment.Slot.Date
                         && a.QueueNumber.HasValue)
                .MaxAsync(a => (int?)a.QueueNumber) ?? 0;

            // Cấp số tiếp theo
            appointment.QueueNumber = maxQueueNum + 1;

            // Đổi trạng thái sang 1 (Đã duyệt / Chờ khám)
            appointment.Status = 1;

            await _context.SaveChangesAsync();

            // TẠO THÔNG BÁO CHO USER
            if (appointment.PatientId > 0)
            {
                var userNotif = new Notification
                {
                    UserId = appointment.PatientId,
                    Title = "Lịch hẹn đã được duyệt",
                    Message = $"Lịch hẹn ngày {appointment.Slot.Date:dd/MM/yyyy} của bạn đã được duyệt. STT: {appointment.QueueNumber}",
                    Type = "success",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(userNotif);
                await _context.SaveChangesAsync();
            }

            // GỬI EMAIL CHO USER
            var targetEmail = _context.Users.FirstOrDefault(u => u.Id == appointment.PatientId)?.Email;
            if (!string.IsNullOrEmpty(targetEmail))
            {
                string emailBody = $@"
                <div style='margin: 0; padding: 0; background-color: #f4f7f6; font-family: ""Segoe UI"", Helvetica, Arial, sans-serif;'>
                    <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                        <tr>
                            <td align='center' style='padding: 20px 10px;'>
                                <table border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 600px; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05);'>
                                    <!-- Header -->
                                    <tr>
                                        <td align='center' bgcolor='#0047AB' style='padding: 30px 20px;'>
                                            <h1 style='color: #ffffff; margin: 0; font-size: 22px; text-transform: uppercase; letter-spacing: 2px;'>Medicare Hospital</h1>
                                        </td>
                                    </tr>
                                    <!-- Content -->
                                    <tr>
                                        <td style='padding: 30px 20px;'>
                                            <h2 style='color: #1e293b; font-size: 20px; margin-top: 0;'>Xác nhận lịch khám</h2>
                                            <p style='color: #64748b; font-size: 15px; line-height: 1.6;'>Chào bạn,</p>
                                            <p style='color: #64748b; font-size: 15px; line-height: 1.6;'>Lịch hẹn của bạn tại Medicare đã được phê duyệt thành công. Chi tiết như sau:</p>
                                            
                                            <table border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #f8fafc; border-radius: 8px; margin: 20px 0;'>
                                                <tr>
                                                    <td style='padding: 20px;'>
                                                        <p style='margin: 5px 0; font-size: 14px; color: #64748b;'>Mã lịch hẹn: <b style='color: #0f172a;'>#{appointment.Id.ToString().Substring(0, 8).ToUpper()}</b></p>
                                                        <p style='margin: 5px 0; font-size: 12px; color: #94a3b8;'>Mã tra cứu (Full ID): {appointment.Id}</p>
                                                        <p style='margin: 5px 0; font-size: 14px; color: #64748b;'>Ngày khám: <b style='color: #0f172a;'>{appointment.Slot.Date:dd/MM/yyyy}</b></p>
                                                        <p style='margin: 5px 0; font-size: 14px; color: #64748b;'>Giờ khám: <b style='color: #0f172a;'>{appointment.Slot.StartTime:hh\:mm}</b></p>
                                                        <div style='margin-top: 15px; padding-top: 15px; border-top: 1px solid #e2e8f0;'>
                                                            <p style='margin: 0; font-size: 14px; color: #64748b;'>Số thứ tự của bạn:</p>
                                                            <p style='margin: 5px 0; font-size: 32px; font-weight: 900; color: #E53935;'>{appointment.QueueNumber}</p>
                                                        </div>
                                                    </td>
                                                </tr>
                                            </table>
                                            
                                             <p style='color: #64748b; font-size: 14px; line-height: 1.6;'>Vui lòng đến trước 15 phút để làm thủ tục. Mã tra cứu của bạn là: <b style='color: #0047AB;'>{appointment.Id.ToString().Substring(0, 8).ToUpper()}</b></p>
                                             
                                             <table border='0' cellpadding='0' cellspacing='0' width='100%' style='margin-top: 30px;'>
                                                 <tr>
                                                     <td align='center'>
                                                         <a href='http://localhost:5173/track?code={appointment.Id}' style='display: inline-block; background-color: #0047AB; color: #ffffff; padding: 14px 30px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 15px;'>Xem chi tiết & Quản lý lịch hẹn</a>
                                                        <div style='margin-top: 25px; text-align: center;'>
                                                            <p style='font-size: 12px; color: #64748b; margin-bottom: 10px;'>Hoặc quét mã QR để check-in tại quầy:</p>
                                                            <img src='https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={appointment.Id}' width='150' height='150' style='border: 8px solid #f8fafc; border-radius: 12px;' />
                                                        </div>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                    <!-- Footer -->
                                    <tr>
                                        <td bgcolor='#f8fafc' style='padding: 20px; text-align: center; border-top: 1px solid #e2e8f0;'>
                                            <p style='margin: 0; color: #64748b; font-size: 12px;'><b>Medicare Hospital</b></p>
                                            <p style='margin: 5px 0; color: #94a3b8; font-size: 11px;'>Địa chỉ: 78 Giải Phóng, Đống Đa, Hà Nội</p>
                                            <p style='margin: 0; color: #94a3b8; font-size: 11px;'>Hotline: 1900 6789</p>
                                        </td>
                                    </tr>
                                </table>
                                <p style='margin-top: 20px; color: #94a3b8; font-size: 10px; text-align: center;'>Đây là thư tự động, vui lòng không trả lời.</p>
                            </td>
                        </tr>
                    </table>
                </div>";
                
                await _emailService.SendEmailAsync(targetEmail, "Xác nhận lịch hẹn thành công - Medicare Hospital", emailBody);
            }

            return Ok(new
            {
                message = "Duyệt thành công, đã tạo thông báo và gửi Email xác nhận!",
                queueNumber = appointment.QueueNumber
            });
        }
        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Receptionist,Patient,Admin")]
        public async Task<IActionResult> CancelAppointment(Guid id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Slot)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return NotFound("Méo thấy đơn khám này m ơi!");
            
            appointment.Status = 2; // Cancelled
            if (appointment.Slot != null)
            {
                appointment.Slot.IsBooked = false; // Release slot
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã hủy lịch hẹn thành công." });
        }

        [HttpGet("all")]
        [Authorize(Roles = "Receptionist,Admin")]
        public async Task<IActionResult> GetAllAppointments()
        {
            var list = await _context.Appointments
                .Include(a => a.Slot)
                .ThenInclude(s => s.Doctor)
                .Include(a => a.MedicalService)
                .Select(a => new
                {
                    a.Id,
                    PatientName = _context.Users.Where(u => u.Id == a.PatientId).Select(u => u.FullName).FirstOrDefault() ?? "Ẩn danh",
                    DoctorName = a.Slot.Doctor.FullName,
                    ServiceName = a.MedicalService != null ? a.MedicalService.Name : "Khám chung",
                    Date = a.Slot.Date,
                    Time = a.Slot.StartTime,
                    Status = a.Status,
                    QueueNumber = a.QueueNumber
                })
                .ToListAsync();

            return Ok(list);
        }

        // API Tra cứu lịch hẹn dành cho khách vãng lai
        [HttpGet("track/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> TrackAppointment(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Mã tra cứu không hợp lệ.");

            Appointment? a = null;

            if (Guid.TryParse(id, out Guid guidId))
            {
                a = await _context.Appointments
                    .Include(x => x.Slot)
                    .ThenInclude(s => s.Doctor)
                    .Include(x => x.MedicalService)
                    .FirstOrDefaultAsync(x => x.Id == guidId);
            }
            else if (id.Length >= 6)
            {
                var sid = id.ToLower();
                a = await _context.Appointments
                    .Include(x => x.Slot)
                    .ThenInclude(s => s.Doctor)
                    .Include(x => x.MedicalService)
                    .FirstOrDefaultAsync(x => x.Id.ToString().ToLower().StartsWith(sid));
            }

            if (a == null) return NotFound("Không tìm thấy lịch hẹn. Vui lòng kiểm tra lại mã.");

            return Ok(new
            {
                a.Id,
                PatientName = _context.Users.Where(u => u.Id == a.PatientId).Select(u => u.FullName).FirstOrDefault() ?? "Khách",
                DoctorName = a.Slot.Doctor.FullName,
                ServiceName = a.MedicalService?.Name ?? "Khám chung",
                Date = a.Slot.Date,
                Time = a.Slot.StartTime,
                Status = a.Status,
                QueueNumber = a.QueueNumber
            });
        }

        // API Lấy trạng thái hàng đợi thực tế
        [HttpGet("queue-status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetQueueStatus()
        {
            var today = DateTime.UtcNow.Date;
            var doctors = await _context.Doctors.ToListAsync();
            var result = new List<object>();

            foreach (var doc in doctors)
            {
                var currentNum = await _context.Appointments
                    .Where(a => a.Slot.DoctorId == doc.Id && a.Slot.Date == today && a.Status == 2)
                    .OrderByDescending(a => a.QueueNumber)
                    .Select(a => (int?)a.QueueNumber)
                    .FirstOrDefaultAsync();

                var anyToday = await _context.Appointments
                    .AnyAsync(a => a.Slot.DoctorId == doc.Id && a.Slot.Date == today);

                result.Add(new
                {
                    Doctor = "BS. " + doc.FullName,
                    Status = anyToday ? "Đang khám" : "Nghỉ ca",
                    CurrentNum = currentNum.HasValue ? currentNum.Value.ToString() : (anyToday ? "0" : "--")
                });
            }

            return Ok(result);
        }

        // API Lấy lịch hẹn của bệnh nhân đang đăng nhập
        [HttpGet("my")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> GetMyAppointments()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var list = await _context.Appointments
                .Include(a => a.Slot)
                    .ThenInclude(s => s.Doctor)
                .Include(a => a.MedicalService)
                .Where(a => a.PatientId == userId)
                .OrderByDescending(a => a.Slot.Date)
                .Select(a => new
                {
                    a.Id,
                    DoctorName = a.Slot.Doctor.FullName,
                    ServiceName = a.MedicalService != null ? a.MedicalService.Name : "Khám chung",
                    Date = a.Slot.Date,
                    Time = a.Slot.StartTime,
                    a.Status,
                    a.QueueNumber
                })
                .ToListAsync();

            return Ok(list);
        }

        // API Lấy lịch hẹn hôm nay của bác sĩ (cho Doctor Dashboard)
        [HttpGet("doctor-today")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetTodayAppointmentsForDoctor()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null)
            {
                var fullName = User.FindFirst("FullName")?.Value;
                if (!string.IsNullOrEmpty(fullName))
                {
                    var cleanName = fullName.Replace("BS. ", "").Replace("Bác sĩ ", "").Trim();
                    doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == null && (d.FullName == cleanName || d.FullName == fullName));
                    if (doctor != null)
                    {
                        doctor.UserId = userId;
                        await _context.SaveChangesAsync();
                    }
                }
            }

            if (doctor == null)
                return BadRequest("Bạn chưa được phân công là bác sĩ trong hệ thống.");

            var today = DateTime.UtcNow.Date;
            var list = await _context.Appointments
                .Include(a => a.Slot)
                .Include(a => a.MedicalService)
                .Where(a => a.Slot.DoctorId == doctor.Id && a.Slot.Date == today)
                .Select(a => new
                {
                    a.Id,
                    PatientName = _context.Users.Where(u => u.Id == a.PatientId).Select(u => u.FullName).FirstOrDefault() ?? "Khách",
                    a.Slot.StartTime,
                    a.Slot.EndTime,
                    a.Status,
                    a.QueueNumber,
                    ServiceName = a.MedicalService != null ? a.MedicalService.Name : "Khám chung",
                    a.Reason
                })
                .OrderBy(a => a.QueueNumber)
                .ToListAsync();

            return Ok(new
            {
                DoctorName = doctor.FullName,
                Date = today,
                Appointments = list
            });
        }

        // API Tự động phân lịch cho bác sĩ
        [HttpPost("sync-slots")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> SyncSlots([FromServices] AppointmentService.Services.IScheduleService scheduler)
        {
            int created = await scheduler.AutoGenerateSlotsAsync(7);
            return Ok(new { message = $"Đã tạo thêm {created} khung giờ mới cho 7 ngày tới.", count = created });
        }
    }
}