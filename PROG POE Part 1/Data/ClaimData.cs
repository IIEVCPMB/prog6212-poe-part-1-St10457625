using PROG_POE_Part_1.Models;
using System.Text.Json;

namespace PROG_POE_Part_1.Data
{
    public class ClaimData
    {
        private static readonly string FilePath = "Data/claims.json";

        private static List<Claim> _claims = LoadClaims();

        private static int _nextID = _claims.Any() ? _claims.Max(c => c.Claim_ID) + 1 : 1;
        private static int _nextReviewID = _claims.SelectMany(c => c.Reviews ?? new List<ClaimReview>())
                                                  .DefaultIfEmpty()
                                                  .Max(r => r?.ID ?? 0) + 1;

        // Load claims from file
        private static List<Claim> LoadClaims()
        {
            if (!File.Exists(FilePath))
            {
                // If no file exists, then create default data
                var defaultClaims = new List<Claim>
                {
                    new Claim
                    {
                        Claim_ID = 1,
                        Lecturer_ID = 12234,
                        Name = "Robert Martin",
                        Date_Submitted = DateTime.Now.AddDays(-5),
                        Total_Hours = 10,
                        Hourly_Rate = 200,
                        Status = Status.Pending,
                        Documents = new List<UploadedDocument>(),
                        Reviews = new List<ClaimReview>()
                    },
                    new Claim
                    {
                        Claim_ID = 2,
                        Lecturer_ID = 12832,
                        Name = "Nick Smith",
                        Date_Submitted = DateTime.Now.AddDays(-2),
                        Total_Hours = 12,
                        Hourly_Rate = 250,
                        Status = Status.Approved,
                        Documents = new List<UploadedDocument>(),
                        Reviews = new List<ClaimReview>()
                    },
                    new Claim
                    {
                        Claim_ID = 3,
                        Lecturer_ID = 12733,
                        Name = "John Wick",
                        Date_Submitted = DateTime.Now.AddDays(-7),
                        Total_Hours = 15,
                        Hourly_Rate = 300,
                        Status = Status.Declined,
                        Documents = new List<UploadedDocument>(),
                        Reviews = new List<ClaimReview>()
                    }
                };

                SaveClaims(defaultClaims);
                return defaultClaims;
            }

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<Claim>>(json) ?? new List<Claim>();
        }

        //Save claims to a file
        private static void SaveClaims(List<Claim>? claims = null)
        {
            var json = JsonSerializer.Serialize(claims ?? _claims, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }

        public static List<Claim> GetAllClaims() => _claims.ToList();

        public static Claim? GetClaimByID(int id) => _claims.FirstOrDefault(b => b.Claim_ID == id);

        public static List<Claim> GetClaimsByStatus(Status status)
            => _claims.Where(b => b.Status == status).ToList();

        public static void AddClaim(Claim claim)
        {
            claim.Claim_ID = _nextID++;
            claim.Date_Submitted = DateTime.Now;
            claim.Status = Status.Pending;
            claim.Reviews = new List<ClaimReview>();
            _claims.Add(claim);
            SaveClaims();
        }

        public static int GetPendingCount() => _claims.Count(b => b.Status == Status.Pending);
        public static int GetApprovedCount() => _claims.Count(b => b.Status == Status.Approved);
        public static int GetDeclinedCount() => _claims.Count(b => b.Status == Status.Declined);
        public static int GetVerifiedCount() => _claims.Count(b => b.Status == Status.Verified);

        public static bool UpdateClaimStatus(int id, Status newStatus, string reviewedBy, string comments)
        {
            var claim = GetClaimByID(id);
            if (claim == null) return false;

            // CREATE REVIEW RECORD
            var review = new ClaimReview
            {
                ID = _nextReviewID++,
                ClaimID = id,
                ReviewerName = reviewedBy,
                ReviewerRole = "Coordinator",
                ReviewDate = DateTime.Now,
                Decision = newStatus,
                Comments = comments
            };

            claim.Reviews ??= new List<ClaimReview>();
            claim.Reviews.Add(review);

            // UPDATE CLAIM STATUS
            claim.Status = newStatus;
            claim.ReviewedBy = reviewedBy;
            claim.ReviewedDate = DateTime.Now;

            SaveClaims();

            return true;
        }
    }
}
