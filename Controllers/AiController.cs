using GoldBranchAI.Data;
using GoldBranchAI.Models;
using GoldBranchAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace GoldBranchAI.Controllers
{
    [Authorize]
    public class AiController : Controller
    {
        private readonly AppDbContext _context;
        private readonly GeminiService _geminiService;
        private readonly BillingService _billingService;

        public AiController(AppDbContext context, GeminiService geminiService, BillingService billingService)
        {
            _context = context;
            _geminiService = geminiService;
            _billingService = billingService;
        }

        private AppUser? GetCurrentUser()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            return _context.Users.FirstOrDefault(u => u.Email == email);
        }

        /// <summary>
        /// AI Görev Bölme sayfası (GET)
        /// </summary>
        [HttpGet]
        public IActionResult Breakdown()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Login", "Auth");
            if (currentUser.Role != "Proje Sefi" && currentUser.Role != "Admin")
                return RedirectToAction("Index", "Task");

            if (!_billingService.CanUseFeature(currentUser.Id, currentUser.Email, "ai_breakdown"))
            {
                TempData["UpgradeRequired"] = "AI Görev Bölme özelliği Pro ve üzeri planlarda kullanılabilir.";
                return RedirectToAction("Index", "Billing");
            }

            ViewBag.Developers = _context.Users.Where(u => u.Role == "Gelistirici").ToList();
            ViewBag.UserRole = currentUser.Role;
            return View();
        }

        /// <summary>
        /// Gemini API'ye proje açıklaması gönder, alt görevleri al (POST - AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Analyze([FromBody] AnalyzeRequest request)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return Unauthorized();
            if (currentUser.Role != "Proje Sefi" && currentUser.Role != "Admin")
                return Forbid();

            if (string.IsNullOrWhiteSpace(request.ProjectDescription))
                return BadRequest(new { error = "Proje açıklaması boş olamaz." });
            if (!_billingService.TryConsumeAiCredit(currentUser.Id, currentUser.Email, out var aiUsageMsg))
                return Json(new { success = false, error = aiUsageMsg, billingUrl = Url.Action("Index", "Billing") });

            try
            {
                var (tasks, rawJson) = await _geminiService.BreakdownProjectAsync(request.ProjectDescription);

                // Breakdown kaydını veritabanına kaydet
                var breakdown = new AiTaskBreakdown
                {
                    ProjectDescription = request.ProjectDescription,
                    GeneratedJson = rawJson,
                    SubTaskCount = tasks.Count,
                    CreatedByUserId = currentUser.Id,
                    CreatedAt = DateTime.Now
                };
                _context.AiTaskBreakdowns.Add(breakdown);

                _context.SystemLogs.Add(new SystemLog
                {
                    ActionType = "AI ANALİZ",
                    Message = $"{currentUser.FullName}, AI ile bir projeyi {tasks.Count} alt göreve böldü."
                });

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    usage = aiUsageMsg,
                    breakdownId = breakdown.Id,
                    tasks = tasks.Select(t => new
                    {
                        title = t.Title,
                        description = t.Description,
                        estimatedHours = t.EstimatedHours,
                        priority = t.Priority,
                        deadlineDays = t.DeadlineDays
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Onaylanan alt görevleri TodoTask tablosuna toplu kaydet (POST - AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ApplyBreakdown([FromBody] ApplyRequest request)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return Unauthorized();
            if (currentUser.Role != "Proje Sefi" && currentUser.Role != "Admin")
                return Forbid();

            var breakdown = _context.AiTaskBreakdowns.Find(request.BreakdownId);
            if (breakdown == null) return NotFound();
            if (breakdown.IsApplied) return BadRequest(new { error = "Bu analiz zaten sisteme aktarıldı." });

            var assignedUser = _context.Users.Find(request.AssignToUserId);
            if (assignedUser == null) return BadRequest(new { error = "Geçersiz geliştirici seçimi." });

            int addedCount = 0;
            foreach (var task in request.Tasks)
            {
                var newTask = new TodoTask
                {
                    Title = task.Title,
                    Description = task.Description,
                    EstimatedTimeHours = task.EstimatedHours,
                    AppUserId = request.AssignToUserId,
                    DueDate = DateTime.Now.AddDays(task.DeadlineDays > 0 ? task.DeadlineDays : 7),
                    Status = "Devam Ediyor",
                    CreatedAt = DateTime.Now
                };
                _context.Tasks.Add(newTask);
                addedCount++;
            }

            breakdown.IsApplied = true;

            _context.SystemLogs.Add(new SystemLog
            {
                ActionType = "AI GÖREV AKTARIMI",
                Message = $"{currentUser.FullName}, AI analizinden {addedCount} görevi {assignedUser.FullName} adlı kişiye toplu atadı."
            });

            await _context.SaveChangesAsync();

            return Json(new { success = true, addedCount });
        }

        /// <summary>
        /// Geliştiriciler için AI Araştırma/Sohbet sayfası (GET)
        /// </summary>
        [HttpGet]
        public IActionResult Research()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Login", "Auth");
            return View();
        }

        /// <summary>
        /// AI Sesli Sohbet - Tam Sayfa (GET)
        /// </summary>
        [HttpGet]
        public IActionResult VoiceChat()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Login", "Auth");
            return View();
        }

        /// <summary>
        /// Geliştirici soruları için AI'a istek at ve veritabanına kaydet
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AskResearch([FromBody] AskRequest request)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new { error = "Soru boş olamaz." });
            if (!_billingService.TryConsumeAiCredit(currentUser.Id, currentUser.Email, out var aiUsageMsg))
                return Json(new { success = false, error = aiUsageMsg, billingUrl = Url.Action("Index", "Billing") });

            var answer = await _geminiService.AskDeveloperQuestionAsync(request.Question);

            // Veritabanına logla
            var log = new AiResearchLog
            {
                AppUserId = currentUser.Id,
                RequestPrompt = request.Question,
                ResponseMarkdown = answer,
                CreatedAt = DateTime.Now
            };
            
            _context.AiResearchLogs.Add(log);
            await _context.SaveChangesAsync();

            return Json(new { success = true, answer = answer, usage = aiUsageMsg });
        }

        /// <summary>
        /// Seçili Chat Grubu'ndaki son konuşmaları çekip yapay zekaya (Gemini) özetletir 
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SummarizeGroup([FromBody] SummarizeRequest request)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return Unauthorized();
            if (currentUser.Role != "Proje Sefi" && currentUser.Role != "Admin") 
                return Forbid();

            if (!_billingService.CanUseFeature(currentUser.Id, currentUser.Email, "ai_summarize"))
                return Json(new { success = false, error = "Sohbet Özeti özelliği Business planında kullanılabilir.", billingUrl = Url.Action("Index", "Billing") });

            var group = _context.ChatGroups.FirstOrDefault(g => g.Id == request.GroupId);
            if (group == null) return NotFound(new { error = "Grup bulunamadı." });
            if (!_billingService.TryConsumeAiCredit(currentUser.Id, currentUser.Email, out var aiUsageMsg))
                return Json(new { success = false, error = aiUsageMsg, billingUrl = Url.Action("Index", "Billing") });

            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.ChatGroupId == request.GroupId)
                .OrderByDescending(m => m.SentAt)
                .Take(50)
                .ToListAsync();

            if (!messages.Any())
                return Json(new { success = false, error = "Özetlenecek kadar mesaj yok." });

            messages.Reverse(); // Cronolojik sıraya koy

            string chatLog = string.Join("\n", messages.Select(m => $"[{m.SentAt:HH:mm}] {m.Sender?.FullName}: {m.MessageText}"));
            string prompt = $"Sen profesyonel bir proje asistanısın. Aşağıda proje yöneticisi ve ekibinin yaptığı toplantı/yazışma kayıtları yatıyor.\n\nSenden İstenenler:\n1. Konuşmanın genel konusunu tek cümleyle özetle.\n2. Alınan kararları ve kime hangi görevlerin düştüğünü madde madde yaz.\n3. Kullanılacak dil profesyonel ve kurumsal olsun.\n\nKONUŞMA DÖKÜMÜ:\n{chatLog}";

            try
            {
                var summaryResponse = await _geminiService.AskDeveloperQuestionAsync(prompt);
                
                // Sisteme log atıyoruz (Admin görsün)
                _context.SystemLogs.Add(new SystemLog { ActionType = "AI ÖZET", Message = $"{currentUser.FullName}, '{group.GroupName}' grubu sohbetlerini yapay zekaya özetletti." });
                await _context.SaveChangesAsync();

                return Json(new { success = true, summary = summaryResponse, usage = aiUsageMsg });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Yapay Zeka Servisi şu an meşgul. Lütfen tekrar deneyin. Detay: " + ex.Message });
            }
        }

        // --- YENİ EKLENEN AI ÖNCELİKLENDİRME ---

        [HttpGet]
        public async Task<IActionResult> PrioritizeTasks()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Login", "Auth");
            if (currentUser.Role == "Admin") return RedirectToAction("Index", "Task");

            var tasksQuery = _context.Tasks.Include(t => t.AppUser).Where(t => !t.IsCompleted);
            
            if (currentUser.Role == "Gelistirici")
                tasksQuery = tasksQuery.Where(t => t.AppUserId == currentUser.Id);

            var tasks = await tasksQuery.ToListAsync();

            if (!tasks.Any())
            {
                TempData["Error"] = "Önceliklendirilecek aktif bir görev bulunamadı.";
                return RedirectToAction("Index", "Task");
            }

            string taskListJson = string.Join("\n", tasks.Select(t => $"- ID:{t.Id} | Başlık: {t.Title} | Bitiş Tarihi: {t.DueDate:dd.MM.yyyy} | Durum: {t.Status} | Tahmini Süre: {t.EstimatedTimeHours} Saat"));
            
            string prompt = $"Sen yetenekli bir Agile proje yöneticisisin. Sana aşağıdaki aktif görev listesini veriyorum. Görevlerin başlıklarına, teslim tarihlerine ve tahmini sürelerine bakarak en acil ve kritik olanlardan en az acil olanlara doğru mantıklı bir sıralama yapmanı istiyorum. Sonucunu bir markdown tablosu olarak hazırla ve yanına kısa 'Neden İlk Sırada?' açıklamaları ekle. İşte Görevler:\n{taskListJson}";

            ViewBag.OriginalCount = tasks.Count;
            
            try
            {
                ViewBag.AiResponse = await _geminiService.AskDeveloperQuestionAsync(prompt);
            }
            catch (Exception ex)
            {
                ViewBag.AiResponse = "Yapay Zeka Servisi şu an yanıt veremiyor. Lütfen daha sonra tekrar deneyin veya API anahtarınızı kontrol edin. Model: gemini-1.5-flash. Detay: " + ex.Message;
            }
            
            return View(); // Ai/PrioritizeTasks.cshtml oluşturuldu.
        }

        [HttpPost]
        public async Task<IActionResult> ProcessVoiceCommand([FromBody] VoiceCommandRequest request)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return Unauthorized();

            if (!_billingService.CanUseFeature(currentUser.Id, currentUser.Email, "ai_voice"))
                return Json(new { success = false, message = "Sesli Komut özelliği Pro ve üzeri planlarda kullanılabilir.", billingUrl = Url.Action("Index", "Billing") });

            if (string.IsNullOrWhiteSpace(request.Command))
                return Json(new { success = false, message = "Ses boş olamaz." });
            if (!_billingService.TryConsumeAiCredit(currentUser.Id, currentUser.Email, out var aiUsageMsg))
                return Json(new { success = false, message = aiUsageMsg, billingUrl = Url.Action("Index", "Billing") });

            var activeUsers = _context.Users.Select(u => new { u.Id, u.FullName, u.Email }).ToList();
            string jsonUsers = System.Text.Json.JsonSerializer.Serialize(activeUsers);

            string prompt = $@"Aşağıdaki metin bir proje yöneticisinin sesli asistanına verdiği bir emirdir. Bu emirden bir veritabanı TodoTask kaydı çıkartmanı istiyorum.
