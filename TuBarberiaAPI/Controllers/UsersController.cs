using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuBarberiaAPI.Data;
using TuBarberiaAPI.DTOs;

namespace TuBarberiaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("barbers")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<BarberSimpleDto>))]
        public async Task<IActionResult> GetBarbersByLoggedUserBarbershop()
        {
            // 👇 Diagnóstico temporal
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"[CLAIM DEBUG] {claim.Type} => {claim.Value}");
            }

            var userIdClaim = User.FindFirst("id")?.Value;
            var role = User.FindFirst("role")?.Value
                       ?? User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;

            if (userIdClaim == null || role == null)
                return Unauthorized();

            var userId = int.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.BarberShopId == null)
                return BadRequest(new { message = "No está asociado a ninguna barbería." });

            if (role == "Administrador")
            {
                var usuarios = await _context.Users
                    .Where(u => u.BarberShopId == user.BarberShopId)
                    .Select(u => new BarberSimpleDto
                    {
                        Id = u.Id,
                        FullName = u.FullName
                    })
                    .ToListAsync();

                return Ok(usuarios);
            }
            else
            {
                var current = await _context.Users
                    .Where(u => u.Id == userId && u.Role == "Barbero")
                    .Select(u => new BarberSimpleDto
                    {
                        Id = u.Id,
                        FullName = u.FullName
                    })
                    .FirstOrDefaultAsync();

                if (current == null)
                    return Forbid();

                return Ok(new[] { current });
            }
        }

        [AllowAnonymous]
        [HttpGet("barbers/by-barbershop/{barberShopId}")]
        public async Task<IActionResult> GetBarbersByBarberShop(int barberShopId)
        {
            var barberos = await _context.Users
                .Where(u => u.BarberShopId == barberShopId)
                .Select(u => new
                {
                    u.Id,
                    u.FullName
                })
                .ToListAsync();

            return Ok(barberos);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateCurrentUser([FromBody] UserUpdateDto dto)
        {
            var userIdClaim = User.FindFirst("id")?.Value;
            if (userIdClaim == null)
                return Unauthorized();
        
            var userId = int.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();
        
            user.FullName = dto.FullName ?? user.FullName;
            user.Email = dto.Email ?? user.Email;
            user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;
        
            await _context.SaveChangesAsync();
            return Ok(new { message = "Perfil actualizado correctamente." });
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst("id")?.Value;
            if (userIdClaim == null)
                return Unauthorized();
        
            var userId = int.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();
        
            return Ok(new {
                fullName = user.FullName,
                email = user.Email,
                phoneNumber = user.PhoneNumber
            });
        }

    }
}
