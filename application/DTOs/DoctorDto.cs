#nullable enable
namespace AppointmentService.DTOs
{
    // DTO này dùng để trả dữ liệu cho Frontend hiển thị danh sách bác sĩ
    public class DoctorResponseDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; }
    }

    // DTO này dùng khi Admin (N3) muốn thêm 1 bác sĩ mới vào DB
    public class CreateDoctorDto
    {
        public string? UserId { get; set; } // Giờ có thể để trống, không bắt buộc
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string Degree { get; set; } = string.Empty;
        public decimal? ConsultationFee { get; set; }
    }
}