using System.ComponentModel.DataAnnotations;

namespace AppointmentService.Models
{
    public class MedicalService
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public string Category { get; set; } // Chẩn đoán, Xét nghiệm, v.v.

        public bool IsActive { get; set; } = true;
    }
}
