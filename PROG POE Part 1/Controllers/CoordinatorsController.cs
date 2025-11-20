using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG_POE_Part_1.Models;
using PROG_POE_Part_1.Services;
using PROG_POE_Part_1.Data;
using System;

namespace PROG6212_POE_Part_Final.Controllers
{
    public class CoordinatorsController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly FileEncryptionService _encryptionService;
        private readonly ClaimService _claimService;
        private readonly AppDbContext _context;

        public CoordinatorsController(IWebHostEnvironment environment, FileEncryptionService encryptionService, ClaimService claimService, AppDbContext context)
        {
            _environment = environment;
            _encryptionService = encryptionService;
            _claimService = claimService;
            _context = context;
        }

        private bool IsCoordinator()
            => HttpContext.Session.GetString("UserRole") == "Coordinator";

        private IActionResult BlockNonCoordinator()
        {
            TempData["Error"] = "Login to access Coordinator pages.";
            return RedirectToAction("Login", "Account");
        }

        public async Task<IActionResult> Index(string filter = "all")
        {
            if (!IsCoordinator()) return BlockNonCoordinator();

            var allClaims = await _claimService.GetAllClaimsAsync();

            var claims = filter.ToLower() switch
            {
                "pending" => allClaims.Where(c => c.Status == Status.Pending).ToList(),
                "verified" => allClaims.Where(c => c.Status == Status.Verified).ToList(),
                "declined" => allClaims.Where(c => c.Status == Status.Declined).ToList(),
                _ => allClaims
            };

            ViewBag.Filter = filter;
            ViewBag.PendingCount = allClaims.Count(c => c.Status == Status.Pending);
            ViewBag.VerifiedCount = allClaims.Count(c => c.Status == Status.Verified);
            ViewBag.DeclinedCount = allClaims.Count(c => c.Status == Status.Declined);

            return View(claims);
        }

        public async Task<IActionResult> Verify(int id)
        {
            var claim = await _context.Claims
                .Include(c => c.Documents)
                .Include(c => c.Reviews)
                .FirstOrDefaultAsync(c => c.Claim_ID == id);

            if (claim == null)
                return NotFound();

            return View(claim);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? comments)
        {
            if (!IsCoordinator()) return BlockNonCoordinator();

            var claim = await _claimService.GetClaimByIDAsync(id);
            if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
                return RedirectToAction("Index");
            }

            if (!WorkflowService.CanVerify(claim))
            {
                TempData["Error"] = "This claim cannot be verified according to workflow rules.";
                return RedirectToAction("Index");
            }

            var coordinatorId = HttpContext.Session.GetInt32("UserID");
            if (!coordinatorId.HasValue)
            {
                TempData["Error"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            claim.Status = Status.Verified;
            claim.ReviewedBy = coordinatorId.Value; // Store numeric UserID
            claim.ReviewedDate = DateTime.Now;

            bool updated = await _claimService.UpdateClaimAsync(claim);

            TempData[updated ? "Success" : "Error"] = updated
                ? $"Claim #{id} verified successfully and sent to Manager for review."
                : "Failed to update claim status.";

            var userName = HttpContext.Session.GetString("FullName");
            var userRole = HttpContext.Session.GetString("UserRole");

            var review = new ClaimReview
            {
                ClaimID = claim.Claim_ID,
                ReviewerName = userName,
                ReviewerRole = userRole,
                Decision = Status.Verified,
                Comments = "Claim verified and forwarded to manager.",
                ReviewDate = DateTime.Now
            };

            _context.ClaimReviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(int id, string comments)
        {
            if (!IsCoordinator()) return BlockNonCoordinator();

            var claim = await _claimService.GetClaimByIDAsync(id);
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

            var coordinatorId = HttpContext.Session.GetInt32("UserID");
            if (!coordinatorId.HasValue)
            {
                TempData["Error"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            claim.Status = Status.Declined;
            claim.ReviewedBy = coordinatorId.Value; // Store numeric UserID
            claim.ReviewedDate = DateTime.Now;

            bool updated = await _claimService.UpdateClaimAsync(claim);

            TempData[updated ? "Success" : "Error"] = updated
                ? $"Claim #{id} declined. Lecturer will be notified."
                : "Failed to update claim status.";

            var userName = HttpContext.Session.GetString("FullName");
            var userRole = HttpContext.Session.GetString("UserRole");

            var review = new ClaimReview
            {
                ClaimID = claim.Claim_ID,
                ReviewerName = userName,
                ReviewerRole = userRole,
                Decision = Status.Declined,
                Comments = comments,
                ReviewDate = DateTime.Now
            };

            _context.ClaimReviews.Add(review);
            await _context.SaveChangesAsync();


            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int claimId, string filePath, string fileName)
        {
            if (!IsCoordinator()) return BlockNonCoordinator();

            var claim = await _claimService.GetClaimByIDAsync(claimId);
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
