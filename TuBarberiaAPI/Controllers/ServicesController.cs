using Microsoft.AspNetCore.Mvc;
using TuBarberiaAPI.Data;
using Microsoft.EntityFrameworkCore;
using TuBarberiaAPI.DTOs;

namespace TuBarberiaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ServicesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ServiceDto>>> GetAll()
        {
            var services = await _context.Services
                .Select(s => new ServiceDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description
                })
                .ToListAsync();

            return Ok(services);
        }
    }
}
