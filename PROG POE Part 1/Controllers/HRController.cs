using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PROG_POE_Part_1.Models;
using PROG_POE_Part_1.Services;
using System.IO;


namespace PROG_POE_Part_1.Controllers
{
    public class HRController : Controller
    {
        private readonly UserService _userService;
        private readonly ClaimService _claimService;

        public HRController(UserService userService, ClaimService claimService)
        {
            _userService = userService;
            _claimService = claimService;
        }

        private bool IsHR() => HttpContext.Session.GetString("UserRole") == "HR";

        private IActionResult BlockNonHR()
        {
            TempData["Error"] = "Login to access HR pages.";
            return RedirectToAction("Login", "Account");
        }

        public IActionResult Index()
        {
            if (!IsHR()) return BlockNonHR();
            return View();
        }

        public async Task<IActionResult> Users()
        {
            if (!IsHR()) return BlockNonHR();

            var users = await Task.Run(() => _userService.GetUsers());
            return View(users);
        }

        public IActionResult Create()
        {
            if (!IsHR()) return BlockNonHR();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            if (!IsHR()) return BlockNonHR();

            if (string.IsNullOrEmpty(user.Role))
            {
                ModelState.AddModelError("", "Role is required.");
                return View(user);
            }

            if (user.Role != "Lecturer")
                user.HourlyRate = 0;
            else if (user.HourlyRate <= 0)
            {
                ModelState.AddModelError("", "Hourly rate must be greater than zero for lecturers.");
                return View(user);
            }

            bool created = await Task.Run(() => _userService.AddUser(user));

            TempData[created ? "Success" : "Error"] = created
                ? "User created successfully."
                : "Failed to create user.";

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            if (!IsHR()) return BlockNonHR();

            var user = await Task.Run(() => _userService.GetUserByID(id));
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User user)
        {
            if (!IsHR()) return BlockNonHR();

            if (user.Role != "Lecturer")
                user.HourlyRate = 0;
            else if (user.HourlyRate <= 0)
            {
                ModelState.AddModelError("", "Hourly rate must be greater than zero for lecturers.");
                return View(user);
            }

            bool updated = await Task.Run(() => _userService.UpdateUser(user));

            TempData[updated ? "Success" : "Error"] = updated
                ? "User updated successfully."
                : "Failed to update user.";

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> LecturerList()
        {
            if (!IsHR()) return BlockNonHR();

            var lecturers = (await Task.Run(() => _userService.GetUsers()))
                .Where(u => u.Role == "Lecturer")
                .Select(u => new LecturerSummaryViewModel
                {
                    UserID = u.UserID,
                    FullName = $"{u.Name} {u.Surname}"
                })
                .ToList();

            return View(lecturers);
        }

        public async Task<IActionResult> LecturerReport(int id)
        {
            if (!IsHR()) return BlockNonHR();

            var lecturer = await Task.Run(() => _userService.GetUserByID(id));
            if (lecturer == null || lecturer.Role != "Lecturer")
                return NotFound("Lecturer not found.");

            var approvedClaims = await _claimService.GetClaimsByStatusAsync(Status.Approved);
            approvedClaims = approvedClaims
                .Where(c => c.Lecturer_ID == id)
                .ToList();

            var report = new ReportViewModel
            {
                UserID = lecturer.UserID,
                Name = lecturer.Name,
                Surname = lecturer.Surname,
                Email = lecturer.Email,
                Role = lecturer.Role,
                HourlyRate = lecturer.HourlyRate,
                TotalHours = approvedClaims.Sum(c => c.Total_Hours),
                TotalPayment = approvedClaims.Sum(c => c.Total_Payment)
            };

            ViewBag.Claims = approvedClaims;

            return View(report);
        }

        public async Task<IActionResult> ExportLecturerReportPdf(int id)
        {
            if (!IsHR()) return BlockNonHR();

            var lecturer = await Task.Run(() => _userService.GetUserByID(id));
            if (lecturer == null || lecturer.Role != "Lecturer")
                return NotFound("Lecturer not found.");

            var approvedClaims = await _claimService.GetClaimsByStatusAsync(Status.Approved);
            approvedClaims = approvedClaims
                .Where(c => c.Lecturer_ID == id)
                .ToList();

            var report = new ReportViewModel
            {
                UserID = lecturer.UserID,
                Name = lecturer.Name,
                Surname = lecturer.Surname,
                Email = lecturer.Email,
                Role = lecturer.Role,
                HourlyRate = lecturer.HourlyRate,
                TotalHours = approvedClaims.Sum(c => c.Total_Hours),
                TotalPayment = approvedClaims.Sum(c => c.Total_Payment)
            };

            using var ms = new MemoryStream();
            using var writer = new iText.Kernel.Pdf.PdfWriter(ms);
            using var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
            var document = new Document(pdf);

            document.Add(new Paragraph($"Lecturer Report: {report.Name} {report.Surname}")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(16));
            document.Add(new Paragraph($"Lecturer ID: {report.UserID}"));
            document.Add(new Paragraph($"Email: {report.Email}"));
            document.Add(new Paragraph($"Hourly Rate: R {report.HourlyRate:F2}"));
            document.Add(new Paragraph($"Total Hours: {report.TotalHours:F2}"));
            document.Add(new Paragraph($"Total Payment: R {report.TotalPayment:F2}"));
            document.Add(new Paragraph("\nApproved Claims:"));

            Table table = new Table(UnitValue.CreatePercentArray(4)).UseAllAvailableWidth();
            table.AddHeaderCell("Claim ID");
            table.AddHeaderCell("Date Submitted");
            table.AddHeaderCell("Total Hours");
            table.AddHeaderCell("Payment");

            foreach (var c in approvedClaims)
            {
                table.AddCell(c.Claim_ID.ToString());
                table.AddCell(c.Date_Submitted.ToString("yyyy-MM-dd"));
                table.AddCell(c.Total_Hours.ToString("F2"));
                table.AddCell(c.Total_Payment.ToString("F2"));
            }

            document.Add(table);
            document.Close();

            byte[] pdfBytes = ms.ToArray();
            return File(pdfBytes, "application/pdf", $"LecturerReport_{report.UserID}.pdf");
        }
    }
}
