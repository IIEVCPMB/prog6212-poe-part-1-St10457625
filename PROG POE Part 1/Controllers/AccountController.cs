using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PROG_POE_1.Models;
using PROG_POE_Part_1.Data;
using PROG_POE_Part_1.Models;
using PROG_POE_Part_1.Services;

namespace PROG_POE_Part_1.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserService _userService;

        public AccountController(UserService userService)
        {
            _userService = userService;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid login attempt.";
                return View(model);
            }

            //Check if user exists
            var user = _userService.ValidateLogin(model.Email, model.Password);

            if (user == null)
            {
                TempData["Error"] = "Incorrect email or password.";
                return View(model);
            }

            //Store session values
            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetString("FullName", $"{user.Name} {user.Surname}");
            HttpContext.Session.SetString("UserRole", user.Role);

            //Redirect user based on role
            return user.Role switch
            {
                "HR" => RedirectToAction("Index", "HR"),
                "Lecturer" => RedirectToAction("Index", "Claims"),
                "Coordinator" => RedirectToAction("Index", "Coordinators"),
                "Manager" => RedirectToAction("Index", "Managers"),
                _ => RedirectToAction("Login")
            };
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
