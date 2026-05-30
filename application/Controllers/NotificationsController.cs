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
    }
}
