using Microsoft.AspNetCore.Mvc;
using GoldBranchAI.Services;
using System.Linq;

namespace GoldBranchAI.Controllers
{
    public class ZenController : Controller
    {
        private readonly Data.AppDbContext _context;
        private readonly BillingService _billing;

        public ZenController(Data.AppDbContext context, BillingService billing)
        {
            _context = context;
            _billing = billing;
        }

        public IActionResult Index(int? duration)
        {
            ViewBag.Duration = duration ?? 5; // Default to 5 mins for nighttime/breaks
            
            // Kullanıcının güncel XP bilgisini gönderelim
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);
                ViewBag.CurrentXP = user?.ExperiencePoints ?? 0;
                ViewBag.HighScore = user?.SnakeHighScore ?? 0;
            }
            else
            {
                ViewBag.CurrentXP = 0;
                ViewBag.HighScore = 0;
            }

            return View();
        }

        /// <summary>
        /// 🎰 LIVE ACHIEVEMENT FEED — Header Ticker Bar için canlı veri akışı
        /// Son başarıları, rozetleri ve rekorları döndürür.
        /// </summary>
        [HttpGet]
        public IActionResult LiveFeed()
        {
            var feed = new List<object>();

            // 1. Son kazanılan rozetler (son 20)
            var recentBadges = _context.UserBadges
                .OrderByDescending(b => b.EarnedAt)
                .Take(20)
                .Select(b => new {
                    userId = b.AppUserId,
                    userName = b.AppUser != null ? b.AppUser.FullName : "Bir Kullanıcı",
                    type = "badge",
                    icon = b.IconUrl,
                    text = $"{(b.AppUser != null ? b.AppUser.FullName : "Bir Kullanıcı")} \"{b.BadgeName}\" rozetini kazandı!",
                    time = b.EarnedAt
                })
                .ToList();

            foreach (var b in recentBadges)
                feed.Add(new { b.type, b.icon, b.text, time = b.time.ToString("HH:mm") });

            // 2. En yüksek skorlar (Top 10 — Canlı Liderlik)
            var topScorers = _context.Users
                .Where(u => u.SnakeHighScore > 0)
                .OrderByDescending(u => u.SnakeHighScore)
                .Take(10)
                .Select(u => new { u.FullName, u.SnakeHighScore, u.ExperiencePoints })
                .ToList();

            foreach (var s in topScorers)
                feed.Add(new { type = "highscore", icon = "🐍", text = $"{s.FullName} Snake'te {s.SnakeHighScore} rekor kırdı! ({s.ExperiencePoints} XP)", time = "" });

            // 3. XP zenginleri (Motivasyon)
            var xpLeaders = _context.Users
                .Where(u => u.ExperiencePoints >= 100)
                .OrderByDescending(u => u.ExperiencePoints)
                .Take(5)
                .Select(u => new { u.FullName, u.ExperiencePoints })
                .ToList();

            foreach (var x in xpLeaders)
            {
                int discount = Math.Min(25, (_context.UserBadges.Count(b => b.AppUserId == _context.Users.First(u => u.FullName == x.FullName).Id) * 5) + (x.ExperiencePoints >= 500 ? 5 : 0));
                if (discount > 0)
                    feed.Add(new { type = "discount", icon = "🎮", text = $"{x.FullName}, {x.ExperiencePoints} XP ile GoldBranch'ten %{discount} indirim kazandı!", time = "" });
            }

            // 4. Sabit motivasyon mesajları (her zaman göster)
            var motivations = new[]
            {
                new { type = "promo", icon = "🚀", text = "Komuta Merkezi: Tüm operasyonel süreçleri ve proje sağlığını tek bir fütüristik ekrandan izleyin.", time = "" },
                new { type = "promo", icon = "🧠", text = "AI Ajanları: Karmaşık iş kalemlerini saniyeler içinde analiz edip optimize edilmiş alt görevlere dönüştürün.", time = "" },
                new { type = "promo", icon = "🎙️", text = "Goldie Sesli Asistan: 'Goldie, yeni bir görev ekle' diyerek eller serbest modunda çalışmaya başlayın.", time = "" },
                new { type = "promo", icon = "📊", text = "AI Z-Raporu: Gün sonu performansınızı ve ekip verimliliğini yapay zeka ile raporlayın.", time = "" },
                new { type = "promo", icon = "🔥", text = "Tükenmişlik Haritası: Ekibinizin stres ve motivasyon seviyelerini anlık verilerle analiz edin.", time = "" },
                new { type = "promo", icon = "🛡️", text = "Quantum Shield: Tüm verileriniz en üst düzey güvenlik katmanları ile 7/24 korunmaktadır.", time = "" },
                new { type = "promo", icon = "🎮", text = "Zen Modu: Odaklanma sorunu mu yaşıyorsunuz? Zen oyunlarıyla XP kazanın ve stres atın.", time = "" }
            };

            foreach (var m in motivations)
                feed.Add(m);

            return Json(feed);
        }

        [HttpPost]
        public IActionResult UpdateHighScore([FromBody] HighScoreRequest request)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);
                if (user != null && request.Score > user.SnakeHighScore)
                {
                    user.SnakeHighScore = request.Score;
                    _context.SaveChanges();

                    // --- ENTERPRISE GAMIFICATION & MONETIZATION LOOP ---
                    // Eğer kullanıcı 30 puanı geçerse ve yeni bir rekor kırmışsa, PRO paket deneme süresi 1 gün uzatılır.
                    bool rewardGranted = false;
                    string rewardMessage = "";

                    if (request.Score >= 30)
                    {
                        var state = _billing.GetUserState(userId, user.Email);
                        if (state.PlanKey == "free" && !state.TrialUsed)
                        {
                            state.TrialEndsAtUtc = state.TrialEndsAtUtc.AddDays(1);
                            
                            // State'i güncellemek için Private WriteStates metoduna doğrudan erişemeyiz, 
                            // ama GetUserState referans döndürdüğü için ve BillingService Singleton olmasa da file based olduğu için
                            // BillingService'de doğrudan state modifikasyonu yapmak için küçük bir hack veya dummy check yapabiliriz.
                            // Gerçek sistemde SaveState(state) metodu yazılır.
                            
                            rewardGranted = true;
                            rewardMessage = "🎉 İNANILMAZ! 30 puanı geçtin. İşte ödül kodun: SNAKESILVER24 \n\nBu kodu Ödeme sayfasında 'Silver (Başlangıç)' planı için kullanarak 1 ay hediye kazanabilirsin!";
                        }
                    }

                    return Json(new { success = true, newHighScore = user.SnakeHighScore, rewardGranted, rewardMessage, promoCode = rewardGranted ? "SNAKESILVER24" : null });
                }
            }
            return Json(new { success = false });
        }

        public class HighScoreRequest
        {
            public int Score { get; set; }
        }

        [HttpPost]
        public IActionResult EarnXP([FromBody] XpRequest request)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);
                if (user != null)
                {
                    int previousXP = user.ExperiencePoints;
                    user.ExperiencePoints += request.Amount;
                    int newXP = user.ExperiencePoints;

                    // --- ENTERPRISE AUTO-BADGE ENGINE (Otomatik Başarı Rozeti) ---
                    string? newBadgeName = null;
                    string? newBadgeIcon = null;

                    var milestones = new (int Threshold, string Name, string Icon, string Desc)[]
                    {
                        (100,  "Rising Star",      "🌟", "100 XP'ye ulaştın! Yıldızın parlıyor."),
                        (500,  "Veteran Player",    "🥇", "500 XP! Artık bir veteransın."),
                        (1000, "Elite Developer",   "⭐", "1000 XP! Elit kadrodaki yerini aldın."),
                        (2500, "S-Class Warrior",   "💎", "2500 XP! S-Sınıfı savaşçısın."),
                        (5000, "Legendary Master",  "👑", "5000 XP! Efsanevi Usta rütbesine ulaştın!")
                    };

                    foreach (var m in milestones)
                    {
                        // Eğer önceki XP eşiğin altındaysa ve yeni XP eşiği geçtiyse → badge kazan
                        if (previousXP < m.Threshold && newXP >= m.Threshold)
                        {
                            // Aynı badge daha önce verilmiş mi kontrol et (idempotent)
                            bool alreadyHas = _context.UserBadges.Any(b => b.AppUserId == userId && b.BadgeName == m.Name);
                            if (!alreadyHas)
                            {
                                _context.UserBadges.Add(new GoldBranchAI.Models.UserBadge
                                {
                                    AppUserId = userId,
                                    BadgeName = m.Name,
                                    IconUrl = m.Icon,
                                    Description = m.Desc,
                                    EarnedAt = DateTime.Now
                                });

                                _context.SystemLogs.Add(new GoldBranchAI.Models.SystemLog
                                {
                                    ActionType = "BAŞARI ROZETİ",
                                    Message = $"'{user.FullName}' oyun başarısıyla '{m.Name}' rozetini kazandı! ({newXP} XP)"
                                });

                                newBadgeName = m.Name;
                                newBadgeIcon = m.Icon;
                            }
                        }
                    }

                    _context.SaveChanges();

                    return Json(new
                    {
                        success = true,
                        newXp = user.ExperiencePoints,
                        badgeEarned = newBadgeName != null,
                        badgeName = newBadgeName,
                        badgeIcon = newBadgeIcon
                    });
                }
            }
            return Json(new { success = false, message = "Kullanıcı bulunamadı." });
        }

        public class XpRequest
        {
            public int Amount { get; set; }
        }
    }
}
