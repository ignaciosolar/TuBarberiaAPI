namespace TuBarberiaAPI.DTOs
{
    public class BarberServiceCreateDto
    {
        public int ServiceId { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
