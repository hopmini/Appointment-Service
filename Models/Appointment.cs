#nullable enable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppointmentService.Models
{
    public class Appointment
    {
        [Key]
        public Guid Id { get; set; }

        // Link tới User Id (kiểu int)
        public int PatientId { get; set; }

        public Guid? MedicalServiceId { get; set; }
        [ForeignKey("MedicalServiceId")]
        public MedicalService MedicalService { get; set; } = default!;

        public Guid SlotId { get; set; }
        [ForeignKey("SlotId")]
        public DoctorSlot Slot { get; set; } = default!;

        // 0: Chờ duyệt, 1: Đã duyệt/Chờ khám, 2: Xong, 3: Hủy
        public int Status { get; set; } = 0;

        // Chờ tiếp tân duyệt xong mới cấp số thứ tự
        public int? QueueNumber { get; set; }

        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty; 

        // Token dùng để tạo mã QR cho bệnh nhân quét
        public string QRToken { get; set; } = Guid.NewGuid().ToString("N"); // Triệu chứng note lại

        // Thời gian khám dự kiến (phút)
        public int ExaminationDuration { get; set; } = 30;
    }
}