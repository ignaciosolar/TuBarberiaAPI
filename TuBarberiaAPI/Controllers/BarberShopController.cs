using BCrypt.Net;
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
    public class BarberShopController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BarberShopController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> RegisterBarberShop([FromBody] RegisterBarberShopDto dto)
        {
            // Verificar si ya existe un usuario con ese correo
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Admin.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "El correo ya está en uso." });
            }

            // Crear nuevo usuario administrador
            var adminUser = new User
            {
                FullName = dto.Admin.FullName,
                Email = dto.Admin.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Admin.Password),
                Role = dto.Admin.Role,
                Phone = dto.Admin.Phone
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync(); // Se guarda para obtener el Id del usuario

            // Crear barbería asociada al usuario
            var barberShop = new BarberShop
            {
                Name = dto.BarberShop.Name,
                Street = dto.BarberShop.Street,
                Number = dto.BarberShop.Number,
                Region = dto.BarberShop.Region,
                Commune = dto.BarberShop.Commune,
                AdminUserId = adminUser.Id
            };

            _context.BarberShops.Add(barberShop);
            await _context.SaveChangesAsync(); // Se guarda para obtener el Id de la barbería

            // Actualizar el usuario con el Id de la barbería
            adminUser.BarberShopId = barberShop.Id;
            _context.Users.Update(adminUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Barbería registrada con éxito." });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var barberShops = await _context.BarberShops
                .Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.Street,
                    b.Number,
                    b.Region,
                    b.Commune
                })
                .ToListAsync();

            return Ok(barberShops);
        }

    }
}
