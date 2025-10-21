using Microsoft.AspNetCore.Mvc;
using PROG_POE_Part_1.Models;
using PROG_POE_Part_1.Data;
using PROG_POE_Part_1.Services;

namespace PROG_POE_Part_1.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly FileEncryptionService _encryptionService;

        public ClaimsController(IWebHostEnvironment environment, FileEncryptionService encryptionService)
        {
            _environment = environment;
            _encryptionService = encryptionService;
        }

        public IActionResult Index()
        {
            try
            {
                var claims = ClaimData.GetAllClaims();
                return View(claims);
            }
            catch
            {
                ViewBag.Error = "There are no claims found.";
                return View(new List<Claim>());
            }
        }

        public IActionResult Details(int id)
        {
            try
            {
                var claim = ClaimData.GetClaimByID(id);
                if (claim == null)
                {
                    return NotFound();
                }

                return View(claim);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error retrieving claim: {ex.Message}";
                return RedirectToAction("Index");
            }
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Claim claim, List<IFormFile> documents)
        {
            try
            {
                if (string.IsNullOrEmpty(claim.Name))
                {
                    ViewBag.Error = "Lecturer name is required.";
                    return View(claim);
                }

                if (claim.Total_Hours == 0)
                {
                    ViewBag.Error = "Please input hours worked.";
                    return View(claim);
                }

                if (claim.Hourly_Rate == 0)
                {
                    ViewBag.Error = "Please enter an hourly rate.";
                    return View(claim);
                }

                // --- File Uploads ---
                if (documents != null && documents.Count > 0)
                {
                    if (claim.Documents == null)
                    {
                        claim.Documents = new List<UploadedDocument>();
                    }

                    foreach (var file in documents)
                    {
                        if (file.Length > 0)
                        {
                            var allowedExtensions = new[] { ".pdf", ".docx", ".txt", ".xlsx" };
                            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                            if (!allowedExtensions.Contains(extension))
                            {
                                ViewBag.Error = $"File extension {extension} is not allowed.";
                                return View(claim);
                            }

                            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                            Directory.CreateDirectory(uploadsFolder);

                            var uniqueFileName = Guid.NewGuid().ToString() + ".encrypted";
                            var encryptedFilePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = file.OpenReadStream())
                            {
                                await _encryptionService.EncryptFileAsync(fileStream, encryptedFilePath);
                            }

                            claim.Documents.Add(new UploadedDocument
                            {
                                FileName = file.FileName,
                                FilePath = "/uploads/" + uniqueFileName,
                                FileSize = file.Length,
                                IsEncrypted = true
                            });
                        }
                    }
                }

                ClaimData.AddClaim(claim);
                ViewBag.Success = "Claim submitted successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"An error occurred: {ex.Message}";
                return View(claim);
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(string filePath, string fileName)
        {
            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));

                if (!System.IO.File.Exists(fullPath))
                    return NotFound("File not found.");

                // Decrypt file before sending
                var decryptedStream = await _encryptionService.DecryptFileAsync(fullPath);

                decryptedStream.Position = 0;

                return File(decryptedStream, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error downloading file: {ex.Message}");
            }
        }

        public IActionResult ManagerDashboard(string filter = "all")
        {
            try
            {
                var claims = ClaimData.GetAllClaims();

                claims = filter.ToLower() switch
                {
                    "pending" => ClaimData.GetClaimsByStatus(Status.Pending),
                    "approved" => ClaimData.GetClaimsByStatus(Status.Approved),
                    "declined" => ClaimData.GetClaimsByStatus(Status.Declined),
                    _ => claims
                };

                ViewBag.Filter = filter;
                ViewBag.Total = ClaimData.GetAllClaims().Count;
                ViewBag.Pending = ClaimData.GetPendingCount();
                ViewBag.Approved = ClaimData.GetApprovedCount();
                ViewBag.Declined = ClaimData.GetDeclinedCount();

                return View("ManagerDashboard", claims);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading manager dashboard: " + ex.Message;
                return View("ManagerDashboard", new List<Claim>());
            }
        }


    }
}
