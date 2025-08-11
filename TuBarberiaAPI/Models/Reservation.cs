namespace TuBarberiaAPI.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        public int BarberId { get; set; }
        public User Barber { get; set; } = null!;

        public int? BarberServiceId { get; set; }
        public BarberService? BarberService { get; set; }

        public string ClientName { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty; // <-- Nuevo campo

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public string Status { get; set; } = "Activa"; // Activa, Cancelada, Finalizada
    }
}
