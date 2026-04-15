using GoldBranchAI.Data;
using GoldBranchAI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using GoldBranchAI.Hubs;
using GoldBranchAI.Services;

namespace GoldBranchAI.Controllers
{
    [Authorize]
    public class TaskController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly EmailService _emailService;

        public TaskController(AppDbContext context, IWebHostEnvironment webHostEnvironment, IHubContext<NotificationHub> notificationHub, EmailService emailService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _notificationHub = notificationHub;
            _emailService = emailService;
        }

        private AppUser? GetCurrentUser()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            return _context.Users.FirstOrDefault(u => u.Email == email);
        }

        public IActionResult Dashboard()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Logout", "Auth");

            ViewBag.UserRole = currentUser.Role;
            
            // Stats logic
            DateTime urgentThreshold = DateTime.Now.AddHours(72);
            var allTasks = _context.Tasks.ToList();
            var myTasks = allTasks.Where(t => t.AppUserId == currentUser.Id).ToList();

            if (currentUser.Role == "Admin")
            {
                ViewBag.TotalTasks = allTasks.Count;
                ViewBag.CompletedTasks = allTasks.Count(t => t.IsCompleted);
                ViewBag.UrgentTasks = allTasks.Count(t => !t.IsCompleted && t.DueDate < urgentThreshold);
                ViewBag.TotalDevelopers = _context.Users.Count(u => u.Role == "Gelistirici");
            }
            else if (currentUser.Role == "Proje Sefi")
            {
                ViewBag.TotalTasks = allTasks.Count;
                ViewBag.ActiveTeamTasks = allTasks.Count(t => !t.IsCompleted);
                ViewBag.CompletedTasks = allTasks.Count(t => t.IsCompleted);
                ViewBag.UrgentTasks = allTasks.Count(t => !t.IsCompleted && t.DueDate < urgentThreshold);
                ViewBag.TotalDevelopers = _context.Users.Count(u => u.Role == "Gelistirici");
            }
            else // Gelistirici
            {
                ViewBag.MyTotalTasks = myTasks.Count;
                ViewBag.MyCompletedTasks = myTasks.Count(t => t.IsCompleted);
                ViewBag.UrgentTasks = myTasks.Count(t => !t.IsCompleted && t.DueDate < urgentThreshold);
                
                // For global stats if needed
                ViewBag.TotalTasks = allTasks.Count;
                ViewBag.CompletedTasks = allTasks.Count(t => t.IsCompleted);
            }

            ViewBag.StatusLabels = System.Text.Json.JsonSerializer.Serialize(new[] { "Bekliyor", "Devam Ediyor", "Onay Bekliyor", "Revize", "Tamamlandı" });
            
            var chartSource = currentUser.Role == "Gelistirici" ? myTasks : allTasks;
            ViewBag.StatusData = System.Text.Json.JsonSerializer.Serialize(new[] {
                chartSource.Count(t => t.Status == "Bekliyor"),
                chartSource.Count(t => t.Status == "Devam Ediyor"),
                chartSource.Count(t => t.Status == "Onay Bekliyor"),
                chartSource.Count(t => t.Status == "Revize"),
                chartSource.Count(t => t.Status == "Tamamlandi" || t.IsCompleted)
            });

            return View();
        }


        public IActionResult Index()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Logout", "Auth");

            List<TodoTask> tasks;
            ViewBag.UserRole = currentUser.Role;

            DateTime urgentThreshold = DateTime.Now.AddHours(72);

            // 1. GELİŞTİRİCİ VERİLERİ
            if (currentUser.Role == "Gelistirici")
            {
                tasks = _context.Tasks.Include(t => t.DependsOnTask).Where(t => t.AppUserId == currentUser.Id && !t.IsCompleted).OrderBy(t => t.DueDate).ToList();
                ViewBag.MyTotal = tasks.Count();
                ViewBag.MyCompleted = _context.Tasks.Count(t => t.AppUserId == currentUser.Id && t.IsCompleted);
                ViewBag.MyUrgent = tasks.Count(t => t.DueDate < urgentThreshold);
            }
            // 2. PROJE ŞEFİ VERİLERİ
            else if (currentUser.Role == "Proje Sefi")
            {
                tasks = _context.Tasks.Include(t => t.DependsOnTask).Where(t => !t.IsCompleted).OrderBy(t => t.DueDate).ToList();
                ViewBag.ActiveTeamTasks = tasks.Count();
                ViewBag.UrgentTeamTasks = tasks.Count(t => t.DueDate < urgentThreshold);
                ViewBag.TotalDevelopers = _context.Users.Count(u => u.Role == "Gelistirici");
            }
            // 3. ADMIN VERİLERİ (Sadece istatistikler, liste boş)
            else
            {
                tasks = new List<TodoTask>();
                ViewBag.TotalTasks = _context.Tasks.Count();
                ViewBag.CompletedTasks = _context.Tasks.Count(t => t.IsCompleted);
                ViewBag.UrgentTasks = _context.Tasks.Count(t => t.DueDate < urgentThreshold && !t.IsCompleted);
            }

            return View(tasks);
        }

        public IActionResult Create()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Logout", "Auth");
            // Görev atamayı sadece Proje Şefi yapabilir!
            if (currentUser.Role != "Proje Sefi") return RedirectToAction("Index");

            ViewBag.Developers = _context.Users.Where(u => u.Role == "Gelistirici").ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(TodoTask task, IFormFile? uploadedFile)
        {
            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + uploadedFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fileStream);
                }
                task.AttachedFilePath = "/uploads/" + uniqueFileName;
            }

            task.CreatedAt = DateTime.Now;
            task.Status = "Devam Ediyor";
            _context.Tasks.Add(task);

            // YENİ GÖREVİ DE LOGLARA DÜŞÜRELİM
            var assignedUser = _context.Users.Find(task.AppUserId);
            if (assignedUser != null)
            {
                _context.SystemLogs.Add(new SystemLog { ActionType = "YENİ GÖREV", Message = $"Proje Şefi, {assignedUser.FullName} adlı kişiye '{task.Title}' görevini atadı." });
                
                // SIGNALR BİLDİRİM MERKEZİ TETİKLEMESİ
                var notif = new SystemNotification { 
                    AppUserId = assignedUser.Id, 
                    Message = $"Sana yeni bir görev atandı: {task.Title}", 
                    Link = "/Task/Index" 
                };
                _context.SystemNotifications.Add(notif);
                await _context.SaveChangesAsync();
                
                await _notificationHub.Clients.User(assignedUser.Id.ToString())
                   .SendAsync("ReceiveNotification", notif.Message, notif.Link);
                   
                // EMAIL GÖNDERİMİ
                string emailBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ffcc00; border-radius: 10px;'>
                        <h2 style='color: #d97706; text-align: center;'>🚨 Yeni Bir Görev Atandı!</h2>
                        <p style='color: #555; font-size: 16px;'>Merhaba <strong>{assignedUser.FullName}</strong>,</p>
                        <p style='color: #555; font-size: 16px;'>Proje Şefi tarafından sana yeni bir görev tahsis edildi.</p>
                        <br>
                        <div style='background: #fdf6e3; padding: 15px; border-left: 4px solid #fbbf24;'>
                            <h3 style='margin-top:0; color: #b45309;'>{task.Title}</h3>
                            <p style='font-size: 14px; color: #333;'>Son Teslim Tarihi: <strong>{task.DueDate:dd MMM yyyy HH:mm}</strong></p>
                        </div>
                        <br>
                        <a href='http://localhost:5161/Task/Index' style='display:inline-block; padding: 10px 20px; background: #fbbf24; color: #000; text-decoration: none; font-weight: bold; border-radius: 5px;'>Sisteme Git ve Görevi İncele</a>
                    </div>";
                _ = _emailService.SendEmailAsync(assignedUser.Email, $"Yeni Görev: {task.Title}", emailBody);
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // YENİ EKLENEN ONAY VE REVİZE ALGORİTMASI (Eski Delete metodunun yerini aldı)
        public async Task<IActionResult> ChangeStatus(int id, string newStatus)
        {
            var task = _context.Tasks.Include(t => t.AppUser).Include(t => t.DependsOnTask).FirstOrDefault(t => t.Id == id);
            var currentUser = GetCurrentUser();
            var currentUserRole = currentUser?.Role ?? "";

            if (task != null)
            {
                // HİYERARŞİ KONTROLÜ
                if ((newStatus == "Devam Ediyor" || newStatus == "Onay Bekliyor" || newStatus == "Tamamlandı") && task.DependsOnTask != null && !task.DependsOnTask.IsCompleted)
                {
                    string errMsg = $"[HİYERARŞİ KİLİDİ] Bu göreve başlamadan önce '{task.DependsOnTask.Title}' görevinin tamamlanması gerekiyor.";
                    if (Request.Headers["Accept"].ToString().Contains("application/json") || Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Sec-Fetch-Dest"] == "empty")
                    {
                        return BadRequest(errMsg);
                    }
                    TempData["Error"] = errMsg;
                    return RedirectToAction("Index");
                }

                // Geliştirici doğrudan Tamamlandı yapamaz
                if (newStatus == "Tamamlandı" && currentUserRole == "Gelistirici")
                {
                    if (Request.Headers["Accept"].ToString().Contains("application/json") || Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Sec-Fetch-Dest"] == "empty")
                        return BadRequest("Geliştiriciler görevleri doğrudan tamamlayamaz. Onaya sunmalısınız.");
                    TempData["Error"] = "Yetki Hatası: Sadece onay bekliyor yapabilirsiniz.";
                    return RedirectToAction("Index");
                }

                task.Status = newStatus;

                // Eğer Şef "Tamamlandı" derse ancak o zaman sistemden düşer
                if (newStatus == "Tamamlandı" && currentUserRole == "Proje Sefi")
                {
                    task.IsCompleted = true;
                    
                    // 🎮 OYUNLAŞTIRMA (GAMIFICATION) MANTIĞI: XP ve Rozet Ataması
                    if (task.AppUser != null)
                    {
                        task.AppUser.ExperiencePoints += 50; // Her başarılı görev = 50 XP
                        
                        // 1. Rozet: İlk Adım
                        bool hasFirstTaskBadge = _context.UserBadges.Any(b => b.AppUserId == task.AppUser.Id && b.BadgeName == "İlk Adım");
                        if (!hasFirstTaskBadge)
                        {
                            _context.UserBadges.Add(new UserBadge {
                                AppUserId = task.AppUser.Id,
                                BadgeName = "İlk Adım",
                                Description = "Sisteme giriş yaptın ve projeye ilk katkını sağladın!",
                                IconUrl = "https://cdn-icons-png.flaticon.com/512/912/912304.png"
                            });
                            _context.SystemNotifications.Add(new SystemNotification {
                                AppUserId = task.AppUser.Id,
                                Message = "🏅 BİR ROZET KAZANDINIZ: Bismillah! 'İlk Adım' rozeti kilidi açıldı.",
                                Link = "/Profile"
                            });
                        }

                        // 2. Rozet: Hız Sabitleyici & Bonus XP (Süresinden önce bitirirse)
                        if (task.EstimatedTimeHours > 0 && task.SpentTimeMinutes > 0)
                        {
                            if ((task.SpentTimeMinutes / 60.0) < task.EstimatedTimeHours)
                            {
                                task.AppUser.ExperiencePoints += 20; // Bonus 20 XP
                                bool hasSpeedBadge = _context.UserBadges.Any(b => b.AppUserId == task.AppUser.Id && b.BadgeName == "Hız Sabitleyici");
                                if (!hasSpeedBadge)
                                {
                                    _context.UserBadges.Add(new UserBadge {
                                        AppUserId = task.AppUser.Id,
                                        BadgeName = "Hız Sabitleyici",
                                        Description = "İşi sana verilen tahmini süreden bile erken bitirdin!",
                                        IconUrl = "https://cdn-icons-png.flaticon.com/512/1004/1004314.png"
                                    });
                                    _context.SystemNotifications.Add(new SystemNotification {
                                        AppUserId = task.AppUser.Id,
                                        Message = "⚡ BİR ROZET KAZANDINIZ: Çok hızlısın! 'Hız Sabitleyici' rozeti ve +20 Bonus XP eklendi.",
                                        Link = "/Profile"
                                    });
                                }
                            }
                        }

                        // ✅ BİLDİRİM: Onay bildirimi geliştiriciye
                        var approvalNotif = new SystemNotification {
                            AppUserId = task.AppUser.Id,
                            Message = $"✅ '{task.Title}' görevin Proje Şefi tarafından ONAYLANDI! +50 XP kazandın.",
                            Link = $"/Task/Details/{task.Id}"
                        };
                        _context.SystemNotifications.Add(approvalNotif);
                        await _context.SaveChangesAsync();
                        await _notificationHub.Clients.User(task.AppUser.Id.ToString())
                            .SendAsync("ReceiveNotification", approvalNotif.Message, approvalNotif.Link);

                        // 🔗 BİLDİRİM: Bu göreve bağımlı olan Görevlerin kilitleri açıldı!
                        var dependantTasks = _context.Tasks.Where(t => t.DependsOnTaskId == task.Id && !t.IsCompleted).Include(t => t.AppUser).ToList();
                        foreach(var depTask in dependantTasks)
                        {
                            if(depTask.AppUser != null)
                            {
                                var depNotif = new SystemNotification {
                                    AppUserId = depTask.AppUser.Id,
                                    Message = $"🔓 KİLİT AÇILDI: '{depTask.Title}' göevinizi engelleyen süreç tamamlandı! İşe başlayabilirsiniz.",
                                    Link = $"/Task/Details/{depTask.Id}"
                                };
                                _context.SystemNotifications.Add(depNotif);
                                await _notificationHub.Clients.User(depTask.AppUser.Id.ToString())
                                    .SendAsync("ReceiveNotification", depNotif.Message, depNotif.Link);
                            }
                        }
                        await _context.SaveChangesAsync();
                    }

                    _context.SystemLogs.Add(new SystemLog { ActionType = "GÖREV ONAYI", Message = $"Proje Şefi, {task.AppUser?.FullName} adlı kişinin '{task.Title}' görevini ONAYLADI." });
                }
                // Geliştirici onaya gönderirse
                else if (newStatus == "Onay Bekliyor")
                {
                    _context.SystemLogs.Add(new SystemLog { ActionType = "GÖREV TESLİMİ", Message = $"{task.AppUser?.FullName}, '{task.Title}' görevini Şefin onayına sundu." });

                    // 🔔 BİLDİRİM: Tüm Proje Şeflerine "Onay bekleyen görev var" bildirimi
                    var chefs = _context.Users.Where(u => u.Role == "Proje Sefi").ToList();
                    foreach (var chef in chefs)
                    {
                        var chefNotif = new SystemNotification {
                            AppUserId = chef.Id,
                            Message = $"📋 {task.AppUser?.FullName}, '{task.Title}' görevini onayınıza sundu.",
                            Link = $"/Task/Details/{task.Id}"
                        };
                        _context.SystemNotifications.Add(chefNotif);
                        await _context.SaveChangesAsync();
                        await _notificationHub.Clients.User(chef.Id.ToString())
                            .SendAsync("ReceiveNotification", chefNotif.Message, chefNotif.Link);
                    }
                }
                // Şef revize isterse
                else if (newStatus == "Revize")
                {
                    _context.SystemLogs.Add(new SystemLog { ActionType = "GÖREV İADESİ", Message = $"Proje Şefi, '{task.Title}' görevini eksik bularak {task.AppUser?.FullName} adlı kişiye REVİZE için geri gönderdi." });

                    // 🔔 BİLDİRİM: Geliştiriciye "Revize istendi" bildirimi
                    if (task.AppUser != null)
                    {
                        var reviseNotif = new SystemNotification {
                            AppUserId = task.AppUser.Id,
                            Message = $"🔄 '{task.Title}' görevin REVİZE için geri gönderildi. Lütfen düzelt ve tekrar sun.",
                            Link = $"/Task/Details/{task.Id}"
                        };
                        _context.SystemNotifications.Add(reviseNotif);
                        await _context.SaveChangesAsync();
                        await _notificationHub.Clients.User(task.AppUser.Id.ToString())
                            .SendAsync("ReceiveNotification", reviseNotif.Message, reviseNotif.Link);
                    }
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        public IActionResult Details(int id)
        {
            var task = _context.Tasks
                .Include(t => t.AppUser)
                .Include(t => t.DependsOnTask)
                .Include(t => t.Comments!)
                    .ThenInclude(c => c.AppUser)
                .FirstOrDefault(t => t.Id == id);
                
            if (task == null) return NotFound();
            return View(task);
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int TodoTaskId, string CommentText)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null || string.IsNullOrWhiteSpace(CommentText)) return RedirectToAction("Details", new { id = TodoTaskId });

            var comment = new TaskComment
            {
                TodoTaskId = TodoTaskId,
                AppUserId = currentUser.Id,
                CommentText = CommentText,
                CreatedAt = DateTime.Now
            };

            _context.TaskComments.Add(comment);
            
            var task = _context.Tasks.Include(t => t.AppUser).FirstOrDefault(t => t.Id == TodoTaskId);
            if (task != null && task.AppUserId != currentUser.Id) 
            {
                // Send notification to task owner
                _context.SystemNotifications.Add(new SystemNotification {
                    AppUserId = task.AppUserId,
                    Message = $"💬 {currentUser.FullName}, sana atanan '{task.Title}' görevine yorum yaptı.",
                    Link = $"/Task/Details/{task.Id}"
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = TodoTaskId });
        }

        public IActionResult Edit(int id)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Logout", "Auth");
            if (currentUser.Role != "Proje Sefi") return RedirectToAction("Index");

            var task = _context.Tasks.Find(id);
            if (task == null) return NotFound();

            ViewBag.Developers = _context.Users.Where(u => u.Role == "Gelistirici").ToList();
            return View(task);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, TodoTask updatedTask, IFormFile? uploadedFile)
        {
            var existingTask = _context.Tasks.Find(id);
            if (existingTask == null) return NotFound();

            existingTask.Title = updatedTask.Title;
            existingTask.Description = updatedTask.Description;
            existingTask.DueDate = updatedTask.DueDate;
            existingTask.EstimatedTimeHours = updatedTask.EstimatedTimeHours;
            existingTask.AppUserId = updatedTask.AppUserId;

            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + uploadedFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fileStream);
                }
                existingTask.AttachedFilePath = "/uploads/" + uniqueFileName;
            }

            _context.SystemLogs.Add(new SystemLog { ActionType = "GÖREV GÜNC.", Message = $"Proje Şefi, '{existingTask.Title}' görevini güncelledi." });
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Logout", "Auth");
            if (currentUser.Role != "Proje Sefi") return RedirectToAction("Index");

            var task = _context.Tasks.Find(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                _context.SystemLogs.Add(new SystemLog { ActionType = "GÖREV İPTAL", Message = $"Proje Şefi, '{task.Title}' görevini sistemden sildi." });
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeleteAllTasks()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Logout", "Auth");
            if (currentUser.Role != "Proje Sefi" && currentUser.Role != "Admin") return Forbid();

            // Tüm görevleri, onlara bağlı yorumları ve breakdown'ları sil
            var tasks = _context.Tasks.ToList();
            var comments = _context.TaskComments.ToList();
            var breakdowns = _context.AiTaskBreakdowns.ToList();

            _context.TaskComments.RemoveRange(comments);
            _context.Tasks.RemoveRange(tasks);
            _context.AiTaskBreakdowns.RemoveRange(breakdowns);

            _context.SystemLogs.Add(new SystemLog { ActionType = "SİSTEM TEMİZLİĞİ", Message = $"{currentUser.FullName}, sistemdeki tüm görevleri ve AI analizlerini sıfırladı (Demo Modu)." });
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
        // --- YENİ EKLENEN ÖZELLİKLER ---

        public IActionResult Kanban()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Logout", "Auth");
            IQueryable<TodoTask> tasksQuery = _context.Tasks.Include(t => t.AppUser).Include(t => t.DependsOnTask);

            if (currentUser.Role == "Gelistirici")
                tasksQuery = tasksQuery.Where(t => t.AppUserId == currentUser.Id);

            var tasks = tasksQuery.ToList();
            return View(tasks);
        }

        public IActionResult Calendar()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Logout", "Auth");
            IQueryable<TodoTask> tasksQuery = _context.Tasks.Include(t => t.AppUser);

            if (currentUser.Role == "Gelistirici")
                tasksQuery = tasksQuery.Where(t => t.AppUserId == currentUser.Id);

            var tasks = tasksQuery.ToList();

            // Sadece başlık, tarih ve durum dönen basit bir JSON (FullCalendar için)
            var events = tasks.Select(t => new {
                title = t.Title + (currentUser.Role != "Gelistirici" ? $" ({t.AppUser?.FullName})" : ""),
                start = t.DueDate.ToString("yyyy-MM-dd"),
                url = $"/Task/Details/{t.Id}",
                color = t.IsCompleted ? "#10b981" : (t.Status == "Revize" || t.Status == "Acil") ? "#ef4444" : "#fbbf24"
            });

            ViewBag.EventsJson = System.Text.Json.JsonSerializer.Serialize(events);
            return View();
        }

        // ⏱️ ZAMAN TAKİP SİSTEMİ (TIME TRACKER) ENDPOINT'I
        [HttpPost]
        public async Task<IActionResult> LogTime([FromBody] TimeLogRequest request)
        {
            var task = await _context.Tasks.FindAsync(request.TaskId);
            if (task != null)
            {
                task.SpentTimeMinutes += request.MinutesAdded;
                await _context.SaveChangesAsync();
                return Ok(new { success = true, newTotal = task.SpentTimeMinutes });
            }
            return NotFound(new { success = false });
        }

        public class TimeLogRequest
        {
            public int TaskId { get; set; }
            public int MinutesAdded { get; set; }
        }
    }
}