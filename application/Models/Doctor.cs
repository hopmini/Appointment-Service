using System.ComponentModel.DataAnnotations;

namespace AppointmentService.Models
{
    public class Doctor
    {
        [Key]
        public Guid Id { get; set; }

        // Cực quan trọng: Lưu ID từ cái token JWT thằng N3 ném cho
        public int? UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [MaxLength(100)]
        public string Specialty { get; set; } // Chuyên khoa

        public string Degree { get; set; } // Bằng cấp

        public decimal ConsultationFee { get; set; } // Giá tiền khám

        // Navigation property để Entity Framework tự hiểu quan hệ 1-N
        public ICollection<DoctorSlot> Slots { get; set; }
    }
}