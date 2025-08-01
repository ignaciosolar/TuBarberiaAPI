namespace TuBarberiaAPI.DTOs
{
    public class RegisterUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Barbero";
        public int BarberShopId { get; set; }
        public string Phone { get; set; } = string.Empty;
    }
}
