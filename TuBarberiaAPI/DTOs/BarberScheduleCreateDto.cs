namespace TuBarberiaAPI.DTOs
{
    public class BarberScheduleCreateDto
    {
        public int BarberId { get; set; }
        public Dictionary<DayOfWeek, List<TimeBlockDto>> Schedules { get; set; } = new();
    }

    public class TimeBlockDto
    {
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
    }
}
