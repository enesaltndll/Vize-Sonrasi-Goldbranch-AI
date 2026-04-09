using System.Diagnostics;
using GoldBranchAI.Models;
using GoldBranchAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoldBranchAI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BillingService _billing;

        public HomeController(ILogger<HomeController> logger, BillingService billing)
        {
            _logger = logger;
            _billing = billing;
        }

        public IActionResult Index()
        {
            ViewBag.Plans = _billing.GetPlans();
            return View();
        }

        public IActionResult Pricing()
        {
            ViewBag.Plans = _billing.GetPlans();
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
