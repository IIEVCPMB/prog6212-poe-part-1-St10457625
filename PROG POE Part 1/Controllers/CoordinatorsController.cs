using Microsoft.AspNetCore.Mvc;

namespace PROG_POE_Part_1.Controllers
{
    public class CoordinatorsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Verify()
        {
            return View();
        }
    }
}

