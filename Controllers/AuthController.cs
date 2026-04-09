using GoldBranchAI.Data;
using GoldBranchAI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using GoldBranchAI.Services;

namespace GoldBranchAI.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public AuthController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
            SeedAdminUser();
        }

        private void SeedAdminUser()
        {
            if (!_context.Users.Any())
            {
                var admin = new AppUser
                {
                    FullName = "Enes Altındal (Admin)",
                    Email = "admin@test.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Role = "Admin"
                };
                _context.Users.Add(admin);
                _context.SaveChanges();
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User?.Identity?.IsAuthenticated == true) return RedirectToAction("Dashboard", "Task");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user != null)
            {
                bool isPasswordValid = false;
                
                try 
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
                } 
                catch (BCrypt.Net.SaltParseException) 
                {
                    // Eski düz metin şifreler için migration
                    if (user.Password == password) 
                    {
                        isPasswordValid = true;
                        user.Password = BCrypt.Net.BCrypt.HashPassword(password);
                        _context.SaveChanges();
                    }
                }

                if (isPasswordValid)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.FullName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role)
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    return RedirectToAction("Dashboard", "Task");
                }
            }

            ViewBag.Error = "E-posta veya şifre hatalı.";
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User?.Identity?.IsAuthenticated == true) return RedirectToAction("Dashboard", "Task");
            return View();
        }

        [HttpPost]
        public IActionResult Register(AppUser newUser)
        {
            if (_context.Users.Any(u => u.Email == newUser.Email))
            {
                ViewBag.Error = "Bu e-posta adresi zaten kullanımda!";
                return View(newUser);
            }

            newUser.Role = "Gelistirici";
            newUser.CreatedAt = DateTime.Now;
            newUser.Password = BCrypt.Net.BCrypt.HashPassword(newUser.Password); // BCrypt Hash

            _context.Users.Add(newUser);

            // YENİ EKLENEN KISIM: Sisteme kayıt olan kişiyi anında Terminale (Log'a) düşür
            var log = new SystemLog
            {
                ActionType = "KAYIT",
                Message = $"Sisteme yeni bir Geliştirici katıldı: {newUser.FullName} ({newUser.Email})"
            };
            _context.SystemLogs.Add(log);

            _context.SaveChanges();

            // Smtp Mail Gönderimi Mimarisi
            string emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                    <h2 style='color: #fbbf24; text-align: center;'>GoldBranch AI'a Hoş Geldin! 🚀</h2>
                    <p style='color: #555; font-size: 16px;'>Merhaba <strong>{newUser.FullName}</strong>,</p>
                    <p style='color: #555; font-size: 16px;'>Hesabın başarıyla oluşturuldu ve görev bekleyen sistemimizde yerini aldın. Artık yöneticilerin sana atadığı görevleri görebilir, yapay zeka ile entegre ekranları kullanabilirsin.</p>
                    <br>
                    <p style='color: #333; font-weight: bold;'>Giriş Bilgilerin:</p>
                    <ul>
                        <li><strong>E-Posta:</strong> {newUser.Email}</li>
                        <li><strong>Şifre:</strong> Güvenliğiniz için gizlenmiştir.</li>
                    </ul>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                    <p style='text-align: center; color: #999; font-size: 12px;'>Bu mesaj otomatik sistem tarafından gönderilmiştir.</p>
                </div>";
            
            // Asenkron olduğu için beklemiyoruz direkt arka plana atıyoruz
            _ = _emailService.SendEmailAsync(newUser.Email, "GoldBranch AI'a Hoş Geldin!", emailBody);

            TempData["Success"] = "Kayıt başarılı! Şimdi giriş yapabilirsiniz.";
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded) return RedirectToAction("Login");

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (email == null) return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                user = new AppUser
                {
                    FullName = name ?? "Google User",
                    Email = email,
                    Password = "GoogleOAuthLogin",
                    Role = "Gelistirici"
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                _context.SystemLogs.Add(new SystemLog { ActionType = "GİRİŞ / KAYIT", Message = $"'{user.FullName}' Google ile sisteme katıldı." });
                await _context.SaveChangesAsync();
            }

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            }, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return RedirectToAction("Dashboard", "Task");
        }

        // --- ŞİFREMİ UNUTTUM & SIFIRLAMA ---
        private static readonly Dictionary<string, (string Email, DateTime Expiry)> ResetTokens = new();

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Bu e-posta adresine ait bir hesap bulunamadı.";
                return View();
            }

            // Basit bir GUID Token üret ve Memory'ye kaydet
            var token = Guid.NewGuid().ToString("N");
            ResetTokens[token] = (email, DateTime.Now.AddMinutes(15)); // 15 dk geçerli

            var resetLink = Url.Action("ResetPassword", "Auth", new { token }, protocol: HttpContext.Request.Scheme);

            string emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                    <h2 style='color: #fbbf24; text-align: center;'>Şifre Sıfırlama Talebi 🔐</h2>
                    <p style='color: #555; font-size: 16px;'>Merhaba <strong>{user.FullName}</strong>,</p>
                    <p style='color: #555; font-size: 16px;'>Hesabına ait şifreyi sıfırlamak için bir talep aldık. Eğer bu işlemi sen yapmadıysan, bu maili dikkate alma.</p>
                    <div style='text-align:center; margin-top:20px; margin-bottom:20px;'>
                        <a href='{resetLink}' style='background-color: #fbbf24; color: #000; padding: 12px 24px; text-decoration: none; border-radius: 8px; font-weight: bold;'>Şifremi Yeni Baştan Belirle</a>
                    </div>
                    <p style='color: #888; font-size: 13px;'>Bu bağlantı sadece <strong>15 dakika</strong> geçerlidir!</p>
                </div>";

            await _emailService.SendEmailAsync(user.Email, "GoldBranch AI - Şifre Sıfırlama", emailBody);

            TempData["Success"] = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token) || !ResetTokens.ContainsKey(token))
            {
                ViewBag.Error = "Geçersiz veya süresi dolmuş bağlantı.";
                return View("ErrorPlain"); // Hata sayfası gerekecek ama şimdilik login atarız
            }

            var tokenData = ResetTokens[token];
            if (tokenData.Expiry < DateTime.Now)
            {
                ResetTokens.Remove(token);
                ViewBag.Error = "Bu bağlantının süresi dolmuş. Lütfen yeniden talep edin.";
                return View("ErrorPlain");
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string token, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Şifreler eşleşmiyor!";
                ViewBag.Token = token;
                return View();
            }

            if (!ResetTokens.ContainsKey(token))
            {
                ViewBag.Error = "Geçersiz bağlantı.";
                return View("ErrorPlain");
            }

            var tokenData = ResetTokens[token];
            if (tokenData.Expiry < DateTime.Now)
            {
                ResetTokens.Remove(token);
                ViewBag.Error = "Bu bağlantının süresi dolmuş.";
                return View("ErrorPlain");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == tokenData.Email);
            if (user != null)
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                _context.SaveChanges();

                // Logla
                _context.SystemLogs.Add(new SystemLog { ActionType = "GÜVENLİK", Message = $"'{user.FullName}' kullanıcısı şifresini sıfırladı." });
                _context.SaveChanges();
            }

            // Token'ı yak/kullanılamaz yap
            ResetTokens.Remove(token);

            TempData["Success"] = "Şifreniz başarıyla güncellendi! Artık yeni şifrenizle giriş yapabilirsiniz.";
            return RedirectToAction("Login");
        }
    }
}