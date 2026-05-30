#nullable enable
namespace AppointmentService.DTOs
{
    // DTO để gọi lệnh tự động đẻ ra slot
    public class GenerateSlotDto
    {
        public Guid DoctorId { get; set; }
        public DateTime Date { get; set; }
    }

    // DTO khi bệnh nhân gửi yêu cầu đặt lịch
    public class BookAppointmentDto
    {
        public int PatientId { get; set; } // Bắt buộc phải đăng nhập
        public Guid SlotId { get; set; }    // Slot vừa chọn
        public Guid MedicalServiceId { get; set; } // Dịch vụ khám chọn từ danh sách
        public string Reason { get; set; } = string.Empty;  // Triệu chứng nhức đầu sổ mũi...
    }
}