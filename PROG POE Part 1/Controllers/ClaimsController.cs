using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PROG_POE_Part_1.Models;
using PROG_POE_Part_1.Services;


namespace PROG6212_POE_Part_Final.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly FileEncryptionService _encryptionService;
        private readonly ClaimService _claimService;
        private readonly UserService _userService;

        public ClaimsController(IWebHostEnvironment environment,
                                FileEncryptionService encryptionService,
                                ClaimService claimService,
                                UserService userService)
        {
            _environment = environment;
            _encryptionService = encryptionService;
            _claimService = claimService;
            _userService = userService;
        }

        private bool IsLecturer()
        {
            return HttpContext.Session.GetString("UserRole") == "Lecturer";
        }

        private IActionResult BlockNonLecturer()
        {
            TempData["Error"] = "Login to access Lecturer pages.";
            return RedirectToAction("Login", "Account");
        }

        private async Task<User?> GetLoggedInLecturerAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return null;

            var user = await _userService.GetUserByIDAsync(userId.Value);
            return (user != null && user.Role == "Lecturer") ? user : null;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsLecturer()) return BlockNonLecturer();

            var lecturer = await GetLoggedInLecturerAsync();
            if (lecturer == null) return BlockNonLecturer();

            var claims = await _claimService.GetClaimsByLecturerAsync(lecturer.UserID);

            return View(claims);
        }

        public async Task<IActionResult> Details(int id)
        {
            if (!IsLecturer()) return BlockNonLecturer();

            var claim = await _claimService.GetClaimByIDAsync(id);
            if (claim == null) return NotFound();

            return View(claim);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!IsLecturer()) return BlockNonLecturer();

            var lecturer = await GetLoggedInLecturerAsync();
            if (lecturer == null) return BlockNonLecturer();

            var claim = new Claim
            {
                Name = $"{lecturer.Name} {lecturer.Surname}",
                Lecturer_ID = lecturer.UserID,
                Hourly_Rate = lecturer.HourlyRate,
                Total_Hours = 0
            };

            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Claim claim, List<IFormFile> documents)
        {
            if (!IsLecturer()) return BlockNonLecturer();

            var lecturer = await GetLoggedInLecturerAsync();
            if (lecturer == null) return BlockNonLecturer();

            claim.Name = $"{lecturer.Name} {lecturer.Surname}";
            claim.Lecturer_ID = lecturer.UserID;
            claim.Hourly_Rate = lecturer.HourlyRate;
            claim.Date_Submitted = DateTime.Now;

            // Monthly limit
            var lecturerClaimsThisMonth = await _claimService.GetClaimsByLecturerMonthAsync(
                lecturer.UserID, claim.Date_Submitted.Month, claim.Date_Submitted.Year
            );

            if (lecturerClaimsThisMonth + claim.Total_Hours > 180)
            {
                ViewBag.Error = $"You cannot submit more than 180 hours in one month. " +
                                $"You currently have {lecturerClaimsThisMonth} hours submitted.";
                return View(claim);
            }

            if (claim.Total_Hours <= 0)
            {
                ViewBag.Error = "Please enter valid hours worked.";
                return View(claim);
            }

            // File Upload
            if (documents != null && documents.Count > 0)
            {
                if (claim.Documents == null)
                    claim.Documents = new List<UploadedDocument>();

                foreach (var file in documents)
                {
                    if (file.Length > 0)
                    {
                        var allowedExtensions = new[] { ".pdf", ".docx", ".txt", ".xlsx" };
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(extension))
                        {
                            ViewBag.Error = $"File type '{extension}' is not allowed.";
                            return View(claim);
                        }

                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                        Directory.CreateDirectory(uploadsFolder);

                        var encryptedName = Guid.NewGuid() + ".encrypted";
                        var encryptedPath = Path.Combine(uploadsFolder, encryptedName);

                        using (var stream = file.OpenReadStream())
                            await _encryptionService.EncryptFileAsync(stream, encryptedPath);

                        claim.Documents.Add(new UploadedDocument
                        {
                            FileName = file.FileName,
                            FilePath = "/uploads/" + encryptedName,
                            FileSize = file.Length,
                            IsEncrypted = true
                        });
                    }
                }
            }

            // Payment
            claim.Total_Payment = claim.Total_Hours * claim.Hourly_Rate;

            var errors = ClaimValidator.Validate(claim);
            if (errors.Count > 0)
            {
                ViewBag.ErrorList = errors;
                return View(claim);
            }

            await _claimService.AddClaimAsync(claim);

            TempData["Success"] = "Claim submitted successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(string filePath, string fileName)
        {
            if (!IsLecturer()) return BlockNonLecturer();

            var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath))
                return NotFound("File not found.");

            var decryptedStream = await _encryptionService.DecryptFileAsync(fullPath);
            decryptedStream.Position = 0;

            return File(decryptedStream, "application/octet-stream", fileName);
        }
    }
}
