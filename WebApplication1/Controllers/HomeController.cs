using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole(Roles.Admin)) return RedirectToAction("Dashboard", "Admin");
                if (User.IsInRole(Roles.Advisor)) return RedirectToAction("Dashboard", "Advisor");
                if (User.IsInRole(Roles.Student)) return RedirectToAction("Dashboard", "Student");
                if (User.IsInRole(Roles.Parent)) return RedirectToAction("Dashboard", "Parent");
            }
            return RedirectToAction("Login", "Account");
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