Mevcut Kullanıcılar Listesi: {jsonUsers}
Söylenen Komut: '{request.Command}'

Kurallar:
1. Söylenen metinde bir isim geçiyorsa, Mevcut Kullanıcılar Listesinden o kişiyi bulup 'AssignToUserId' (int) değişkenini ata. Bulamazsan Id'yi {currentUser.Id} yap (yani komutu verene ata).
2. Metinden çıkarılan asıl yapılacak işi 'Title' olarak çok kısa belirt.
3. Detayları 'Description' kısmına yaz.
4. Bir zaman geçiyorsa (yarın, haftaya, 3 gün sonra) bunu bulup bugünün tarihine ({DateTime.Now:yyyy-MM-dd}) göre hesaplayarak 'Deadline' (yyyy-MM-dd formatında) olarak ver. Bulamazsan 1 hafta sonrasını ver.
5. LÜTFEN SADECE VE SADECE aşağıdaki JSON formatında yanıt dön. Başka hiçbir açıklama, markdown bloğu (```json vs) ekleme. Doğrudan JSON string'i olsun.
{{
    ""Title"": ""... "",
    ""Description"": ""... "",
    ""AssignToUserId"": 1,
    ""Deadline"": ""2026-05-12""
}}
";
            try
            {
                var geminiResponse = await _geminiService.AskDeveloperQuestionAsync(prompt);
                geminiResponse = geminiResponse.Replace("```json", "").Replace("```", "").Trim();
                
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var dto = System.Text.Json.JsonSerializer.Deserialize<VoiceTaskDto>(geminiResponse, options);
                
                if (dto != null && !string.IsNullOrEmpty(dto.Title))
                {
                    var task = new TodoTask
                    {
                        Title = dto.Title,
                        Description = dto.Description + " *(Sesli Asistan Tarafından Oluşturuldu)*",
                        DueDate = dto.Deadline != default ? dto.Deadline : DateTime.Now.AddDays(7),
                        AppUserId = dto.AssignToUserId,
                        Status = "Bekliyor",
                        EstimatedTimeHours = 2,
                        IsCompleted = false,
                        CreatedAt = DateTime.Now
                    };
                    
                    _context.Tasks.Add(task);
                    _context.SystemLogs.Add(new SystemLog { ActionType = "AI SESLİ KOMUT", Message = $"'{currentUser.FullName}' sesli komutla bir görev oluşturdu: {task.Title}" });
                    await _context.SaveChangesAsync();
                    
                    return Json(new { success = true, message = task.Title, usage = aiUsageMsg });
                }
                
                return Json(new { success = false, message = "JSON Parse edilemedi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "AI Hatası: " + ex.Message });
            }
        }
        // --- 📊 AI SPRINT RAPORU ENDPOINT'İ ---
        [HttpGet]
        public async Task<IActionResult> GenerateSprintReport()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null || (currentUser.Role != "Proje Sefi" && currentUser.Role != "Admin"))
                return Forbid();

            if (!_billingService.CanUseFeature(currentUser.Id, currentUser.Email, "ai_sprint_report"))
            {
                TempData["UpgradeRequired"] = "Sprint Raporu özelliği Business planında kullanılabilir.";
                return RedirectToAction("Index", "Billing");
            }

            // Tüm veritabanını tarayıp istatistik çıkaralım (Son 7 gün ağırlıklı)
            var weekAgo = DateTime.Now.AddDays(-7);
            
            var completedTasksCount = await _context.Tasks.CountAsync(t => t.IsCompleted && t.CreatedAt >= weekAgo);
            var delayedTasksCount = await _context.Tasks.CountAsync(t => !t.IsCompleted && t.DueDate < DateTime.Now);
            var devs = await _context.Users.Where(u => u.Role == "Gelistirici").Include(u => u.TodoTasks).ToListAsync();

            var devStats = devs.Select(d => new {
                Name = d.FullName,
                Completed = d.TodoTasks?.Count(t => t.IsCompleted && t.CreatedAt >= weekAgo) ?? 0,
                TotalXP = d.ExperiencePoints,
                Overtime = d.TodoTasks?.Where(t => t.SpentTimeMinutes > (t.EstimatedTimeHours * 60)).Count() ?? 0
            }).ToList();

            string prompt = $@"
Aşağıda yazılım ekibimizin son 7 günlük çalışma istatistikleri ve genel durumu mevcuttur. 
Kurumsal, profesyonel ama bir o kadar da vizyoner ve enerjik bir dil kullanarak, markdown formatında yapılandırılmış detaylı bir 'Haftalık Sprint Özeti ve Tavsiyeler Raporu' oluştur.

VERİLER:
- Son 7 günde tamamlanan görev sayısı: {completedTasksCount}
- Geciken/Patlamış olan görev sayısı: {delayedTasksCount}
- Geliştirici Performansları: {System.Text.Json.JsonSerializer.Serialize(devStats)}

LÜTFEN ŞU BAŞLIKLARI KULLAN:
# Haftalık Sprint Performans Değerlendirmesi
## 🌟 Öne Çıkan Başarılar
## ⚠️ Tıkanıklıklar ve Gecikmeler
## 💡 AI Tavsiyeleri (Performans ve Sağlık)

Not: Sadece Markdown formatında, temiz bir çıktı ver.";

            var (reportMarkdown, _) = await _geminiService.BreakdownProjectAsync(prompt); // Metot ismi "BreakdownProjectAsync" olsa da string prompt besliyor
            
            // Generate a random ID or simply pass to view using ViewBag or TempData
            ViewBag.ReportText = reportMarkdown;
            return View("SprintReport");
        }
        /// <summary>
        /// Akademik ödev üretimi (Okul Projesi Modülü)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GenerateAcademicHomework([FromBody] AcademicRequest request)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Topic))
                return BadRequest(new { success = false, error = "Konu boş olamaz." });

            try
            {
                var content = await _geminiService.GenerateAcademicHomeworkAsync(request.Topic, request.University, request.Department, request.ExtraRequest);
                
                // --- Hafıza (Memory) Kayıt Ekleme ---
                var log = new AiResearchLog
                {
                    AppUserId = currentUser.Id,
                    RequestPrompt = $"[PROJE] {request.University} | {request.Department} | {request.Topic}",
                    ResponseMarkdown = content,
                    CreatedAt = DateTime.Now
                };
                _context.AiResearchLogs.Add(log);
                await _context.SaveChangesAsync();
                // ------------------------------------

                return Json(new { success = true, content = content });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcının geçmiş AI etkileşimlerini / projelerini getirir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMemoryLogs()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return Unauthorized();

            try
            {
                var logs = await _context.AiResearchLogs
                    .Where(l => l.AppUserId == currentUser.Id)
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(30)
                    .Select(l => new {
                        id = l.Id,
                        rawPrompt = l.RequestPrompt,
                        title = l.RequestPrompt.Length > 85 ? l.RequestPrompt.Substring(0, 85) + "..." : l.RequestPrompt,
                        content = l.ResponseMarkdown,
                        date = l.CreatedAt.ToString("dd MMM yyyy HH:mm"),
                        isProject = l.RequestPrompt.StartsWith("[PROJE]")
                    })
                    .ToListAsync();

                return Json(new { success = true, logs = logs });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Hafızadan proje siler
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeleteMemoryLog([FromBody] DeleteMemoryRequest request)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return Unauthorized();

            var log = await _context.AiResearchLogs.FirstOrDefaultAsync(l => l.Id == request.Id && l.AppUserId == currentUser.Id);
            if (log == null) return NotFound(new { success = false, message = "Kayıt bulunamadı veya silme yetkiniz yok." });

            try
            {
                _context.AiResearchLogs.Remove(log);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcının tüm hafızasını (Geçmiş projeleri) siler
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ClearMemoryLogs()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return Unauthorized();

            var logs = await _context.AiResearchLogs.Where(l => l.AppUserId == currentUser.Id).ToListAsync();

            try
            {
                _context.AiResearchLogs.RemoveRange(logs);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    // --- Request DTO'ları ---
    public class DeleteMemoryRequest
    {
        public int Id { get; set; }
    }
    public class AnalyzeRequest
    {
        public string ProjectDescription { get; set; } = string.Empty;
    }

    public class AskRequest
    {
        public string Question { get; set; } = string.Empty;
    }

    public class ApplyRequest
    {
        public int BreakdownId { get; set; }
        public int AssignToUserId { get; set; }
        public List<ApplyTaskItem> Tasks { get; set; } = new();
    }

    public class ApplyTaskItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int EstimatedHours { get; set; }
        public int DeadlineDays { get; set; }
    }

    public class SummarizeRequest
    {
        public int GroupId { get; set; }
    }

    public class VoiceCommandRequest
    {
        public string Command { get; set; } = string.Empty;
    }

    public class VoiceTaskDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int AssignToUserId { get; set; }
        public DateTime Deadline { get; set; }
    }

    public class AcademicRequest
    {
        public string University { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string ExtraRequest { get; set; } = string.Empty;
    }
}
