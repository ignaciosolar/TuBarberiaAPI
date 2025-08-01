namespace TuBarberiaAPI.DTOs
{
    public class RegisterBarberShopDto
    {
        public BarberShopDto BarberShop { get; set; }
        public AdminDto Admin { get; set; }
    }

    public class BarberShopDto
    {
        public string Name { get; set; }
        public string Street { get; set; }
        public string Number { get; set; }
        public string Region { get; set; }
        public string Commune { get; set; }
    }

    public class AdminDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; } // ✅ AGREGA ESTA LÍNEA
        public string Role { get; set; }
    }
}
