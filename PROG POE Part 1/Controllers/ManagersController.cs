using Microsoft.AspNetCore.Mvc;

namespace PROG_POE_Part_1.Controllers
{
    public class ManagersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
