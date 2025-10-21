namespace PROG_POE_Part_1.Models
{
    public class Claim
    {
        public int Claim_ID {  get; set; }
        public int Lecturer_ID { get; set; }
        public string Name { get; set; }
        public decimal Total_Hours { get; set; }
        public int Hourly_Rate { get; set; }
        public DateTime Date_Submitted { get; set; }
        public decimal Total_Amount => Total_Hours * Hourly_Rate;
        public Status Status { get; set; }
        public string ReviewedBy { get; set; }
        public DateTime ReviewedDate { get; set; }

        public List<UploadedDocument> Documents { get; set; }

        public List<ClaimReview> Reviews { get; set; } = new List<ClaimReview>();


    }
    public enum Status
    {
        Pending,
        Verified,
        Approved,
        Declined
    }

}
