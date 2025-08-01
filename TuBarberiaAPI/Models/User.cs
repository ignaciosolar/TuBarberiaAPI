namespace TuBarberiaAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Administrador";
        public int? BarberShopId { get; set; }  // Ahora es nullable
        public BarberShop? BarberShop { get; set; }
        public ICollection<BarberShop>? BarberShops { get; set; }
        public ICollection<BarberService> BarberServices { get; set; } = new List<BarberService>();
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<BarberBlockedTime> BlockedTimes { get; set; } = new List<BarberBlockedTime>();
        public ICollection<BarberSchedule> Schedules { get; set; } = new List<BarberSchedule>();



    }
}
