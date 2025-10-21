namespace PROG_POE_Part_1.Models
{
    public class ClaimReview
    {
        public int ID { get; set; }
        public int ClaimID { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewerRole { get; set; } = string.Empty;
        public DateTime ReviewDate { get; set; }
        public Status Decision { get; set; }
        public string Comments { get; set; } = string.Empty;

        //public string ReviewType { get; set; } = string.Empty; // "Initial", "Re-review"

    }
}
