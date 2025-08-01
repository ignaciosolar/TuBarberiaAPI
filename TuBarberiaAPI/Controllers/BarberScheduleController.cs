using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuBarberiaAPI.Data;
using TuBarberiaAPI.DTOs;
using TuBarberiaAPI.Models;

namespace TuBarberiaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BarberScheduleController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BarberScheduleController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> SetSchedule([FromBody] BarberScheduleCreateDto dto)
        {
            var barber = await _context.Users.FindAsync(dto.BarberId);
            if (barber == null)
                return NotFound(new { message = "Barbero no encontrado." });

            // Eliminar horarios anteriores
            var existing = await _context.BarberSchedules
                .Where(s => s.BarberId == dto.BarberId)
                .ToListAsync();

            _context.BarberSchedules.RemoveRange(existing);

            foreach (var kv in dto.Schedules)
            {
                var day = kv.Key;
                var bloques = kv.Value;

                foreach (var bloque in bloques)
                {
                    if (!TimeSpan.TryParse(bloque.StartTime, out var start) ||
                        !TimeSpan.TryParse(bloque.EndTime, out var end))
                    {
                        return BadRequest(new { message = $"Formato de hora inválido en {day}" });
                    }

                    if (start >= end)
                    {
                        return BadRequest(new { message = $"Hora inicio debe ser menor que fin en {day}" });
                    }

                    _context.BarberSchedules.Add(new BarberSchedule
                    {
                        BarberId = dto.BarberId,
                        DayOfWeek = day,
                        StartTime = start,
                        EndTime = end
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Horario actualizado correctamente con múltiples bloques por día." });
        }

        [HttpGet("{barberId}")]
        public async Task<ActionResult<Dictionary<DayOfWeek, List<TimeBlockDto>>>> GetSchedule(int barberId)
        {
            var schedules = await _context.BarberSchedules
                .Where(s => s.BarberId == barberId)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            var grouped = schedules
                .GroupBy(s => s.DayOfWeek)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(b => new TimeBlockDto
                    {
                        StartTime = b.StartTime.ToString(@"hh\:mm"),
                        EndTime = b.EndTime.ToString(@"hh\:mm")
                    }).ToList()
                );

            return Ok(grouped);
        }
    }
}
