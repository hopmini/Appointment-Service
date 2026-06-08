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
            var userEmail = !string.IsNullOrEmpty(request.PatientEmail) && request.PatientEmail.Contains("@")
                            ? request.PatientEmail
                            : (User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                               ?? User.FindFirst("email")?.Value 
                               ?? User.FindFirst("Email")?.Value);

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
                    Email = !string.IsNullOrEmpty(userEmail) && userEmail.Contains("@") ? userEmail : userName + "@medicare.vn",
                    Role = "Patient"
                };
                _context.Users.Add(patient);
                await _context.SaveChangesAsync();
            }
            else if (!string.IsNullOrEmpty(userEmail) && userEmail.Contains("@"))
            {
                // Cập nhật và chữa lành email mới nếu người dùng thay đổi email liên hệ mới hoặc sửa email ảo!
                if (patient.Email != userEmail)
                {
                    patient.Email = userEmail;
                    await _context.SaveChangesAsync();
                }
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
                Reason = request.Reason,
                ExaminationDuration = request.ExaminationDuration > 0 ? request.ExaminationDuration : 30
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
                    Reason = a.Reason,
                    ExaminationDuration = a.ExaminationDuration
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
                <div style='margin: 0; padding: 0; background-color: #f8fafc; font-family: ""Inter"", ""Segoe UI"", Helvetica, Arial, sans-serif;'>
                    <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                        <tr>
                            <td align='center' style='padding: 40px 10px;'>
                                <table border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 600px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 30px rgba(15,23,42,0.08); border: 1px solid #e2e8f0;'>
                                    <!-- Header -->
                                    <tr>
                                        <td align='center' bgcolor='#2563eb' style='padding: 40px 20px; background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);'>
                                            <div style='display: inline-block; background-color: rgba(255,255,255,0.15); padding: 12px; border-radius: 12px; margin-bottom: 12px;'>
                                                <svg fill='none' viewBox='0 0 32 32' xmlns='http://www.w3.org/2000/svg' style='height: 32px; width: 32px; display: block;'>
                                                    <rect fill='#ffffff' height='32' rx='8' width='32' />
                                                    <path d='M16 6v20M6 16h20' stroke='#2563eb' stroke-linecap='round' stroke-width='4' />
                                                </svg>
                                            </div>
                                            <h1 style='color: #ffffff; margin: 0; font-size: 24px; font-weight: 800; letter-spacing: -0.5px;'>MEDICARE HOSPITAL</h1>
                                            <p style='color: rgba(255,255,255,0.85); margin: 6px 0 0 0; font-size: 14px; font-weight: 500;'>Cổng Chăm Sóc Sức Khỏe Toàn Diện</p>
                                        </td>
                                    </tr>
                                    <!-- Content -->
                                    <tr>
                                        <td style='padding: 40px 30px;'>
                                            <h2 style='color: #0f172a; font-size: 22px; font-weight: 800; margin-top: 0; margin-bottom: 8px; letter-spacing: -0.5px;'>Lịch Hẹn Khám Được Phê Duyệt</h2>
                                            <p style='color: #475569; font-size: 15px; line-height: 1.6; margin: 0 0 24px 0; font-weight: 500;'>Chào bạn, lịch đặt khám trực tuyến của bạn đã được tiếp nhận, phê duyệt và cấp số thứ tự thành công. Vui lòng kiểm tra chi tiết thông tin bên dưới:</p>
                                            
                                            <!-- Detail Box -->
                                            <table border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #f8fafc; border-radius: 12px; margin-bottom: 30px; border: 1px solid #f1f5f9;'>
                                                <tr>
                                                    <td style='padding: 24px;'>
                                                        <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                                            <tr>
                                                                <td style='padding-bottom: 12px; border-bottom: 1px dashed #e2e8f0; font-size: 14px;'>
                                                                    <span style='color: #64748b; font-weight: 600;'>Mã số lịch hẹn:</span>
                                                                    <span style='float: right; color: #2563eb; font-weight: 800;'>#{appointment.Id.ToString().Substring(0, 8).ToUpper()}</span>
                                                                </td>
                                                            </tr>
                                                            <tr>
                                                                <td style='padding: 12px 0; border-bottom: 1px dashed #e2e8f0; font-size: 14px;'>
                                                                    <span style='color: #64748b; font-weight: 600;'>Ngày khám lâm sàng:</span>
                                                                    <span style='float: right; color: #0f172a; font-weight: 750;'>{appointment.Slot.Date:dd/MM/yyyy}</span>
                                                                </td>
                                                            </tr>
                                                            <tr>
                                                                <td style='padding: 12px 0; border-bottom: 1px dashed #e2e8f0; font-size: 14px;'>
                                                                    <span style='color: #64748b; font-weight: 600;'>Giờ hẹn chi tiết:</span>
                                                                    <span style='float: right; color: #0f172a; font-weight: 750;'>{appointment.Slot.StartTime:hh\:mm}</span>
                                                                </td>
                                                            </tr>
                                                            <tr>
                                                                <td style='padding-top: 16px;'>
                                                                    <span style='color: #64748b; font-size: 14px; font-weight: 600; display: block; margin-bottom: 6px;'>SỐ THỨ TỰ KHÁM CỦA BẠN:</span>
                                                                    <div style='background-color: #f0fdf4; border: 1.5px solid #10b981; border-radius: 8px; padding: 12px; text-align: center; margin-top: 6px;'>
                                                                        <span style='font-size: 38px; font-weight: 900; color: #10b981; line-height: 1;'>{appointment.QueueNumber}</span>
                                                                        <p style='margin: 4px 0 0 0; font-size: 11px; color: #047857; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px;'>Vui lòng đến trước 15 phút so với giờ hẹn</p>
                                                                    </div>
                                                                </td>
                                                            </tr>
                                                        </table>
                                                    </td>
                                                </tr>
                                            </table>
                                            
                                             <!-- Action Button & QR -->
                                             <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                                 <tr>
                                                     <td align='center'>
                                                         <a href='http://103.72.99.53:3000/track?code={appointment.Id}' style='display: inline-block; background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%); color: #ffffff; padding: 16px 36px; text-decoration: none; border-radius: 8px; font-weight: 800; font-size: 15px; box-shadow: 0 4px 12px rgba(37,99,235,0.2); letter-spacing: -0.2px;'>Tra Cứu & Quản Lý Lịch Hẹn</a>
                                                        
                                                        <div style='margin-top: 32px; border-top: 1px solid #e2e8f0; padding-top: 28px; text-align: center;'>
                                                            <p style='font-size: 13px; font-weight: 700; color: #475569; margin: 0 0 12px 0;'>Mã QR Check-in Nhanh Tại Quầy:</p>
                                                            <img src='https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={appointment.Id}' width='140' height='140' style='border: 6px solid #f8fafc; border-radius: 12px; display: block; margin: 0 auto;' />
                                                            <p style='font-size: 11px; color: #94a3b8; margin: 8px 0 0 0; font-weight: 500;'>Quét mã này tại Kiosk Lễ tân để check-in tức thời</p>
                                                        </div>
                                                     </td>
                                                 </tr>
                                             </table>
                                        </td>
                                    </tr>
                                    <!-- Footer -->
                                    <tr>
                                        <td bgcolor='#f8fafc' style='padding: 30px; text-align: center; border-top: 1px solid #e2e8f0;'>
                                            <p style='margin: 0; color: #0f172a; font-size: 14px; font-weight: 800;'>Bệnh viện Quốc tế Medicare</p>
                                            <p style='margin: 6px 0; color: #64748b; font-size: 12px; font-weight: 500;'>Địa chỉ: 78 Giải Phóng, Đống Đa, Hà Nội</p>
                                            <p style='margin: 0; color: #64748b; font-size: 12px; font-weight: 500;'>Hotline Hỗ Trợ: 1900 6789</p>
                                        </td>
                                    </tr>
                                </table>
                                <p style='margin-top: 24px; color: #94a3b8; font-size: 11px; text-align: center; font-weight: 500;'>Đây là thư thông báo tự động từ hệ thống Medicare Hospital. Vui lòng không phản hồi thư này.</p>
                            </td>
                        </tr>
                    </table>
                </div>";
                await _emailService.SendEmailAsync(targetEmail, "Xác nhận lịch khám thành công - Medicare Hospital", emailBody);
            }

            return Ok(new
            {
                message = "Duyệt thành công, đã tạo thông báo và gửi Email xác nhận!",
                queueNumber = appointment.QueueNumber
            });
        }
        [HttpPut("{id}/cancel")]
        [AllowAnonymous]
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

            // TẠO THÔNG BÁO CHO USER KHI HỦY
            if (appointment.PatientId > 0)
            {
                var cancelNotif = new Notification
                {
                    UserId = appointment.PatientId,
                    Title = "Lịch hẹn đã bị hủy",
                    Message = $"Lịch hẹn ngày {appointment.Slot.Date:dd/MM/yyyy} lúc {appointment.Slot.StartTime:hh\\:mm} của bạn đã bị hủy.",
                    Type = "error",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(cancelNotif);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã hủy lịch hẹn thành công." });
        }

        // API Tra cứu cập nhật lý do khám
        [HttpPut("{id}/reason")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateReason(Guid id, [FromBody] UpdateReasonDto request)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound("Méo thấy đơn khám này!");
            if (appointment.Status != 0 && appointment.Status != 1) return BadRequest("Lịch khám đã hoàn thành hoặc đã hủy, không thể thay đổi thông tin.");
            
            appointment.Reason = request.Reason;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã cập nhật lý do khám thành công." });
        }

        // API Đổi lịch hẹn
        [HttpPut("{id}/reschedule")]
        [AllowAnonymous]
        public async Task<IActionResult> RescheduleAppointment(Guid id, [FromBody] RescheduleDto request)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Slot)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return NotFound("Méo thấy đơn khám này!");
            if (appointment.Status != 0 && appointment.Status != 1) return BadRequest("Lịch khám đã được xử lý, không thể đổi lịch.");

            var newSlot = await _context.DoctorSlots.FindAsync(request.NewSlotId);
            if (newSlot == null) return NotFound("Khung giờ khám mới không tồn tại.");
            if (newSlot.IsBooked) return BadRequest("Khung giờ khám này vừa mới có người khác chọn rồi.");

            // Release old slot
            if (appointment.Slot != null)
            {
                appointment.Slot.IsBooked = false;
            }

            // Book new slot
            newSlot.IsBooked = true;
            appointment.SlotId = request.NewSlotId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đổi lịch khám thành công." });
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
                    PatientId = a.PatientId,
                    PatientName = _context.Users.Where(u => u.Id == a.PatientId).Select(u => u.FullName).FirstOrDefault() ?? "Ẩn danh",
                    DoctorName = a.Slot.Doctor.FullName,
                    ServiceName = a.MedicalService != null ? a.MedicalService.Name : "Khám chung",
                    Date = a.Slot.Date,
                    Time = a.Slot.StartTime,
                    Status = a.Status,
                    QueueNumber = a.QueueNumber,
                    ExaminationDuration = a.ExaminationDuration
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
                DoctorId = a.Slot.DoctorId,
                ServiceName = a.MedicalService?.Name ?? "Khám chung",
                Date = a.Slot.Date,
                Time = a.Slot.StartTime,
                Status = a.Status,
                QueueNumber = a.QueueNumber,
                ExaminationDuration = a.ExaminationDuration
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
                    a.QueueNumber,
                    ExaminationDuration = a.ExaminationDuration
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
            var fullName = User.FindFirst("FullName")?.Value;

            if (doctor == null && !string.IsNullOrEmpty(fullName))
            {
                var cleanName = fullName.Replace("BS. ", "").Replace("Bác sĩ ", "").Trim();
                // 1. Tìm bác sĩ khớp tên mà chưa được gán UserId
                doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == null && (d.FullName == cleanName || d.FullName == fullName));

                // 2. Nếu không có bác sĩ chưa gán, tìm bất kỳ bác sĩ nào khớp tên (cưỡng ép gán lại)
                if (doctor == null)
                {
                    doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.FullName == cleanName || d.FullName == fullName);
                }

                if (doctor != null)
                {
                    doctor.UserId = userId;
                    await _context.SaveChangesAsync();
                }
            }

            // 3. Nếu vẫn không thấy bác sĩ trong DB, tự động tạo hồ sơ Bác sĩ (Self-Healing) để tránh lỗi giao diện!
            if (doctor == null)
            {
                var cleanName = !string.IsNullOrEmpty(fullName) ? fullName.Replace("BS. ", "").Replace("Bác sĩ ", "").Trim() : "Nguyễn Văn A";
                doctor = new Doctor
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    FullName = cleanName,
                    Specialty = "Nội tổng quát",
                    Degree = "Bác sĩ",
                    ConsultationFee = 200000
                };
                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();
                Console.WriteLine($"🛡️ [Self-Healing] Đã tự động tạo hồ sơ Bác sĩ cho {fullName} (UserId: {userId})");
            }

            var localTodayLimit = DateTime.Today.AddDays(-1);
            var utcTodayLimit = DateTime.UtcNow.Date.AddDays(-1);

            var list = await _context.Appointments
                .Include(a => a.Slot)
                .Include(a => a.MedicalService)
                .Where(a => a.Slot.DoctorId == doctor.Id && (a.Slot.Date >= localTodayLimit || a.Slot.Date >= utcTodayLimit))
                .Select(a => new
                {
                    a.Id,
                    PatientId = a.PatientId,
                    PatientName = _context.Users.Where(u => u.Id == a.PatientId).Select(u => u.FullName).FirstOrDefault() ?? "Khách",
                    Date = a.Slot.Date,
                    a.Slot.StartTime,
                    a.Slot.EndTime,
                    a.Status,
                    a.QueueNumber,
                    ServiceName = a.MedicalService != null ? a.MedicalService.Name : "Khám chung",
                    a.Reason,
                    ExaminationDuration = a.ExaminationDuration
                })
                .OrderBy(a => a.Date)
                .ThenBy(a => a.StartTime)
                .ToListAsync();

            return Ok(new
            {
                DoctorName = doctor.FullName,
                Date = DateTime.Today,
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