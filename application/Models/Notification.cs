using System;
using System.ComponentModel.DataAnnotations;

namespace AppointmentService.Models
{
    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Đổi sang int? để khớp với PatientId trong hệ thống hiện tại
        public int? UserId { get; set; } 
        
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public string Type { get; set; } = "info"; // success, info, warning, danger

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
