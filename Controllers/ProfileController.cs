using GoldBranchAI.Data;
using GoldBranchAI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GoldBranchAI.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProfileController(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Auth");

            int userId = int.Parse(userIdStr);
            return await GetProfileView(userId, true);
        }

        public async Task<IActionResult> Details(int id)
        {
            return await GetProfileView(id, false);
        }

        private async Task<IActionResult> GetProfileView(int userId, bool isOwnProfile)
        {
            var user = await _context.Users
                .Include(u => u.UserBadges)
                .Include(u => u.TodoTasks)
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null) return NotFound();

            ViewBag.CompletedTasks = user.TodoTasks?.Count(t => t.IsCompleted) ?? 0;
            ViewBag.OverallTasks = user.TodoTasks?.Count ?? 0;
            ViewBag.IsOwnProfile = isOwnProfile;

            // Arkadaşlık durumu kontrolü
            if (!isOwnProfile)
            {
                var currentUserIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(currentUserIdStr))
                {
                    int currentUserId = int.Parse(currentUserIdStr);
                    ViewBag.FriendshipStatus = await _context.Friendships
                        .FirstOrDefaultAsync(f => (f.UserId == currentUserId && f.FriendId == userId) || (f.UserId == userId && f.FriendId == currentUserId));
                }
            }

            // GitHub Repos Fetch
            if (!string.IsNullOrEmpty(user.GithubUsername))
            {
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Add("User-Agent", "GoldBranchAI-App");
                    var response = await client.GetAsync($"https://api.github.com/users/{user.GithubUsername}/repos?sort=updated&per_page=6");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        ViewBag.GithubRepos = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                    }
                }
                catch { /* GitHub API hatası durumunda sessiz kal */ }
            }
            
            return View("Index", user);
        }

        [HttpPost]
        public async Task<IActionResult> AddFriend(int friendId)
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Json(new { success = false });

            int userId = int.Parse(userIdStr);
            if (userId == friendId) return Json(new { success = false, message = "Kendinizi arkadaş olarak ekleyemezsiniz." });

            var existing = await _context.Friendships
                .FirstOrDefaultAsync(f => (f.UserId == userId && f.FriendId == friendId) || (f.UserId == friendId && f.FriendId == userId));

            if (existing != null) return Json(new { success = false, message = "Zaten arkadaşsınız veya istek beklemede." });

            var friendship = new Friendship
            {
                UserId = userId,
                FriendId = friendId,
                IsPending = false // Şimdilik direk eklesin (Kullanıcı isteği üzerine)
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAvatar(string avatarUrl)
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Json(new { success = false });

            int userId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(userId);
            
            if(user != null && !string.IsNullOrWhiteSpace(avatarUrl))
            {
                user.AvatarUrl = avatarUrl;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}
