using Microsoft.AspNetCore.Mvc;

namespace GoldBranchAI.Controllers
{
    public class ZenController : Controller
    {
        public IActionResult Index(int? duration)
        {
            ViewBag.Duration = duration ?? 25; // Default to 25 mins if not specified
            return View();
        }
    }
}
