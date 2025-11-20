using PROG_POE_Part_1.Models;
using System.ComponentModel.DataAnnotations;

namespace PROG_POE_Part_1.Models
{
    public class Claim
    {
        [Key]
        public int Claim_ID { get; set; }
        public int Lecturer_ID { get; set; }
        public string Name { get; set; }
        public decimal Total_Hours { get; set; }
        public decimal Hourly_Rate { get; set; }
        public DateTime Date_Submitted { get; set; }
        public decimal Total_Payment { get; set; }
        public Status Status { get; set; }
        public int? ReviewedBy { get; set; }
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
