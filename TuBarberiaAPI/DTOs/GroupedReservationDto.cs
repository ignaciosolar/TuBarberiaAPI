namespace TuBarberiaAPI.DTOs
{
    public class GroupedReservationDto
    {
        public int BarberId { get; set; }
        public string BarberName { get; set; } = string.Empty;
        public List<ReservationDto> Reservations { get; set; } = new();
    }
}
