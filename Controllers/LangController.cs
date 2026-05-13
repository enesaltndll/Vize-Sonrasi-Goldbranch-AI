using Microsoft.AspNetCore.Mvc;

namespace GoldBranchAI.Controllers
{
    public class LangController : Controller
    {
        [HttpGet]
        public IActionResult Switch(string lang, string returnUrl)
        {
            if (lang == "tr" || lang == "en")
            {
                // Cookie Sync
                CookieOptions options = new CookieOptions
                {
                    Expires = DateTime.Now.AddYears(1),
                    IsEssential = true
                };
                Response.Cookies.Append("lang_pref", lang, options);

                // Database Sync (If Logged In)
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    using (var scope = HttpContext.RequestServices.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
                        var user = context.Users.Find(userId);
                        if (user != null)
                        {
                            user.PreferredLanguage = lang;
                            context.SaveChanges();
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Dashboard", "Task");
        }
    }
}
