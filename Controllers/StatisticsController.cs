using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppointmentService.Data;
using Microsoft.AspNetCore.Authorization;

namespace AppointmentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Receptionist")]
    public class StatisticsController : ControllerBase
    {
        private readonly AppointmentDbContext _context;

        public StatisticsController(AppointmentDbContext context)
        {
            _context = context;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            // 1. KPI CƠ BẢN - Truy vấn tách biệt để đảm bảo chính xác
            var totalAppointments = await _context.Appointments.CountAsync();
            var pendingCount = await _context.Appointments.CountAsync(a => a.Status == 0);
            var approvedCount = await _context.Appointments.CountAsync(a => a.Status == 1);
            var cancelledCount = await _context.Appointments.CountAsync(a => a.Status == 2);
            
            // Tính doanh thu thật (Giá dịch vụ + Giá khám bác sĩ)
            var appointmentsWithFees = await _context.Appointments
                .Include(a => a.MedicalService)
                .Include(a => a.Slot)
                    .ThenInclude(s => s.Doctor)
                .Where(a => a.Status == 1)
                .ToListAsync();
                
            decimal totalRevenue = 0;
            foreach (var app in appointmentsWithFees)
            {
                var servicePrice = app.MedicalService?.Price ?? 0;
                var doctorFee = app.Slot?.Doctor?.ConsultationFee ?? 0;
                totalRevenue += (servicePrice + doctorFee);
            }

            Console.WriteLine($"[Revenue Check] Count: {appointmentsWithFees.Count}, Total: {totalRevenue}");

            // 2. DỮ LIỆU BIỂU ĐỒ MIỀN (7 NGÀY GẦN NHẤT)
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            // Lấy toàn bộ slot đã đặt trong 7 ngày
            var bookedSlots = await _context.DoctorSlots
                .Where(s => s.Date >= last7Days.First() && s.IsBooked)
                .ToListAsync();

            var chartData = last7Days.Select(d => new 
            { 
                Label = d.ToString("dd/MM"), 
                Value = bookedSlots.Count(s => s.Date.Date == d.Date) 
            }).ToList();

            // 3. DỮ LIỆU BIỂU ĐỒ TRÒN
            var statusDist = new[]
            {
                new { Label = "Chờ duyệt", Value = pendingCount, Color = "#f59e0b" },
                new { Label = "Đã duyệt", Value = approvedCount, Color = "#10b981" },
                new { Label = "Đã hủy", Value = cancelledCount, Color = "#ef4444" }
            };

            // 4. TOP DỊCH VỤ
            var services = await _context.Appointments
                .Include(a => a.MedicalService)
                .Where(a => a.MedicalService != null)
                .ToListAsync();

            var topServices = services
                .GroupBy(a => a.MedicalService.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(4)
                .ToList();

            return Ok(new
            {
                Kpis = new { TotalAppointments = totalAppointments, PendingCount = pendingCount, TotalRevenue = totalRevenue },
                Trends = chartData,
                Distribution = statusDist,
                TopServices = topServices
            });
        }
    }
}
