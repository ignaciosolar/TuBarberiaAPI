namespace TuBarberiaAPI.Models
{
    public class BarberSchedule
    {
        public int Id { get; set; }

        public int BarberId { get; set; }
        public User Barber { get; set; } = null!;

        public DayOfWeek DayOfWeek { get; set; } // Enum: Monday = 1, Sunday = 0

        public TimeSpan StartTime { get; set; } // Ej: 09:00
        public TimeSpan EndTime { get; set; }   // Ej: 20:00
    }
}
