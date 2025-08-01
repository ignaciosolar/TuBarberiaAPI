namespace TuBarberiaAPI.Models
{
    
        public class Service
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;

            public ICollection<BarberService> BarberServices { get; set; } = new List<BarberService>();
        }

    }

