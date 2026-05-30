using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppointmentService.Data;
using AppointmentService.Models;
using Microsoft.AspNetCore.Authorization;

namespace AppointmentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MedicalServicesController : ControllerBase
    {
        private readonly AppointmentDbContext _context;

        public MedicalServicesController(AppointmentDbContext context)
        {
            _context = context;
        }

        // Public API for everyone to see
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MedicalService>>> GetServices()
        {
            return await _context.MedicalServices.Where(s => s.IsActive).ToListAsync();
        }

        // Admin only APIs
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<MedicalService>> CreateService(MedicalService service)
        {
            service.Id = Guid.NewGuid();
            _context.MedicalServices.Add(service);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetServices), new { id = service.Id }, service);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateService(Guid id, MedicalService service)
        {
            if (id != service.Id) return BadRequest();

            _context.Entry(service).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ServiceExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteService(Guid id)
        {
            var service = await _context.MedicalServices.FindAsync(id);
            if (service == null) return NotFound();

            service.IsActive = false; // Soft delete
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ServiceExists(Guid id)
        {
            return _context.MedicalServices.Any(e => e.Id == id);
        }
    }
}
