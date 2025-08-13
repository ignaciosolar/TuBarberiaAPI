namespace TuBarberiaAPI.DTOs
{
    public class TestEmailDto
    {
        public string To { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public bool IsHtml { get; set; } = true;
    }
}
