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
                CookieOptions options = new CookieOptions
                {
                    Expires = DateTime.Now.AddYears(1),
                    IsEssential = true
                };
                Response.Cookies.Append("lang_pref", lang, options);
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Task");
        }
    }
}
