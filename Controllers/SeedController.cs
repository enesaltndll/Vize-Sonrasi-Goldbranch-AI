using GoldBranchAI.Data;
using GoldBranchAI.Models;
using GoldBranchAI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldBranchAI.Controllers
{
    public class SeedController : Controller
    {
        private readonly AppDbContext _context;
        private readonly BillingService _billingService;

        public SeedController(AppDbContext context, BillingService billingService)
        {
            _context = context;
            _billingService = billingService;
        }

        [HttpGet]
        public async Task<IActionResult> ResetDatabase()
        {
            // 1. Clear related data to avoid FK conflicts
            var allUsers = await _context.Users.ToListAsync();
            var adminId = allUsers.FirstOrDefault(u => u.Email.ToLower() == "admin@test.com")?.Id;

            // Delete for everyone EXCEPT admin if we want to keep admin clean, 
            // but for a full reset, it's safer to clear everything and re-seed admin if needed.
            // Here we only delete for users we are about to remove.

            foreach (var user in allUsers)
            {
                if (user.Email.ToLower() != "admin@test.com")
                {
                    // Clear all restricted relations
                    var memberships = _context.ChatGroupMembers.Where(m => m.AppUserId == user.Id);
                    _context.ChatGroupMembers.RemoveRange(memberships);

                    var messages = _context.ChatMessages.Where(m => m.SenderId == user.Id || m.ReceiverId == user.Id);
                    _context.ChatMessages.RemoveRange(messages);

                    var notifications = _context.SystemNotifications.Where(n => n.AppUserId == user.Id);
                    _context.SystemNotifications.RemoveRange(notifications);

                    var comments = _context.TaskComments.Where(c => c.AppUserId == user.Id);
                    _context.TaskComments.RemoveRange(comments);

                    var filterViews = _context.TaskFilterViews.Where(v => v.AppUserId == user.Id);
                    _context.TaskFilterViews.RemoveRange(filterViews);

                    var tasks = _context.Tasks.Where(t => t.AppUserId == user.Id);
                    _context.Tasks.RemoveRange(tasks);
                    
                    var badges = _context.UserBadges.Where(b => b.AppUserId == user.Id);
                    _context.UserBadges.RemoveRange(badges);

                    var timeLogs = _context.DailyTimeLogs.Where(l => l.AppUserId == user.Id);
                    _context.DailyTimeLogs.RemoveRange(timeLogs);

                    _context.Users.Remove(user);
                }
            }
            await _context.SaveChangesAsync();

            // 2. Add requested test users
            var usersToAdd = new List<AppUser>
            {
                new AppUser { FullName = "Ediz Test (Silver)", Email = "ediz@test.com", Password = BCrypt.Net.BCrypt.HashPassword("ediz123"), Role = "Gelistirici", ExperiencePoints = 50 },
                new AppUser { FullName = "Ercan Test (Gold)", Email = "ercan@test.com", Password = BCrypt.Net.BCrypt.HashPassword("erco123"), Role = "Gelistirici", ExperiencePoints = 1200 },
                new AppUser { FullName = "Enes Test (Diamond)", Email = "enes@test.com", Password = BCrypt.Net.BCrypt.HashPassword("enes123"), Role = "Proje Sefi", ExperiencePoints = 3000 }
            };

            foreach (var u in usersToAdd)
            {
                if (!await _context.Users.AnyAsync(x => x.Email == u.Email))
                {
                    _context.Users.Add(u);
                }
            }
            await _context.SaveChangesAsync();

            // 3. Update Billing State for each user
            var ediz = await _context.Users.FirstAsync(x => x.Email == "ediz@test.com");
            var ercan = await _context.Users.FirstAsync(x => x.Email == "ercan@test.com");
            var enes = await _context.Users.FirstAsync(x => x.Email == "enes@test.com");

            _billingService.ForceUpdatePlan(ediz.Id, "free");     // Silver
            _billingService.ForceUpdatePlan(ercan.Id, "pro");    // Gold
            _billingService.ForceUpdatePlan(enes.Id, "business"); // Diamond

            return Content("Veritabanı sıfırlandı ve test kullanıcıları (Ediz, Ercan, Enes) oluşturuldu. Planlar tanımlandı. /Account/Login sayfasından giriş yapabilirsiniz.");
        }
    }
}
