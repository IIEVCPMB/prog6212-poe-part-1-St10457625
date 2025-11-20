namespace PROG_POE_Part_1.Models
{
    public class ReportViewModel
    {
        public int UserID { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalPayment { get; set; }
    }
}
