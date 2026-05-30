using Microsoft.EntityFrameworkCore;
using AppointmentService.Models;

namespace AppointmentService.Data
{
    public class AppointmentDbContext : DbContext
    {
        public AppointmentDbContext(DbContextOptions<AppointmentDbContext> options)
            : base(options) { }

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<DoctorSlot> DoctorSlots { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<MedicalService> MedicalServices { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ép kiểu cho cột tiền tệ để DB không bị ngáo
            modelBuilder.Entity<Doctor>()
                .Property(d => d.ConsultationFee)
                .HasColumnType("numeric(18,2)");

            modelBuilder.Entity<MedicalService>()
                .Property(s => s.Price)
                .HasColumnType("numeric(18,2)");

            // Chống xung đột đặt lịch bằng xmin (Concurrency Token hệ thống của PostgreSQL)
            modelBuilder.Entity<DoctorSlot>()
                .UseXminAsConcurrencyToken();
        }
    }
}