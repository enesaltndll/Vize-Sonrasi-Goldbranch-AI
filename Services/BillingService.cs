using System.Text.Json;

namespace GoldBranchAI.Services
{
    public class BillingService
    {
        private readonly string _stateFile;
        private readonly object _lock = new();

        public BillingService(IWebHostEnvironment env)
        {
            var dir = Path.Combine(env.ContentRootPath, "App_Data");
            Directory.CreateDirectory(dir);
            _stateFile = Path.Combine(dir, "billing-state.json");
        }

        // ==================== PLAN MODELS ====================

        public sealed class Plan
        {
            public string Key { get; set; } = "free";
            public string Name { get; set; } = "Free";
            public string NameTr { get; set; } = "Ücretsiz";
            public decimal MonthlyPrice { get; set; }
            public string Currency { get; set; } = "$";
            public int AiMonthlyLimit { get; set; }
            public int MaxUsers { get; set; }
            public int MaxActiveTasks { get; set; }
            public int MaxChatGroups { get; set; }
            public int MaxFileSizeMB { get; set; }
            public bool HasAdvancedReports { get; set; }
            public bool HasAiBreakdown { get; set; }
            public bool HasAiVoice { get; set; }
            public bool HasAiSummarize { get; set; }
            public bool HasAiSprintReport { get; set; }
            public bool HasExcelExport { get; set; }
            public bool HasBurnoutMap { get; set; }
            public bool HasFocusTimer { get; set; }
            public bool HasAllThemes { get; set; }
            public string Badge { get; set; } = "";
            public string Color { get; set; } = "#8b949e";
            public List<string> Highlights { get; set; } = new();
        }

        public sealed class UserState
        {
            public int UserId { get; set; }
            public string Email { get; set; } = string.Empty;
            public string PlanKey { get; set; } = "free";
            public string UsageMonth { get; set; } = DateTime.UtcNow.ToString("yyyy-MM");
            public int AiUsedInMonth { get; set; }
            public DateTime TrialEndsAtUtc { get; set; } = DateTime.UtcNow.AddDays(14);
            public bool TrialUsed { get; set; } = false;
            public string? LastCardMasked { get; set; }
            public DateTime? LastPaymentAtUtc { get; set; }
            public List<PaymentRecord> PaymentHistory { get; set; } = new();
        }

        public sealed class PaymentRecord
        {
            public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12].ToUpper();
            public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
            public string PlanKey { get; set; } = "";
            public decimal Amount { get; set; }
            public string CardMasked { get; set; } = "";
            public string Status { get; set; } = "success"; // demo: always success
        }

        public sealed class PaymentRequest
        {
            public string CardNumber { get; set; } = "";
            public string CardHolder { get; set; } = "";
            public string Expiry { get; set; } = "";
            public string Cvv { get; set; } = "";
            public string PlanKey { get; set; } = "";
            public string? PromoCode { get; set; }
        }

