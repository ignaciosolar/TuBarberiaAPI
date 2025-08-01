namespace TuBarberiaAPI.DTOs
{
    public class ReservationCreateDto
    {
        public int BarberId { get; set; }
        public int BarberServiceId { get; set; }

        public string ClientName { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
    }
}
