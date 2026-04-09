using GoldBranchAI.Data;
using GoldBranchAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GoldBranchAI.Controllers
{
    [Authorize]
    public class BillingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly BillingService _billing;

        public BillingController(AppDbContext context, BillingService billing)
        {
            _context = context;
            _billing = billing;
        }

        private (int Id, string Email)? GetUser()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email)) return null;
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            return user == null ? null : (user.Id, user.Email);
        }

        public IActionResult Index()
        {
            var u = GetUser();
            if (u == null) return RedirectToAction("Login", "Auth");

            var state = _billing.GetUserState(u.Value.Id, u.Value.Email);
            ViewBag.Plans = _billing.GetPlans();
            ViewBag.State = state;
            ViewBag.EffectivePlan = _billing.GetEffectivePlan(state);
            ViewBag.PlanBadge = _billing.GetPlanBadge(state);
            ViewBag.TrialDaysLeft = _billing.GetTrialDaysLeft(state);
            return View();
        }

        [HttpPost]
        public IActionResult ProcessPayment([FromBody] BillingService.PaymentRequest request)
        {
            var u = GetUser();
            if (u == null) return Unauthorized();

            var result = _billing.ProcessDemoPayment(u.Value.Id, u.Value.Email, request);

            if (result.Success)
            {
                _context.SystemLogs.Add(new Models.SystemLog
                {
                    ActionType = "ÖDEME",
                    Message = $"Demo ödeme başarılı: {request.PlanKey.ToUpper()} planı. İşlem: {result.TransactionId}"
                });
                _context.SaveChanges();
            }

            return Json(result);
        }

        [HttpPost]
        public IActionResult ChangePlan(string planKey)
        {
            var u = GetUser();
            if (u == null) return RedirectToAction("Login", "Auth");

            _billing.ChangePlan(u.Value.Id, u.Value.Email, planKey);
            TempData["Success"] = "Plan güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // API: Check if a feature is available for current user
        [HttpGet]
        public IActionResult CheckFeature(string feature)
        {
            var u = GetUser();
            if (u == null) return Unauthorized();

            var allowed = _billing.CanUseFeature(u.Value.Id, u.Value.Email, feature);
            var state = _billing.GetUserState(u.Value.Id, u.Value.Email);
            var effectivePlan = _billing.GetEffectivePlan(state);

            return Json(new
            {
                allowed,
                currentPlan = state.PlanKey,
                effectivePlan = effectivePlan.Key,
                feature
            });
        }
    }
}
