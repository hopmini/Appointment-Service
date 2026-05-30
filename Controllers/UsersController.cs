using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppointmentService.Data;
using AppointmentService.Models;
using Microsoft.AspNetCore.Authorization;

namespace AppointmentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Receptionist")]
    public class UsersController : ControllerBase
    {
        private readonly AppointmentDbContext _context;

        public UsersController(AppointmentDbContext context)
        {
            _context = context;
        }

        [HttpGet("patients")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPatients()
        {
            var patients = await _context.Users
                .Where(u => u.Role == "Patient")
                .Select(u => new {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.Email,
                    u.Role
                })
                .ToListAsync();

            return Ok(patients);
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.Email,
                    u.Role
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("doctors")]
        public async Task<IActionResult> GetDoctors()
        {
             // This might overlap with DoctorsController but gives user info
            var doctors = await _context.Users
                .Where(u => u.Role == "Doctor")
                .Select(u => new {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.Email,
                    u.Role
                })
                .ToListAsync();

            return Ok(doctors);
        }
    }
}
