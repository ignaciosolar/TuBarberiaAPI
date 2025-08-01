using TuBarberiaAPI.Models;

public class BarberBlockedTime
{
    public int Id { get; set; }

    public int BarberId { get; set; }
    public User Barber { get; set; } = null!;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public string Reason { get; set; } = string.Empty; // Ej: "Almuerzo", "Inactividad", "Vacaciones"
}
