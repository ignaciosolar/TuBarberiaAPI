using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TuBarberiaAPI.DTOs;
using TuBarberiaAPI.Services;

namespace TuBarberiaAPI.Controllers
{
    [ApiController]
    [Route("api/debug-email")]
    public class EmailTestController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public EmailTestController(EmailService emailService, IConfiguration config, IWebHostEnvironment env)
        {
            _emailService = emailService;
            _config = config;
            _env = env;
        }

        // Solo habilitar si: entorno Desarrollo o flag en appsettings: "Email:EnableTestEndpoint": true
        [HttpPost("send")]
        [AllowAnonymous]
        public async Task<IActionResult> Send([FromBody] TestEmailDto dto)
        {
            var enabled = _env.IsDevelopment() ||
                          string.Equals(_config["Email:EnableTestEndpoint"], "true", StringComparison.OrdinalIgnoreCase);

            if (!enabled)
                return Forbid("Endpoint de prueba deshabilitado en este entorno.");

            if (string.IsNullOrWhiteSpace(dto.To))
                return BadRequest(new { message = "Debe indicar 'to'." });

            var subject = dto.Subject ?? "Prueba SMTP - TuBarbería";
            var body = dto.Body ?? @"<h3>Correo de prueba</h3><p>Si llegó, el SMTP está OK.</p>";

            await _emailService.SendEmailAsync(dto.To, subject, body, dto.IsHtml);
            return Ok(new { message = "Correo enviado (si no llega, revisa los logs del App Service)." });
        }
    }
}
