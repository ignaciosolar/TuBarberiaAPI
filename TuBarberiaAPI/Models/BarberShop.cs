namespace TuBarberiaAPI.Models
{
    public class BarberShop
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Commune { get; set; } = string.Empty;

        public int AdminUserId { get; set; }
        public User AdminUser { get; set; } = null!;
        public ICollection<User> Users { get; set; } = new List<User>();

    }
}
