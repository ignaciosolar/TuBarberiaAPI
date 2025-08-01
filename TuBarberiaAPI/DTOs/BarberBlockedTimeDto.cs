namespace TuBarberiaAPI.DTOs
{
    public class BarberBlockedTimeDto
    {
        public int BarberId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Reason { get; set; } = "Bloqueo manual";
    }
}
