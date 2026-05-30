using System;
using System.Collections.Generic;

namespace AppointmentService.DTOs
{
    public class DutyScheduleDto
    {
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public List<DutySlotDto> Slots { get; set; } = new List<DutySlotDto>();
    }

    public class DutySlotDto
    {
        public Guid SlotId { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public bool IsBooked { get; set; }
    }
}
