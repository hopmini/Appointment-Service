using System.Text;
using System.Text.Json;

namespace AppointmentService.Services
{
    public interface IExternalSyncService
    {
        Task<bool> SyncAppointmentToMedicalRecord(Guid appointmentId, int patientId, Guid doctorId, DateTime date);
        Task<bool> PushPrescriptionToPharmacy(Guid appointmentId, string patientName, decimal totalAmount);
    }

    public class ExternalSyncService : IExternalSyncService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExternalSyncService> _logger;
        private readonly IConfiguration _configuration;

        public ExternalSyncService(HttpClient httpClient, ILogger<ExternalSyncService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        // Gửi thông tin cuộc hẹn sang Medical Record Service
        public async Task<bool> SyncAppointmentToMedicalRecord(Guid appointmentId, int patientId, Guid doctorId, DateTime date)
        {
            var medicalRecordUrl = _configuration["MEDICAL_RECORD_SERVICE_URL"] ?? "http://localhost:8001";
            _logger.LogInformation($"[SYNC] Đồng bộ lịch hẹn {appointmentId} sang Medical Record Service...");

            var payload = new
            {
                AppointmentId = appointmentId,
                PatientId = patientId,
                DoctorId = doctorId,
                AppointmentDate = date,
                Status = "confirmed",
                Timestamp = DateTime.Now
            };

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{medicalRecordUrl}/api/appointments/sync", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"[SUCCESS] Đã đồng bộ lịch hẹn {appointmentId} sang Medical Record Service");
                    return true;
                }

                _logger.LogWarning($"[WARNING] Medical Record Service trả về {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Không thể kết nối Medical Record Service: {ex.Message}");
                return false;
            }
        }

        // Gửi thông tin thanh toán sang Pharmacy & Billing Service
        public async Task<bool> PushPrescriptionToPharmacy(Guid appointmentId, string patientName, decimal totalAmount)
        {
            var pharmacyUrl = _configuration["PHARMACY_SERVICE_URL"] ?? "http://localhost:8002";
            _logger.LogInformation($"[SYNC] Đẩy thông tin thanh toán {appointmentId} sang Pharmacy & Billing Service...");

            var payload = new
            {
                AppointmentId = appointmentId,
                PatientName = patientName,
                Amount = totalAmount,
                ServiceType = "consultation",
                Timestamp = DateTime.Now
            };

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{pharmacyUrl}/api/billing/appointments", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"[SUCCESS] Đã đẩy thanh toán {appointmentId} sang Pharmacy Service");
                    return true;
                }

                _logger.LogWarning($"[WARNING] Pharmacy Service trả về {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Không thể kết nối Pharmacy Service: {ex.Message}");
                return false;
            }
        }
    }
}