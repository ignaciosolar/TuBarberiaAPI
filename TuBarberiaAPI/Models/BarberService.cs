namespace TuBarberiaAPI.Models
{
    public class BarberService
    {
        public int Id { get; set; }

        public int UserId { get; set; } // barbero
        public User User { get; set; } = null!;

        public int ServiceId { get; set; } // tipo de servicio
        public Service Service { get; set; } = null!;

        public decimal Price { get; set; }
        public int DurationMinutes { get; set; } = 30;
        public bool IsActive { get; set; } = true;
    }

}
