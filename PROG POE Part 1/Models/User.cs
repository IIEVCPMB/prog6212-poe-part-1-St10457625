namespace PROG_POE_Part_1.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }

        public string Password { get; set; } 

        public decimal HourlyRate { get; set; }

        public string Role { get; set; }
    }
}
