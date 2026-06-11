using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppointmentService.Data;
using AppointmentService.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AppointmentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly AppointmentDbContext _context;

        public NotificationsController(AppointmentDbContext context)
        {
            _context = context;
        }

        // Lấy thông báo của người dùng hiện tại
        [HttpGet("my")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            
            if (!int.TryParse(userIdStr, out int userId)) return BadRequest("Invalid User ID");

            var list = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .ToListAsync();

            return Ok(list);
        }

        // Lấy thông báo hệ thống (Dành cho Admin/Receptionist - Những thông báo không có UserId)
        [HttpGet("system")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> GetSystemNotifications()
        {
            var list = await _context.Notifications
                .Where(n => n.UserId == null)
                .OrderByDescending(n => n.CreatedAt)
                .Take(30)
                .ToListAsync();

            return Ok(list);
        }

        // Đánh dấu đã đọc
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var notif = await _context.Notifications.FindAsync(id);
            if (notif == null) return NotFound();

            notif.IsRead = true;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // Tạo thông báo mới từ các dịch vụ khác (ví dụ: khi ghi xong bệnh án)
        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] NotificationDto dto)
        {
            return await CreateNotificationInternal(dto);
        }

        // Endpoint nội bộ cho service-to-service (không cần JWT)
        [HttpPost("create-direct")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateNotificationDirect([FromBody] NotificationDto dto)
        {
            var apiKey = Request.Headers["X-Service-API-Key"].FirstOrDefault();
            if (apiKey != "MedicareServiceInternalKey2024")
                return Unauthorized(new { message = "Invalid service API key." });
            return await CreateNotificationInternal(dto);
        }

        private async Task<IActionResult> CreateNotificationInternal(NotificationDto dto)
        {
            if (dto == null) return BadRequest("Dữ liệu trống");

            var notif = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type ?? "info",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notif);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, notificationId = notif.Id });
        }
    }

    public class NotificationDto
    {
        public int? UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Type { get; set; }
    }
}