        public sealed class PaymentResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
            public string? TransactionId { get; set; }
        }

        // ==================== PLAN DEFINITIONS ====================

        public IReadOnlyList<Plan> GetPlans() => new List<Plan>
        {
            new()
            {
                Key = "free", Name = "Silver (Free)", NameTr = "Silver (Başlangıç)",
                MonthlyPrice = 0m, AiMonthlyLimit = 30,
                MaxUsers = 3, MaxActiveTasks = 25, MaxChatGroups = 1, MaxFileSizeMB = 5,
                HasAdvancedReports = false, HasAiBreakdown = false, HasAiVoice = false, HasAiSummarize = false, HasAiSprintReport = false, HasExcelExport = false, HasBurnoutMap = false, HasFocusTimer = false, HasAllThemes = false,
                Badge = "🥈", Color = "#8b949e",
                Highlights = new() { 
                    "Sadece Kendi API Anahtarınla Çalışır",
                    "Temel Kanban ve Görev Yönetimi",
                    "30 Sistem AI Kredisi (Acil Durum)",
                    "3 Ekip Üyesi",
                    "Standart Raporlama"
                }
            },
            new()
            {
                Key = "pro", Name = "Gold (Pro)", NameTr = "Gold (Profesyonel)",
                MonthlyPrice = 12m, AiMonthlyLimit = 400,
                MaxUsers = 15, MaxActiveTasks = 200, MaxChatGroups = 5, MaxFileSizeMB = 25,
                HasAdvancedReports = true, HasAiBreakdown = true, HasAiVoice = true, HasAiSummarize = false, HasAiSprintReport = false, HasExcelExport = true, HasBurnoutMap = false, HasFocusTimer = true, HasAllThemes = true,
                Badge = "⭐", Color = "#fbbf24",
                Highlights = new() { 
                    "Managed AI: Anahtar Gerekmez (Hızlı)",
                    "AI Sesli Komut Kontrolü",
                    "AI Smart-Breakdown (Görev Bölme)",
                    "15 Ekip Üyesi + Excel Export",
                    "Zen Modu & Odaklanma Analitiği"
                }
            },
            new()
            {
                Key = "business", Name = "Diamond (Business)", NameTr = "Diamond (Kurumsal)",
                MonthlyPrice = 39m, AiMonthlyLimit = 2000,
                MaxUsers = 9999, MaxActiveTasks = 99999, MaxChatGroups = 9999, MaxFileSizeMB = 100,
                HasAdvancedReports = true, HasAiBreakdown = true, HasAiVoice = true, HasAiSummarize = true, HasAiSprintReport = true, HasExcelExport = true, HasBurnoutMap = true, HasFocusTimer = true, HasAllThemes = true,
                Badge = "🏢", Color = "#a78bfa",
                Highlights = new() { 
                    "Limitsiz Enterprise AI Engine",
                    "AI Team Burnout Map (Duygu Analizi)",
                    "AI Sprint Strategy & Raporlama",
                    "AI Chat Summarize (Sohbet Özeti)",
                    "Sınırsız Kullanıcı & Özel Destek"
                }
            }
        };

        // ==================== PROMO CODE SYSTEM ====================

        public sealed class PromoResult
        {
            public bool Valid { get; set; }
            public string Message { get; set; } = "";
            public decimal DiscountPercent { get; set; }
            public string TargetPlanKey { get; set; } = "";
        }

        public PromoResult ValidatePromoCode(string code, string planKey)
        {
            code = code?.Trim().ToUpper() ?? "";
            
            // Snake Game Reward: SNAKESILVER24 (Silver Plan Reward)
            if (code == "SNAKESILVER24")
            {
                if (planKey != "free")
                {
                    return new PromoResult 
                    { 
                        Valid = false, 
                        Message = "Bu kod sadece Silver paketi için geçerlidir. Daha üst paketler için oyunlarda daha yüksek skor yapmalısın! 😉" 
                    };
                }
                return new PromoResult 
                { 
                    Valid = true, 
                    Message = "Tebrikler! Snake başarınız uygulandı. Silver Plan (1 Ay Hediye)!", 
                    DiscountPercent = 100, 
                    TargetPlanKey = "free" 
                };
            }

            // Diğer kodlar buraya eklenebilir...
            if (code == "GOLDBRANCH10")
            {
                return new PromoResult 
                { 
                    Valid = true, 
                    Message = "Kurumsal indirim uygulandı! %10 İndirim.", 
                    DiscountPercent = 10, 
                    TargetPlanKey = "" // Boş ise tüm planlarda geçerli
                };
            }

            return new PromoResult { Valid = false, Message = "Geçersiz veya süresi dolmuş kod." };
        }

        // ==================== STATE MANAGEMENT ====================

        public void ForceUpdatePlan(int userId, string planKey)
        {
            lock (_lock)
            {
                var list = ReadStates();
                var st = list.FirstOrDefault(x => x.UserId == userId);
                if (st != null)
                {
                    st.PlanKey = planKey;
                    st.TrialUsed = true;
                    WriteStates(list);
                }
            }
        }

        public List<UserState> GetAllStates()
        {
            lock (_lock)
            {
                return ReadStates();
            }
        }

        public UserState GetUserState(int userId, string email)
        {
            lock (_lock)
            {
                var list = ReadStates();
                var st = list.FirstOrDefault(x => x.UserId == userId);
                if (st == null)
                {
                    st = new UserState
                    {
                        UserId = userId,
                        Email = email,
                        PlanKey = "free",
                        TrialEndsAtUtc = DateTime.UtcNow.AddDays(14)
                    };
                    list.Add(st);
                    WriteStates(list);
                }
                else
                {
                    ResetMonthlyIfNeeded(st);
                    CheckTrialExpiry(st);
                    WriteStates(list);
                }
                return st;
            }
        }

        // ==================== FEATURE GATING ====================

        public bool CanUseFeature(int userId, string email, string featureName)
        {
            var state = GetUserState(userId, email);
            var plan = GetEffectivePlan(state);

            return featureName switch
            {
                "ai_breakdown" => plan.HasAiBreakdown,
                "ai_voice" => plan.HasAiVoice,
                "ai_summarize" => plan.HasAiSummarize,
                "ai_sprint_report" => plan.HasAiSprintReport,
                "excel_export" => plan.HasExcelExport,
                "burnout_map" => plan.HasBurnoutMap,
                "focus_timer" => plan.HasFocusTimer,
                "all_themes" => plan.HasAllThemes,
                "advanced_reports" => plan.HasAdvancedReports,
                _ => true // unknown features default to allowed
            };
        }

        public Plan GetEffectivePlan(UserState state)
        {
            var planKey = state.PlanKey;
            
            // Trial: if user is on free plan but trial hasn't expired, treat as pro
            if (planKey == "free" && !state.TrialUsed && state.TrialEndsAtUtc > DateTime.UtcNow)
            {
                planKey = "pro"; // trial gives pro features
            }

            return GetPlans().FirstOrDefault(p => p.Key == planKey) ?? GetPlans().First();
        }

        public string GetPlanBadge(UserState state)
        {
            if (state.PlanKey == "free" && !state.TrialUsed && state.TrialEndsAtUtc > DateTime.UtcNow)
                return "TRIAL";
            return state.PlanKey.ToUpper();
        }

        public int GetTrialDaysLeft(UserState state)
        {
            if (state.TrialUsed || state.PlanKey != "free") return 0;
            var days = (state.TrialEndsAtUtc - DateTime.UtcNow).TotalDays;
            return Math.Max(0, (int)Math.Ceiling(days));
        }

        // ==================== AI CREDITS ====================

        public bool TryConsumeAiCredit(int userId, string email, out string message)
        {
            lock (_lock)
            {
                var list = ReadStates();
                var st = list.FirstOrDefault(x => x.UserId == userId) ?? new UserState { UserId = userId, Email = email };
                if (!list.Any(x => x.UserId == userId)) list.Add(st);

                ResetMonthlyIfNeeded(st);

                var plan = GetEffectivePlan(st);
                if (st.AiUsedInMonth >= plan.AiMonthlyLimit)
                {
                    message = $"Aylık AI limitin doldu ({plan.AiMonthlyLimit}). Plan yükselterek devam edebilirsin.";
                    WriteStates(list);
                    return false;
                }

                st.AiUsedInMonth += 1;
                WriteStates(list);
                message = $"Kullanım: {st.AiUsedInMonth}/{plan.AiMonthlyLimit}";
                return true;
            }
        }

        // ==================== FAKE PAYMENT ====================

        public PaymentResult ProcessDemoPayment(int userId, string email, PaymentRequest req)
        {
            lock (_lock)
            {
                // Validate card number (demo: accept any 13+ digit number)
                var cleanCard = req.CardNumber?.Replace(" ", "").Replace("-", "") ?? "";
                if (cleanCard.Length < 13)
                    return new PaymentResult { Success = false, Message = "Kart numarası en az 13 haneli olmalıdır." };

                if (string.IsNullOrWhiteSpace(req.CardHolder))
                    return new PaymentResult { Success = false, Message = "Kart sahibi adı gereklidir." };

                if (string.IsNullOrWhiteSpace(req.Expiry) || req.Expiry.Length < 4)
                    return new PaymentResult { Success = false, Message = "Geçerli bir son kullanma tarihi giriniz (AA/YY)." };

                if (string.IsNullOrWhiteSpace(req.Cvv) || req.Cvv.Length < 3)
                    return new PaymentResult { Success = false, Message = "Geçerli bir CVV giriniz." };

                var plans = GetPlans();
                var targetPlan = plans.FirstOrDefault(p => p.Key == req.PlanKey);
                if (targetPlan == null)
                    return new PaymentResult { Success = false, Message = "Geçersiz plan seçimi." };

                decimal finalAmount = targetPlan.MonthlyPrice;

                // --- PROMO CODE VALIDATION (No cheating!) ---
                if (!string.IsNullOrWhiteSpace(req.PromoCode))
                {
                    var promo = ValidatePromoCode(req.PromoCode, req.PlanKey);
                    if (!promo.Valid)
                    {
                        return new PaymentResult { Success = false, Message = $"Geçersiz Promosyon Kodu: {promo.Message}" };
                    }
                    // Apply discount
                    finalAmount = finalAmount * (1 - (promo.DiscountPercent / 100));
                }

                // Demo mode: always approve payment
                var list = ReadStates();
                var st = list.FirstOrDefault(x => x.UserId == userId);
                if (st == null)
                {
                    st = new UserState { UserId = userId, Email = email };
                    list.Add(st);
                }

                var maskedCard = "**** **** **** " + cleanCard[^4..];
                var txnId = "TXN-" + Guid.NewGuid().ToString("N")[..10].ToUpper();

                var payment = new PaymentRecord
                {
                    Id = txnId,
                    PlanKey = req.PlanKey,
                    Amount = finalAmount,
                    CardMasked = maskedCard,
                    PaidAtUtc = DateTime.UtcNow,
                    Status = "success"
                };

                st.PlanKey = req.PlanKey;
                st.LastCardMasked = maskedCard;
                st.LastPaymentAtUtc = DateTime.UtcNow;
                st.TrialUsed = true; // payment = trial bitti
                st.PaymentHistory.Add(payment);

                ResetMonthlyIfNeeded(st);
                WriteStates(list);

                return new PaymentResult
                {
                    Success = true,
                    Message = $"{targetPlan.Name} planına başarıyla geçiş yapıldı! Ödenen: ${finalAmount:F2}. İşlem: {txnId}",
                    TransactionId = txnId
                };
            }
        }

        // ==================== PLAN CHANGE (no payment) ====================

        public void ChangePlan(int userId, string email, string newPlan)
        {
            lock (_lock)
            {
                var plans = GetPlans().Select(p => p.Key).ToHashSet();
                if (!plans.Contains(newPlan)) return;

                var list = ReadStates();
                var st = list.FirstOrDefault(x => x.UserId == userId);
                if (st == null)
                {
                    st = new UserState { UserId = userId, Email = email };
                    list.Add(st);
                }
                st.PlanKey = newPlan;
                if (newPlan != "free") st.TrialUsed = true;
                ResetMonthlyIfNeeded(st);
                WriteStates(list);
            }
        }

        // ==================== INTERNAL HELPERS ====================

        private void CheckTrialExpiry(UserState st)
        {
            if (st.PlanKey == "free" && !st.TrialUsed && st.TrialEndsAtUtc <= DateTime.UtcNow)
            {
                st.TrialUsed = true; // trial expired
            }
        }

        private void ResetMonthlyIfNeeded(UserState st)
        {
            var nowMonth = DateTime.UtcNow.ToString("yyyy-MM");
            if (!string.Equals(st.UsageMonth, nowMonth, StringComparison.Ordinal))
            {
                st.UsageMonth = nowMonth;
                st.AiUsedInMonth = 0;
            }
        }

        private List<UserState> ReadStates()
        {
            if (!File.Exists(_stateFile)) return new List<UserState>();
            var json = File.ReadAllText(_stateFile);
            if (string.IsNullOrWhiteSpace(json)) return new List<UserState>();
            return JsonSerializer.Deserialize<List<UserState>>(json) ?? new List<UserState>();
        }

        private void WriteStates(List<UserState> states)
        {
            var json = JsonSerializer.Serialize(states, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_stateFile, json);
        }
    }
}
