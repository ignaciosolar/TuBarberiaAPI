using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TuBarberiaAPI.Data;
using TuBarberiaAPI.DTOs;
using TuBarberiaAPI.Models;
using TuBarberiaAPI.Services;

namespace TuBarberiaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;

        public ReservationsController(AppDbContext context, EmailService emailService, IConfiguration config)
        {
            _context = context;
            _emailService = emailService;
            _config = config;
        }

        // =========================
        // Crear reserva (privado)
        // =========================
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
                ClientEmail = dto.ClientEmail, // ya lo tenías
                StartTime = dto.StartTime,
                EndTime = endTime,
                Status = "Activa"
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Emails: barbero + cliente
            await SendBarberReservationCreatedEmail(reservation.Id);
            await SendClientReservationCreatedEmail(reservation.Id);

            return Ok(new { message = "Reserva creada con éxito." });
        }

        // ===================================
        // Crear reserva pública (sin auth)
        // ===================================
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
                ClientEmail = dto.ClientEmail,
                StartTime = dto.StartTime,
                EndTime = endTime,
                Status = "Activa"
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Emails: barbero + cliente
            await SendBarberReservationCreatedEmail(reservation.Id);
            await SendClientReservationCreatedEmail(reservation.Id);

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

        // ===================================
        // Anular por token (link público)
        // ===================================
        [AllowAnonymous]
        [HttpGet("cancel-by-token")]
        public async Task<IActionResult> CancelByToken([FromQuery] string token)
        {
            var principal = ValidateActionToken(token, out string? reason);
            if (principal == null)
                return BadRequest(new { message = reason ?? "Token inválido." });

            var resIdClaim = principal.FindFirst("resId")?.Value;
            var purpose = principal.FindFirst("purpose")?.Value;

            if (purpose != "cancel" || string.IsNullOrEmpty(resIdClaim) || !int.TryParse(resIdClaim, out int resId))
                return BadRequest(new { message = "Token no válido para anular." });

            var reservation = await _context.Reservations.FindAsync(resId);
            if (reservation == null)
                return NotFound(new { message = "Reserva no encontrada." });

            if (reservation.Status != "Cancelada")
            {
                reservation.Status = "Cancelada";
                await _context.SaveChangesAsync();

                await SendBarberReservationCancelledEmail(reservation.Id);
                await SendClientReservationCancelledEmail(reservation.Id);
            }

            var html = @"<!doctype html><html><head><meta charset='utf-8'><title>Reserva anulada</title></head>
            <body style='font-family:Arial,Helvetica,sans-serif'>
              <h2>Reserva anulada</h2>
              <p>La reserva ha sido anulada correctamente.</p>
            </body></html>";
            return Content(html, "text/html");
        }

        // ==============================
        // Listado por barbero (existente)
        // ==============================
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

        // ===========================
        // Anulación (panel interno)
        // ===========================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null) return NotFound();

            if (reservation.Status != "Cancelada")
            {
                reservation.Status = "Cancelada";
                await _context.SaveChangesAsync();

                await SendBarberReservationCancelledEmail(reservation.Id);
                await SendClientReservationCancelledEmail(reservation.Id);
            }

            return Ok(new { message = "Reserva anulada." });
        }

        // ===========================
        // Bloqueos (tus endpoints)
        // ===========================
        [HttpPost("block")]
        public async Task<IActionResult> BlockTime([FromBody] BarberBlockedTimeDto dto)
        {
            var barber = await _context.Users.FindAsync(dto.BarberId);
            if (barber == null)
                return NotFound(new { message = "Barbero no encontrado" });

            var tz = GetChileTimeZone();
            var startLocal = TimeZoneInfo.ConvertTime(dto.StartTime, tz);
            var endLocal = TimeZoneInfo.ConvertTime(dto.EndTime, tz);

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

            var tz = GetChileTimeZone();
            var startLocal = TimeZoneInfo.ConvertTime(dto.StartTime, tz);
            var endLocal = TimeZoneInfo.ConvertTime(dto.EndTime, tz);

            bloqueo.StartTime = DateTime.SpecifyKind(startLocal, DateTimeKind.Local);
            bloqueo.EndTime = DateTime.SpecifyKind(endLocal, DateTimeKind.Local);
            bloqueo.Reason = dto.Reason;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Bloqueo actualizado correctamente." });
        }

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

        // ===========================
        // Disponibilidad (existente)
        // ===========================
        [HttpGet("available-hours/{barberId}")]
        public async Task<ActionResult<IEnumerable<string>>> GetAvailableHours(int barberId, [FromQuery] DateTime date, [FromQuery] int duration)
        {
            var dayOfWeek = date.DayOfWeek;

            var schedules = await _context.BarberSchedules
                .Where(s => s.BarberId == barberId && s.DayOfWeek == dayOfWeek)
                .ToListAsync();

            if (schedules == null || !schedules.Any())
                return Ok(new List<string>());

            var allSlots = new List<(DateTime Start, DateTime End)>();
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

            var reservations = await _context.Reservations
                .Where(r => r.BarberId == barberId &&
                            r.Status == "Activa" &&
                            r.StartTime.Date == date.Date)
                .ToListAsync();

            var blocks = await _context.BarberBlockedTimes
                .Where(b => b.BarberId == barberId &&
                            b.StartTime.Date == date.Date)
                .ToListAsync();

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

        // ===========================
        // Helpers de correo / tiempo
        // ===========================
        private TimeZoneInfo GetChileTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Pacific SA Standard Time"); } // Windows
            catch { return TimeZoneInfo.FindSystemTimeZoneById("America/Santiago"); }       // Linux
        }

        private (string DiaSemana, string Fecha, string Relativo, string Hora) BuildDateParts(DateTime startUtc)
        {
            var tz = GetChileTimeZone();
            var startLocal = TimeZoneInfo.ConvertTime(startUtc, tz);
            var culture = new CultureInfo("es-CL");

            var diaSemana = culture.DateTimeFormat.GetDayName(startLocal.DayOfWeek);
            diaSemana = char.ToUpper(diaSemana[0], culture) + diaSemana.Substring(1);
            var fecha = startLocal.ToString("dd-MM-yyyy");
            var hora = startLocal.ToString("HH:mm");

            var hoyLocal = TimeZoneInfo.ConvertTime(DateTime.Now, tz).Date;
            var diasRestantes = (startLocal.Date - hoyLocal).Days;
            var relativo = diasRestantes == 0 ? "hoy" :
                           diasRestantes == 1 ? "mañana" :
                           diasRestantes > 1 ? $"en {diasRestantes} días" :
                           $"hace {-diasRestantes} días";

            return (diaSemana, fecha, relativo, hora);
        }

        private string GetEmailTemplate(string fileName)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", fileName);
            return System.IO.File.ReadAllText(path);
        }

        private string RenderTemplate(string templateName, Dictionary<string, string> data)
        {
            var tpl = GetEmailTemplate(templateName);
            foreach (var kv in data)
                tpl = tpl.Replace("{{" + kv.Key + "}}", kv.Value ?? string.Empty);
            return tpl.Replace("{{Anio}}", DateTime.Now.Year.ToString());
        }

        private string GenerateActionToken(int reservationId, string purpose, TimeSpan ttl)
        {
            var key = _config["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Jwt:Key no configurado.");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim("purpose", purpose),
                new Claim("resId", reservationId.ToString())
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.Add(ttl),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private ClaimsPrincipal? ValidateActionToken(string token, out string? reason)
        {
            reason = null;
            try
            {
                var key = _config["Jwt:Key"];
                var tokenHandler = new JwtSecurityTokenHandler();
                var parameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!)),
                    ClockSkew = TimeSpan.FromMinutes(2)
                };

                var principal = tokenHandler.ValidateToken(token, parameters, out _);
                return principal;
            }
            catch (SecurityTokenExpiredException)
            {
                reason = "El enlace ha expirado.";
                return null;
            }
            catch
            {
                reason = "Token inválido.";
                return null;
            }
        }

        // ===========================
        // Envíos concretos
        // ===========================
        private async Task SendBarberReservationCreatedEmail(int reservationId)
        {
            var res = await _context.Reservations
                .Include(r => r.Barber).ThenInclude(b => b.BarberShop)
                .Include(r => r.BarberService)!.ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (res?.Barber == null || res.BarberService == null) return;

            var (dia, fecha, rel, hora) = BuildDateParts(res.StartTime);

            var cancelToken = GenerateActionToken(res.Id, "cancel", TimeSpan.FromDays(30));
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var cancelUrl = $"{baseUrl}/api/reservations/cancel-by-token?token={Uri.EscapeDataString(cancelToken)}";

            var html = RenderTemplate("BarberNotification.html", new Dictionary<string, string>
            {
                ["Barbero"] = res.Barber.FullName,
                ["Barberia"] = res.Barber.BarberShop?.Name ?? "TuBarbería",
                ["Cliente"] = res.ClientName,
                ["Servicio"] = res.BarberService.Service.Name,
                ["DiaSemana"] = dia,
                ["Fecha"] = fecha,
                ["Relativo"] = rel,
                ["Hora"] = hora,
                ["CancelUrl"] = cancelUrl
            });

            if (!string.IsNullOrWhiteSpace(res.Barber.Email))
            {
                await _emailService.SendEmailAsync(
                    res.Barber.Email,
                    $"Nueva reserva: {fecha} {hora}",
                    html,
                    isHtml: true
                );
            }
        }

        private async Task SendClientReservationCreatedEmail(int reservationId)
        {
            var res = await _context.Reservations
                .Include(r => r.Barber).ThenInclude(b => b.BarberShop)
                .Include(r => r.BarberService)!.ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (res == null || string.IsNullOrWhiteSpace(res.ClientEmail)) return;

            var (dia, fecha, rel, hora) = BuildDateParts(res.StartTime);

            var html = RenderTemplate("ClientReservationSuccess.html", new Dictionary<string, string>
            {
                ["Cliente"] = res.ClientName,
                ["Barberia"] = res.Barber?.BarberShop?.Name ?? "TuBarbería",
                ["Barbero"] = res.Barber?.FullName ?? "Barbero",
                ["Servicio"] = res.BarberService!.Service.Name,
                ["DiaSemana"] = dia,
                ["Fecha"] = fecha,
                ["Relativo"] = rel,
                ["Hora"] = hora
            });

            await _emailService.SendEmailAsync(
                res.ClientEmail,
                $"Tu reserva en {res.Barber?.BarberShop?.Name ?? "TuBarbería"}",
                html,
                isHtml: true
            );
        }

        private async Task SendBarberReservationCancelledEmail(int reservationId)
        {
            var res = await _context.Reservations
                .Include(r => r.Barber).ThenInclude(b => b.BarberShop)
                .Include(r => r.BarberService)!.ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (res?.Barber == null || res.BarberService == null) return;

            var (dia, fecha, _, hora) = BuildDateParts(res.StartTime);

            var html = RenderTemplate("BarberCancellation.html", new Dictionary<string, string>
            {
                ["Barbero"] = res.Barber.FullName,
                ["Barberia"] = res.Barber.BarberShop?.Name ?? "TuBarbería",
                ["Cliente"] = res.ClientName,
                ["Servicio"] = res.BarberService.Service.Name,
                ["DiaSemana"] = dia,
                ["Fecha"] = fecha,
                ["Hora"] = hora
            });

            if (!string.IsNullOrWhiteSpace(res.Barber.Email))
            {
                await _emailService.SendEmailAsync(
                    res.Barber.Email,
                    $"Reserva anulada: {fecha} {hora}",
                    html,
                    isHtml: true
                );
            }
        }

        private async Task SendClientReservationCancelledEmail(int reservationId)
        {
            var res = await _context.Reservations
                .Include(r => r.Barber).ThenInclude(b => b.BarberShop)
                .Include(r => r.BarberService)!.ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (res == null || string.IsNullOrWhiteSpace(res.ClientEmail)) return;

            var (dia, fecha, _, hora) = BuildDateParts(res.StartTime);

            var html = RenderTemplate("ClientCancellation.html", new Dictionary<string, string>
            {
                ["Cliente"] = res.ClientName,
                ["Barberia"] = res.Barber?.BarberShop?.Name ?? "TuBarbería",
                ["Barbero"] = res.Barber?.FullName ?? "Barbero",
                ["Servicio"] = res.BarberService!.Service.Name,
                ["DiaSemana"] = dia,
                ["Fecha"] = fecha,
                ["Hora"] = hora
            });

            await _emailService.SendEmailAsync(
                res.ClientEmail,
                $"Tu reserva fue anulada",
                html,
                isHtml: true
            );
        }
    }
}
