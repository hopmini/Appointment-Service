using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using AppointmentService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AppointmentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppointmentDbContext _context;
        private readonly HttpClient _httpClient;
        private const string GROQ_API_URL = "https://api.groq.com/openai/v1/chat/completions";

        public ChatController(IConfiguration configuration, AppointmentDbContext context)
        {
            _configuration = configuration;
            _context = context;
            _httpClient = new HttpClient();
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                var apiKey = _configuration["GROQ_API_KEY"] ?? "";
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Guest";

                // 1. THU THẬP DỮ LIỆU NGỮ CẢNH (CONTEXT)
                var doctors = await _context.Doctors.Select(d => new { d.FullName, d.Specialty, d.ConsultationFee }).ToListAsync();
                var services = await _context.MedicalServices.Select(s => new { s.Name, s.Price }).ToListAsync();
                
                var totalAppointments = await _context.Appointments.CountAsync();
                var pendingCount = await _context.Appointments.CountAsync(a => a.Status == 0);
                
                string adminData = "";
                if (userRole == "Admin" || userRole == "Receptionist")
                {
                    var totalRevenue = await _context.Appointments
                        .Include(a => a.MedicalService)
                        .Where(a => a.Status == 1)
                        .SumAsync(a => a.MedicalService != null ? a.MedicalService.Price : 0);
                    adminData = $"- TỔNG DOANH THU: {totalRevenue:N0} VNĐ\n- ĐƠN CHỜ DUYỆT: {pendingCount} đơn\n- TỔNG SỐ LỊCH HẸN: {totalAppointments}";
                }

                // 2. XÂY DỰNG SYSTEM PROMPT
                string systemPrompt = $@"Bạn là Medicare AI, trợ lý ảo của Medicare Hospital.
DỮ LIỆU THỰC TẾ:
- BÁC SĨ: {JsonSerializer.Serialize(doctors)}
- DỊCH VỤ: {JsonSerializer.Serialize(services)}
{(string.IsNullOrEmpty(adminData) ? "" : "\nTHÔNG TIN QUẢN TRỊ:\n" + adminData)}

HƯỚNG DẪN:
- Khách (Guest): Tư vấn khám, bác sĩ, dịch vụ. KHÔNG báo doanh thu.
- Admin: Báo cáo số liệu doanh thu, lịch hẹn.
- Trả lời thân thiện, ngắn gọn bằng tiếng Việt.";

                // 3. GỌI GROQ API (DÙNG MODEL MỚI NHẤT)
                var groqRequest = new
                {
                    model = "llama-3.1-8b-instant",
                    messages = new[] 
                    { 
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = request.Message } 
                    },
                    temperature = 0.7
                };

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, GROQ_API_URL);
                httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
                httpRequest.Content = new StringContent(JsonSerializer.Serialize(groqRequest), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(httpRequest);
                var resultText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Groq API Error: {resultText}");
                    return Ok(new { response = "Xin lỗi, tôi đang gặp chút trục trặc khi kết nối với bộ não AI. Bạn thử hỏi lại câu khác ngắn hơn nhé!" });
                }

                var groqResult = JsonSerializer.Deserialize<JsonElement>(resultText);
                var aiResponse = groqResult.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                return Ok(new { response = aiResponse });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chat Error: {ex.Message}");
                return Ok(new { response = "Hệ thống đang bận một chút. Tôi sẽ quay lại ngay!" });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}
