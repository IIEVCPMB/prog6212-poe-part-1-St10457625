using PROG_POE_Part_1.Models;

namespace PROG_POE_Part_1.Services
{
    public static class WorkflowService
    {
        public static bool CanVerify(Claim claim)
        {
            return claim.Status == Status.Pending;
        }

        public static bool CanApprove(Claim claim)
        {
            return claim.Status == Status.Verified;
        }

        public static bool CanDecline(Claim claim)
        {
            return claim.Status == Status.Verified || claim.Status == Status.Pending;
        }
    }
}
