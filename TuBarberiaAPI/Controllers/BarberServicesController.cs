using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuBarberiaAPI.Data;
using TuBarberiaAPI.DTOs;
using TuBarberiaAPI.Models;

namespace TuBarberiaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BarberServicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BarberServicesController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/barberservices/{userId}
        [HttpPost("{userId}")]
        public async Task<IActionResult> AssignServiceToBarber(int userId, [FromBody] BarberServiceCreateDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            var service = await _context.Services.FindAsync(dto.ServiceId);

            if (user == null || service == null)
                return NotFound(new { message = "Usuario o servicio no encontrado." });

            var barberService = new BarberService
            {
                UserId = userId,
                ServiceId = dto.ServiceId,
                Price = dto.Price,
                DurationMinutes = dto.DurationMinutes,
                IsActive = dto.IsActive
            };

            _context.BarberServices.Add(barberService);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Servicio asignado al barbero." });
        }

        // GET: api/barberservices/{userId}
        [HttpGet("{userId}")]
        public async Task<ActionResult<IEnumerable<BarberServiceDto>>> GetServicesByBarber(int userId)
        {
            var services = await _context.BarberServices
                .Include(bs => bs.Service)
                .Where(bs => bs.UserId == userId)
                .Select(bs => new BarberServiceDto
                {
                    Id = bs.Id,
                    ServiceName = bs.Service.Name,
                    Price = bs.Price,
                    DurationMinutes = bs.DurationMinutes,
                    IsActive = bs.IsActive
                })
                .ToListAsync();

            return Ok(services);
        }

        // ✅ PUT: api/barberservices/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBarberService(int id, [FromBody] BarberServiceDto dto)
        {
            var barberService = await _context.BarberServices.Include(bs => bs.Service).FirstOrDefaultAsync(bs => bs.Id == id);
            if (barberService == null)
                return NotFound(new { message = "Servicio no encontrado." });

            barberService.Price = dto.Price;
            barberService.DurationMinutes = dto.DurationMinutes;
            barberService.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Servicio actualizado correctamente." });
        }

        // ✅ DELETE: api/barberservices/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBarberService(int id)
        {
            var barberService = await _context.BarberServices.FindAsync(id);
            if (barberService == null)
                return NotFound(new { message = "Servicio no encontrado." });

            _context.BarberServices.Remove(barberService);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Servicio eliminado correctamente." });
        }
    }
}
