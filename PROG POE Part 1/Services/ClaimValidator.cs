using PROG_POE_Part_1.Models;

namespace PROG_POE_Part_1.Services
{
    public static class ClaimValidator
    {
        private const int MAX_HOURS = 200;
        private const int MIN_RATE = 100;
        private const int MAX_RATE = 500;

        public static List<string> Validate(Claim claim)
        {
            var errors = new List<string>();

            if (claim.Total_Hours <= 0)
                errors.Add("Total hours cannot be zero or negative.");

            if (claim.Total_Hours > MAX_HOURS)
                errors.Add($"Total hours cannot exceed {MAX_HOURS} per month.");

            if (claim.Hourly_Rate < MIN_RATE || claim.Hourly_Rate > MAX_RATE)
                errors.Add($"Hourly rate must be between {MIN_RATE} and {MAX_RATE}.");

            if (claim.Total_Payment != claim.Total_Hours * claim.Hourly_Rate)
                errors.Add("Total payment calculation mismatch.");

            if (claim.Documents == null || claim.Documents.Count == 0)
                errors.Add("At least one supporting document is required.");

            return errors;
        }
    }
}
