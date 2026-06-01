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
    public class DoctorsController : ControllerBase
    {
        private readonly AppointmentDbContext _context;

        // Cắm dây DbContext vào để chọc xuống Database
        public DoctorsController(AppointmentDbContext context)
        {
            _context = context;
        }

        // API 1: Lấy danh sách bác sĩ (Frontend gọi cái này)
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<DoctorResponseDto>>> GetDoctors()
        {
            var doctors = await _context.Doctors
                .Select(d => new DoctorResponseDto
                {
                    Id = d.Id,
                    FullName = d.FullName,
                    Specialty = d.Specialty,
                    ConsultationFee = d.ConsultationFee
                })
                .ToListAsync();

            return Ok(doctors);
        }

        // API 2: Thêm 1 ông bác sĩ mới
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> CreateDoctor(CreateDoctorDto dto)
        {
            Console.WriteLine($"[DEBUG] CreateDoctor received: UserId={dto.UserId}, Name={dto.FullName}, Fee={dto.ConsultationFee}");

            int? userIntId = null;

            // Nếu có nhập UserId thì mới parse và kiểm tra trùng
            if (!string.IsNullOrEmpty(dto.UserId))
            {
                if (int.TryParse(dto.UserId, out var parsedId))
                {
                    userIntId = parsedId;
                    
                    // Kiểm tra xem thằng này đã là bác sĩ chưa
                    var existing = await _context.Doctors.AnyAsync(d => d.UserId == userIntId);
                    if (existing)
                    {
                        return BadRequest(new { message = "Người dùng này đã là bác sĩ rồi m ơi!" });
                    }
                }
                else
                {
                    return BadRequest(new { message = "ID người dùng không hợp lệ!" });
                }
            }

            if (string.IsNullOrEmpty(dto.FullName))
            {
                return BadRequest(new { message = "Họ tên không được để trống!" });
            }

            // Parse tiền
            decimal fee = dto.ConsultationFee ?? 0;

            var newDoctor = new Doctor
            {
                Id = Guid.NewGuid(),
                UserId = userIntId,
                FullName = dto.FullName,
                Specialty = dto.Specialty ?? "Đa khoa",
                Degree = dto.Degree ?? "Bác sĩ",
                ConsultationFee = fee
            };

            _context.Doctors.Add(newDoctor);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Thêm bác sĩ thành công!", doctorId = newDoctor.Id });
        }
        [HttpGet("{id}/slots")]
        [AllowAnonymous]
        public async Task<ActionResult> GetAvailableSlots(Guid id, [FromQuery] DateTime date)
        {
            // 1. Kiểm tra bác sĩ có thật không
            var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == id);
            if (!doctorExists) return NotFound("Méo tìm thấy bác sĩ này m ơi!");

            var slotsList = await _context.DoctorSlots
                .Where(s => s.DoctorId == id && s.Date.Date == date.Date)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            // TỰ ĐỘNG CHỮA LÀNH/SINH LỊCH TỰ ĐỘNG NẾU BÁC SĨ CHƯA CÓ GIỜ KHÁM
            if (slotsList.Count == 0)
            {
                var slotsToCreate = new List<DoctorSlot>();

                // Khởi tạo ca SÁNG: 8:00 đến 11:30 (Mỗi ca 30p)
                var startTimeMorning = new TimeSpan(8, 0, 0);
                var endTimeMorning = new TimeSpan(11, 30, 0);
                while (startTimeMorning < endTimeMorning)
                {
                    slotsToCreate.Add(new DoctorSlot
                    {
                        Id = Guid.NewGuid(),
                        DoctorId = id,
                        Date = date.Date,
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
                        DoctorId = id,
                        Date = date.Date,
                        StartTime = startTimeAfternoon,
                        EndTime = startTimeAfternoon.Add(TimeSpan.FromMinutes(30)),
                        IsBooked = false
                    });
                    startTimeAfternoon = startTimeAfternoon.Add(TimeSpan.FromMinutes(30));
                }

                _context.DoctorSlots.AddRange(slotsToCreate);
                await _context.SaveChangesAsync();
                slotsList = slotsToCreate;
            }

            var result = slotsList.Select(s => new {
                s.Id,
                s.StartTime,
                s.EndTime,
                s.IsBooked
            })
            .OrderBy(s => s.StartTime)
            .ToList();

            return Ok(result);
        }

        // API 4: Lấy toàn bộ lịch trực hệ thống (Dùng cho trang Lịch trực hôm nay)
        [HttpGet("duty-schedule")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<DutyScheduleDto>>> GetDutySchedule([FromQuery] DateTime? date)
        {
            var targetDate = (date?.ToUniversalTime() ?? DateTime.UtcNow).Date;

            var dutySchedule = await _context.Doctors
                .Include(d => d.Slots)
                .Where(d => d.Slots.Any(s => s.Date.Date == targetDate))
                .Select(d => new DutyScheduleDto
                {
                    DoctorId = d.Id,
                    DoctorName = d.FullName,
                    Specialty = d.Specialty,
                    Slots = d.Slots
                        .Where(s => s.Date.Date == targetDate)
                        .OrderBy(s => s.StartTime)
                        .Select(s => new DutySlotDto
                        {
                            SlotId = s.Id,
                            StartTime = s.StartTime.ToString(@"hh\:mm"),
                            EndTime = s.EndTime.ToString(@"hh\:mm"),
                            IsBooked = s.IsBooked
                        }).ToList()
                })
                .ToListAsync();

            return Ok(dutySchedule);
        }
    }
}