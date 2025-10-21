using Microsoft.AspNetCore.Mvc;
using PROG_POE_Part_1.Data;
using PROG_POE_Part_1.Models;
using PROG_POE_Part_1.Services;

namespace PROG_POE_Part_1.Controllers
{
    public class ManagersController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly FileEncryptionService _encryptionService;

        public ManagersController(IWebHostEnvironment environment, FileEncryptionService encryptionService)
        {
            _environment = environment;
            _encryptionService = encryptionService;
        }

        public IActionResult Index(string filter = "all")
        {
            try
            {
                var claims = ClaimData.GetAllClaims();
                ViewBag.Filter = filter;

                claims = filter.ToLower() switch
                {
                    "pending" => ClaimData.GetClaimsByStatus(Status.Pending),
                    "verified" => ClaimData.GetClaimsByStatus(Status.Verified),
                    "approved" => ClaimData.GetClaimsByStatus(Status.Approved),
                    "declined" => ClaimData.GetClaimsByStatus(Status.Declined),
                    _ => claims
                };

                ViewBag.PendingCount = ClaimData.GetPendingCount();
                ViewBag.VerifiedCount = ClaimData.GetVerifiedCount();
                ViewBag.ApprovedCount = ClaimData.GetApprovedCount();
                ViewBag.DeclinedCount = ClaimData.GetDeclinedCount();

                return View(claims);
            }
            catch
            {
                ViewBag.Error = "Unable to load claims.";
                return View(new List<Claim>());
            }
        }

        public IActionResult Review(int id)
        {
            var claim = ClaimData.GetClaimByID(id);
            if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
                return RedirectToAction("Index");
            }

            ViewBag.Reviews = claim.Reviews ?? new List<ClaimReview>();
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

            // Only allow approval if verified
            if (claim.Status != Status.Verified)
            {
                TempData["Error"] = "Claim cannot be approved unless it has been verified by the Programme Coordinator.";
                return RedirectToAction("Index");
            }

            bool updated = ClaimData.UpdateClaimStatus(id, Status.Approved, "Manager", comments ?? "Approved successfully.");

            TempData[updated ? "Success" : "Error"] = updated
                ? $"Claim #{id} approved successfully."
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

            // Only allow decline if verified
            if (claim.Status != Status.Verified)
            {
                TempData["Error"] = "Claim cannot be declined unless it has been verified by the Programme Coordinator.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(comments))
            {
                TempData["Error"] = "Decline reason is required.";
                return RedirectToAction("Review", new { id });
            }

            bool updated = ClaimData.UpdateClaimStatus(id, Status.Declined, "Manager", comments);

            TempData[updated ? "Success" : "Error"] = updated
                ? $"Claim #{id} declined successfully."
                : "Failed to update claim status.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int claimId, string filePath, string fileName)
        {
            var claim = ClaimData.GetClaimByID(claimId);
            if (claim == null) return NotFound("Claim not found.");

            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
                if (!System.IO.File.Exists(fullPath))
                    return NotFound("File not found.");

                var decryptedStream = await _encryptionService.DecryptFileAsync(fullPath);
                decryptedStream.Position = 0;

                return File(decryptedStream, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error downloading file: {ex.Message}");
            }
        }
    }
}
