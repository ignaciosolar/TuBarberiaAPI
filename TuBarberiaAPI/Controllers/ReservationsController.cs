using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuBarberiaAPI.Data;
using TuBarberiaAPI.DTOs;
using TuBarberiaAPI.Models;

namespace TuBarberiaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReservationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReservationCreateDto dto)
        {
            var service = await _context.BarberServices
                .Include(s => s.Service)
                .FirstOrDefaultAsync(s => s.Id == dto.BarberServiceId && s.UserId == dto.BarberId && s.IsActive);

            if (service == null)
                return BadRequest(new { message = "Servicio no válido para este barbero." });

            var endTime = dto.StartTime.AddMinutes(service.DurationMinutes);

            bool isOccupied = await _context.Reservations.AnyAsync(r =>
                r.BarberId == dto.BarberId &&
                r.Status == "Activa" &&
                r.StartTime < endTime &&
                r.EndTime > dto.StartTime);

            bool isBlocked = await _context.BarberBlockedTimes.AnyAsync(b =>
                b.BarberId == dto.BarberId &&
                b.StartTime < endTime &&
                b.EndTime > dto.StartTime);

            if (isOccupied || isBlocked)
                return Conflict(new { message = "Horario no disponible." });

            var reservation = new Reservation
            {
                BarberId = dto.BarberId,
                BarberServiceId = dto.BarberServiceId,
                ClientName = dto.ClientName,
                ClientPhone = dto.ClientPhone,
                ClientEmail = dto.ClientEmail, // <-- Asigna aquí
                StartTime = dto.StartTime,
                EndTime = endTime,
                Status = "Activa"
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Reserva creada con éxito." });
        }

        [HttpGet("barber/{barberId}")]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetByBarber(int barberId, [FromQuery] DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1);

            var reservations = await _context.Reservations
                .Include(r => r.BarberService)
                    .ThenInclude(bs => bs.Service)
                .Where(r => r.BarberId == barberId && r.StartTime >= start && r.StartTime < end && r.Status == "Activa")
                .Select(r => new ReservationDto
                {
                    Id = r.Id,
                    ClientName = r.ClientName,
                    ClientPhone = r.ClientPhone,
                    ServiceName = r.BarberService!.Service.Name,
                    StartTime = r.StartTime,
                    EndTime = r.EndTime,
                    Status = r.Status
                })
                .ToListAsync();

            return Ok(reservations);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null) return NotFound();

            reservation.Status = "Cancelada";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Reserva anulada." });
        }

        [HttpPost("block")]
        public async Task<IActionResult> BlockTime([FromBody] BarberBlockedTimeDto dto)
        {
            var barber = await _context.Users.FindAsync(dto.BarberId);
            if (barber == null)
                return NotFound(new { message = "Barbero no encontrado" });

            // Asegurar que las fechas están en hora local de Chile
            var chileTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific SA Standard Time"); // Para Windows
                                                                                                 // var chileTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago"); // Para Linux

            var startLocal = TimeZoneInfo.ConvertTime(dto.StartTime, chileTimeZone);
            var endLocal = TimeZoneInfo.ConvertTime(dto.EndTime, chileTimeZone);

            var bloqueo = new BarberBlockedTime
            {
                BarberId = dto.BarberId,
                StartTime = DateTime.SpecifyKind(startLocal, DateTimeKind.Local),
                EndTime = DateTime.SpecifyKind(endLocal, DateTimeKind.Local),
                Reason = dto.Reason
            };

            _context.BarberBlockedTimes.Add(bloqueo);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Bloqueo creado correctamente." });
        }

        // GET: api/reservations/blocks/{barberId}
        [HttpGet("blocks/{barberId}")]
        public async Task<IActionResult> GetBlockedTimes(int barberId)
        {
            var bloques = await _context.BarberBlockedTimes
                .Where(b => b.BarberId == barberId)
                .OrderByDescending(b => b.StartTime)
                .ToListAsync();

            return Ok(bloques.Select(b => new
            {
                b.Id,
                b.StartTime,
                b.EndTime,
                b.Reason
            }));
        }

        [HttpPut("block/{id}")]
        public async Task<IActionResult> UpdateBlockedTime(int id, [FromBody] BarberBlockedTimeDto dto)
        {
            var bloqueo = await _context.BarberBlockedTimes.FindAsync(id);
            if (bloqueo == null)
                return NotFound(new { message = "Bloqueo no encontrado." });

            var chileTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific SA Standard Time"); // Windows
            var startLocal = TimeZoneInfo.ConvertTime(dto.StartTime, chileTimeZone);
            var endLocal = TimeZoneInfo.ConvertTime(dto.EndTime, chileTimeZone);

            bloqueo.StartTime = DateTime.SpecifyKind(startLocal, DateTimeKind.Local);
            bloqueo.EndTime = DateTime.SpecifyKind(endLocal, DateTimeKind.Local);
            bloqueo.Reason = dto.Reason;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Bloqueo actualizado correctamente." });
        }


        // DELETE: api/reservations/block/{id}
        [HttpDelete("block/{id}")]
        public async Task<IActionResult> DeleteBlockedTime(int id)
        {
            var bloqueo = await _context.BarberBlockedTimes.FindAsync(id);
            if (bloqueo == null)
                return NotFound(new { message = "Bloqueo no encontrado." });

            _context.BarberBlockedTimes.Remove(bloqueo);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Bloqueo eliminado correctamente." });
        }


        [HttpGet("available-hours/{barberId}")]
        public async Task<ActionResult<IEnumerable<string>>> GetAvailableHours(int barberId, [FromQuery] DateTime date, [FromQuery] int duration)
        {
            var dayOfWeek = date.DayOfWeek;

            // ✅ Obtener todos los bloques de ese día
            var schedules = await _context.BarberSchedules
                .Where(s => s.BarberId == barberId && s.DayOfWeek == dayOfWeek)
                .ToListAsync();

            if (schedules == null || !schedules.Any())
                return Ok(new List<string>());

            var allSlots = new List<(DateTime Start, DateTime End)>();

            // ✅ Construir todos los bloques de 10 minutos por cada tramo
            foreach (var schedule in schedules)
            {
                var startDay = date.Date.Add(schedule.StartTime);
                var endDay = date.Date.Add(schedule.EndTime);

                for (var slotStart = startDay; slotStart.AddMinutes(duration) <= endDay; slotStart = slotStart.AddMinutes(30))
                {
                    var slotEnd = slotStart.AddMinutes(duration);
                    allSlots.Add((slotStart, slotEnd));
                }
            }

            // 🟨 Reservas activas del día
            var reservations = await _context.Reservations
                .Where(r => r.BarberId == barberId &&
                            r.Status == "Activa" &&
                            r.StartTime.Date == date.Date)
                .ToListAsync();

            // 🟨 Bloqueos del día
            var blocks = await _context.BarberBlockedTimes
                .Where(b => b.BarberId == barberId &&
                            b.StartTime.Date == date.Date)
                .ToListAsync();

            // ✅ Filtrar solo los tramos disponibles (sin conflicto con reservas ni bloqueos)
            var available = allSlots.Where(slot =>
                !reservations.Any(r => slot.Start < r.EndTime && slot.End > r.StartTime) &&
                !blocks.Any(b => slot.Start < b.EndTime && slot.End > b.StartTime))
                .Select(s => s.Start.ToString("HH:mm"))
                .ToList();

            return Ok(available);
        }


        [HttpGet("next-available-slot/{barberId}")]
        public async Task<IActionResult> GetNextAvailableSlot(int barberId, [FromQuery] int duration)
        {
            var today = DateTime.Now.Date;
            var now = DateTime.Now;

            for (int i = 0; i < 7; i++)
            {
                var date = today.AddDays(i);
                var dayOfWeek = date.DayOfWeek;

                var schedule = await _context.BarberSchedules
                    .FirstOrDefaultAsync(s => s.BarberId == barberId && s.DayOfWeek == dayOfWeek);

                if (schedule == null) continue;

                var dayStart = date.Add(schedule.StartTime);
                var dayEnd = date.Add(schedule.EndTime);

                var startSlot = i == 0 ? now : dayStart;

                var allSlots = new List<(DateTime Start, DateTime End)>();
                for (var t = startSlot; t.AddMinutes(duration) <= dayEnd; t = t.AddMinutes(10))
                {
                    allSlots.Add((t, t.AddMinutes(duration)));
                }

                var reservations = await _context.Reservations
                    .Where(r => r.BarberId == barberId &&
                                r.Status == "Activa" &&
                                r.StartTime.Date == date)
                    .ToListAsync();

                var blocks = await _context.BarberBlockedTimes
                    .Where(b => b.BarberId == barberId &&
                                b.StartTime.Date == date)
                    .ToListAsync();

                var availableSlot = allSlots.FirstOrDefault(slot =>
                    !reservations.Any(r => slot.Start < r.EndTime && slot.End > r.StartTime) &&
                    !blocks.Any(b => slot.Start < b.EndTime && slot.End > b.StartTime));

                if (availableSlot.Start != default)
                {
                    return Ok(new
                    {
                        nextAvailable = availableSlot.Start,
                        formatted = availableSlot.Start.ToString("yyyy-MM-dd HH:mm")
                    });
                }
            }

            return NotFound(new { message = "No hay horarios disponibles en los próximos 7 días." });
        }

        [AllowAnonymous]
        [HttpPost("public")]
        public async Task<IActionResult> CreatePublic([FromBody] ReservationCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ClientName) || string.IsNullOrWhiteSpace(dto.ClientPhone))
                return BadRequest(new { message = "Nombre y teléfono del cliente son obligatorios." });

            var barber = await _context.Users
                .Include(u => u.BarberShop)
                .FirstOrDefaultAsync(u => u.Id == dto.BarberId);

            if (barber == null)
                return BadRequest(new { message = "El barbero no existe o no está autorizado." });

            if (barber.BarberShop == null)
                return BadRequest(new { message = "El barbero no está asociado a ninguna barbería." });

            var service = await _context.BarberServices
                .Include(s => s.Service)
                .FirstOrDefaultAsync(s => s.Id == dto.BarberServiceId &&
                                          s.UserId == dto.BarberId &&
                                          s.IsActive);

            if (service == null)
                return BadRequest(new { message = "Servicio no válido para este barbero." });

            var endTime = dto.StartTime.AddMinutes(service.DurationMinutes);

            var isConflict = await _context.Reservations.AnyAsync(r =>
                r.BarberId == dto.BarberId &&
                r.Status == "Activa" &&
                r.StartTime < endTime &&
                r.EndTime > dto.StartTime);

            var isBlocked = await _context.BarberBlockedTimes.AnyAsync(b =>
                b.BarberId == dto.BarberId &&
                b.StartTime < endTime &&
                b.EndTime > dto.StartTime);

            if (isConflict || isBlocked)
                return Conflict(new { message = "Horario no disponible." });

            var reservation = new Reservation
            {
                BarberId = dto.BarberId,
                BarberServiceId = dto.BarberServiceId,
                ClientName = dto.ClientName.Trim(),
                ClientPhone = dto.ClientPhone.Trim(),
                ClientEmail = dto.ClientEmail, // <-- Asigna aquí
                StartTime = dto.StartTime,
                EndTime = endTime,
                Status = "Activa"
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Reserva realizada con éxito.",
                reservation = new
                {
                    reservation.Id,
                    reservation.ClientName,
                    reservation.StartTime,
                    reservation.EndTime
                }
            });
        }

        [HttpGet("barbershop/{barbershopId}")]
        public async Task<IActionResult> GetReservationsByBarberShop(int barbershopId, [FromQuery] DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1);

            // Obtener barberos de la barbería
            var barbers = await _context.Users
                .Where(u => u.BarberShopId == barbershopId && u.Role == "Barbero")
                .ToListAsync();

            if (!barbers.Any())
                return NotFound(new { message = "No se encontraron barberos en esta barbería." });

            var reservations = await _context.Reservations
                .Include(r => r.Barber)
                .Include(r => r.BarberService)
                    .ThenInclude(bs => bs.Service)
                .Where(r => barbers.Select(b => b.Id).Contains(r.BarberId) &&
                            r.StartTime >= start && r.StartTime < end &&
                            r.Status == "Activa")
                .ToListAsync();

            var grouped = barbers.Select(b => new GroupedReservationDto
            {
                BarberId = b.Id,
                BarberName = b.FullName,
                Reservations = reservations
                    .Where(r => r.BarberId == b.Id)
                    .Select(r => new ReservationDto
                    {
                        Id = r.Id,
                        ClientName = r.ClientName,
                        ClientPhone = r.ClientPhone,
                        ServiceName = r.BarberService!.Service.Name,
                        StartTime = r.StartTime,
                        EndTime = r.EndTime,
                        Status = r.Status
                    }).ToList()
            }).ToList();

            return Ok(grouped);
        }
    }
}
