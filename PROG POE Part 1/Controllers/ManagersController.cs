using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG_POE_Part_1.Models;
using PROG_POE_Part_1.Services;
using PROG_POE_Part_1.Data;
using System;

namespace PROG6212_POE_Part_Final.Controllers
{
    public class ManagersController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly FileEncryptionService _encryptionService;
        private readonly ClaimService _claimService;
        private readonly AppDbContext _context;


        public ManagersController(IWebHostEnvironment environment, FileEncryptionService encryptionService, ClaimService claimService, AppDbContext context)
        {
            _environment = environment;
            _encryptionService = encryptionService;
            _claimService = claimService;
            _context = context;
        }

        private bool IsManager() => HttpContext.Session.GetString("UserRole") == "Manager";

        private IActionResult BlockNonManager()
        {
            TempData["Error"] = "Login to access Manager pages.";
            return RedirectToAction("Login", "Account");
        }

        public async Task<IActionResult> Index(string filter = "all")
        {
            if (!IsManager()) return BlockNonManager();

            try
            {
                var allClaims = await _claimService.GetAllClaimsAsync();

                var claims = filter.ToLower() switch
                {
                    "pending" => allClaims.Where(c => c.Status == Status.Pending).ToList(),
                    "verified" => allClaims.Where(c => c.Status == Status.Verified).ToList(),
                    "approved" => allClaims.Where(c => c.Status == Status.Approved).ToList(),
                    "declined" => allClaims.Where(c => c.Status == Status.Declined).ToList(),
                    _ => allClaims
                };

                ViewBag.Filter = filter;
                ViewBag.PendingCount = allClaims.Count(c => c.Status == Status.Pending);
                ViewBag.VerifiedCount = allClaims.Count(c => c.Status == Status.Verified);
                ViewBag.ApprovedCount = allClaims.Count(c => c.Status == Status.Approved);
                ViewBag.DeclinedCount = allClaims.Count(c => c.Status == Status.Declined);

                return View(claims);
            }
            catch
            {
                ViewBag.Error = "Unable to load claims.";
                return View(new List<Claim>());
            }
        }

        // Approve/Revivew
        public async Task<IActionResult> Review(int id)
        {
            var claim = await _context.Claims
                .Include(c => c.Documents)
                .Include(c => c.Reviews)
                .FirstOrDefaultAsync(c => c.Claim_ID == id);

            if (claim == null)
                return NotFound();

            ViewBag.Reviews = claim.Reviews;

            return View(claim);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? comments)
        {
            if (!IsManager()) return BlockNonManager();

            var claim = await _claimService.GetClaimByIDAsync(id);
            if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
                return RedirectToAction("Index");
            }

            if (!WorkflowService.CanApprove(claim))
            {
                TempData["Error"] = "Manager cannot approve a claim unless it has been verified.";
                return RedirectToAction("Index");
            }

            var managerId = HttpContext.Session.GetInt32("UserID");
            if (managerId == null)
            {
                TempData["Error"] = "Manager not logged in.";
                return RedirectToAction("Login", "Account");
            }

            claim.Status = Status.Approved;
            claim.ReviewedBy = managerId.Value; // store int UserID
            claim.ReviewedDate = DateTime.Now;

            var updated = await _claimService.UpdateClaimAsync(claim);

            TempData[updated ? "Success" : "Error"] = updated
                ? $"Claim #{id} approved successfully."
                : "Failed to update claim status.";

            var userName = HttpContext.Session.GetString("FullName");
            var userRole = HttpContext.Session.GetString("UserRole");

            var review = new ClaimReview
            {
                ClaimID = claim.Claim_ID,
                ReviewerName = userName,
                ReviewerRole = userRole,
                Decision = Status.Approved,
                Comments = "Claim approved.",
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
            if (!IsManager()) return BlockNonManager();

            var claim = await _claimService.GetClaimByIDAsync(id);
            if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
                return RedirectToAction("Index");
            }

            if (!WorkflowService.CanDecline(claim))
            {
                TempData["Error"] = "Claim decline is not allowed at this stage.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(comments))
            {
                TempData["Error"] = "Decline reason is required.";
                return RedirectToAction("Review", new { id });
            }

            var managerId = HttpContext.Session.GetInt32("UserID");
            if (managerId == null)
            {
                TempData["Error"] = "Manager not logged in.";
                return RedirectToAction("Login", "Account");
            }

            claim.Status = Status.Declined;
            claim.ReviewedBy = managerId.Value; // store int UserID
            claim.ReviewedDate = DateTime.Now;

            var updated = await _claimService.UpdateClaimAsync(claim);

            TempData[updated ? "Success" : "Error"] = updated
                ? $"Claim #{id} declined successfully."
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
            if (!IsManager()) return BlockNonManager();

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
