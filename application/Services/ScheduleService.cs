using AppointmentService.Data;
using AppointmentService.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Services
{
    public interface IScheduleService
    {
        Task<int> AutoGenerateSlotsAsync(int daysAhead);
    }

    public class ScheduleService : IScheduleService
    {
        private readonly AppointmentDbContext _context;

        public ScheduleService(AppointmentDbContext context)
        {
            _context = context;
        }

        public async Task<int> AutoGenerateSlotsAsync(int daysAhead)
        {
            var doctors = await _context.Doctors.ToListAsync();
            var today = DateTime.Today;
            int createdCount = 0;

            // Khung giờ sáng: 08:00 - 11:30 (30p mỗi slot)
            var morningSlots = new List<TimeSpan>
            {
                new TimeSpan(8, 0, 0), new TimeSpan(8, 30, 0),
                new TimeSpan(9, 0, 0), new TimeSpan(9, 30, 0),
                new TimeSpan(10, 0, 0), new TimeSpan(10, 30, 0),
                new TimeSpan(11, 0, 0)
            };

            // Khung giờ chiều: 13:30 - 16:30 (30p mỗi slot)
            var afternoonSlots = new List<TimeSpan>
            {
                new TimeSpan(13, 30, 0), new TimeSpan(14, 0, 0),
                new TimeSpan(14, 30, 0), new TimeSpan(15, 0, 0),
                new TimeSpan(15, 30, 0), new TimeSpan(16, 0, 0)
            };

            var allTimeSlots = morningSlots.Concat(afternoonSlots).ToList();

            foreach (var doctor in doctors)
            {
                for (int i = 0; i < daysAhead; i++)
                {
                    var targetDate = today.AddDays(i);

                    // Kiểm tra xem ngày này bác sĩ đã có lịch chưa
                    var existingSlots = await _context.DoctorSlots
                        .AnyAsync(s => s.DoctorId == doctor.Id && s.Date == targetDate);

                    if (!existingSlots)
                    {
                        foreach (var time in allTimeSlots)
                        {
                            _context.DoctorSlots.Add(new DoctorSlot
                            {
                                Id = Guid.NewGuid(),
                                DoctorId = doctor.Id,
                                Date = targetDate,
                                StartTime = time,
                                EndTime = time.Add(new TimeSpan(0, 30, 0)),
                                IsBooked = false
                            });
                            createdCount++;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return createdCount;
        }
    }
}
