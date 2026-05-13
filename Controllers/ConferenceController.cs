using Microsoft.AspNetCore.Mvc;
using GoldBranchAI.Services;

namespace GoldBranchAI.Controllers
{
    public class ConferenceController : Controller
    {
        private readonly BillingService _billing;

        public ConferenceController(BillingService billing)
        {
            _billing = billing;
        }

        public IActionResult Index()
        {
            // Create a default Room ID for convenience
            ViewBag.SuggestedRoomId = "GB-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            return View();
        }

        public IActionResult Room(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

            // Plan-based feature gating could be added here
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            var userRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            ViewBag.UserRole = userRole;
            ViewBag.RoomId = id;
            return View();
        }
    }
}
