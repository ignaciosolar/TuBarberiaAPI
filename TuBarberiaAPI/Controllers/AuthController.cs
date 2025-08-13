using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuBarberiaAPI.Data;
using TuBarberiaAPI.DTOs;
using TuBarberiaAPI.Models;
using TuBarberiaAPI.Services;

namespace TuBarberiaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowAngularApp")] // 👈 aplica la policy al controller completo
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        // 👇 Maneja preflight explícito
        [HttpOptions("login")]
        public IActionResult OptionsLogin() => NoContent();

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user == null) return Unauthorized(new { message = "Usuario no encontrado" });

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Contraseña incorrecta" });

            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.Phone,
                    user.Role,
                    user.BarberShopId
                }
            });
        }

        // (opcional) también para register:
        [HttpOptions("register")]
        public IActionResult OptionsRegister() => NoContent();

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLower();
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (existingUser != null) return BadRequest(new { message = "El correo ya está registrado." });

            var barberShop = await _context.BarberShops.FindAsync(dto.BarberShopId);
            if (barberShop == null) return BadRequest(new { message = "La barbería no existe." });

            var user = new User
            {
                FullName = dto.FullName,
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                BarberShopId = dto.BarberShopId,
                Phone = dto.Phone
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Usuario registrado exitosamente.",
                user = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.Phone,
                    user.Role,
                    user.BarberShopId
                }
            });
        }
    }
}
