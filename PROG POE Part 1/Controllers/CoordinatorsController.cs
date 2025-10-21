using Microsoft.AspNetCore.Mvc;
using PROG_POE_Part_1.Data;
using PROG_POE_Part_1.Models;

namespace PROG_POE_Part_1.Controllers
{
    public class CoordinatorsController : Controller
    {
        public IActionResult Index(string filter = "all")
        {
            var claims = ClaimData.GetAllClaims();
            ViewBag.Filter = filter;

            claims = filter.ToLower() switch
            {
                "pending" => ClaimData.GetClaimsByStatus(Status.Pending),
                "verified" => ClaimData.GetClaimsByStatus(Status.Verified),
                "declined" => ClaimData.GetClaimsByStatus(Status.Declined),
                _ => claims
            };

            ViewBag.PendingCount = ClaimData.GetPendingCount();
            ViewBag.VerifiedCount = ClaimData.GetVerifiedCount();
            ViewBag.DeclinedCount = ClaimData.GetDeclinedCount();

            return View(claims);
        }

        public IActionResult Verify(int id)
        {
            var claim = ClaimData.GetClaimByID(id);
            if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
                return RedirectToAction("Index");
            }

            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int id, string? comments)
        {
            var claim = ClaimData.GetClaimByID(id);
            if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
                return RedirectToAction("Index");
            }

            if (claim.Status != Status.Pending)
            {
                TempData["Error"] = "Only pending claims can be verified.";
                return RedirectToAction("Index");
            }

            bool updated = ClaimData.UpdateClaimStatus(id, Status.Verified, "Coordinator", comments ?? "Verified successfully.");

            TempData[updated ? "Success" : "Error"] = updated
                ? $"Claim #{id} verified successfully and sent to Manager for review."
                : "Failed to update claim status.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Decline(int id, string comments)
        {
            var claim = ClaimData.GetClaimByID(id);
            if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
                return RedirectToAction("Index");
            }

            if (claim.Status != Status.Pending)
            {
                TempData["Error"] = "Only pending claims can be declined.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(comments))
            {
                TempData["Error"] = "Decline reason is required.";
                return RedirectToAction("Verify", new { id });
            }

            bool updated = ClaimData.UpdateClaimStatus(id, Status.Declined, "Coordinator", comments);

            TempData[updated ? "Success" : "Error"] = updated
                ? $"Claim #{id} declined. Lecturer will be notified."
                : "Failed to update claim status.";

            return RedirectToAction("Index");
        }
    }
}
