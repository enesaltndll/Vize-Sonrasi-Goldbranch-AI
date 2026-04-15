using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;

namespace GoldBranchAI.Services
{
    public class AiSubTask
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("estimatedHours")]
        public int EstimatedHours { get; set; } = 2;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "medium";

        [JsonPropertyName("deadlineDays")]
        public int DeadlineDays { get; set; } = 7;

        [JsonPropertyName("dependsOnIndex")]
        public int? DependsOnIndex { get; set; }

        [JsonPropertyName("techBranch")]
        public string TechBranch { get; set; } = string.Empty;

        [JsonPropertyName("starterCode")]
        public string StarterCode { get; set; } = string.Empty;
    }

    public class AiBreakdownResult
    {
        [JsonPropertyName("tasks")]
        public List<AiSubTask> Tasks { get; set; } = new();
    }

    /// <summary>
    /// AI Servisi: Çoklu Provider Desteği (Cohere, SambaNova, OpenAI, Gemini, Anthropic)
    /// Kullanıcı kendi API Key'ini girdiğinde seçtiği provider'ı kullanır.
    /// Girmezse varsayılan fallback (Cohere → SambaNova).
    /// </summary>
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        // ==================== COHERE ====================
        private async Task<string?> CallCohere(string systemPrompt, string userContent, string? apiKey = null)
        {
            var key = apiKey ?? _configuration["CohereApi:ApiKey"];
            var model = _configuration["CohereApi:Model"] ?? "command-a-03-2025";
            if (string.IsNullOrEmpty(key)) return null;

            try
            {
                var requestBody = new
                {
                    model = model,
                    messages = new object[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userContent }
                    },
                    temperature = 0.5
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.cohere.com/v2/chat")
                {
                    Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[Cohere] Hata {Status}: {Body}", response.StatusCode, body.Length > 200 ? body[..200] : body);
                    return null;
                }

                var doc = JsonDocument.Parse(body);
                return doc.RootElement
                    .GetProperty("message")
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Cohere] Exception");
                return null;
            }
        }

        // ==================== SAMBANOVA ====================
        private async Task<string?> CallSambaNova(string systemPrompt, string userContent, string? apiKey = null)
        {
            var key = apiKey ?? _configuration["SambaNovaApi:ApiKey"];
            var model = _configuration["SambaNovaApi:Model"] ?? "Meta-Llama-3.3-70B-Instruct";
            if (string.IsNullOrEmpty(key)) return null;

            try
            {
                var requestBody = new
                {
                    model = model,
                    messages = new object[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userContent }
                    },
                    temperature = 0.5
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sambanova.ai/v1/chat/completions")
                {
                    Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[SambaNova] Hata {Status}: {Body}", response.StatusCode, body.Length > 200 ? body[..200] : body);
                    return null;
                }

                // OpenAI uyumlu format: choices[0].message.content
                var doc = JsonDocument.Parse(body);
                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SambaNova] Exception");
                return null;
            }
        }

        // ==================== OPENAI (GPT-4o, GPT-3.5-turbo vb.) ====================
        private async Task<string?> CallOpenAI(string systemPrompt, string userContent, string apiKey, string? model = null)
        {
            if (string.IsNullOrEmpty(apiKey)) return null;
            var modelName = model ?? "gpt-4o";

            try
            {
                var requestBody = new
                {
                    model = modelName,
                    messages = new object[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userContent }
                    },
                    temperature = 0.5
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
                {
                    Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[OpenAI] Hata {Status}: {Body}", response.StatusCode, body.Length > 200 ? body[..200] : body);
                    return null;
                }

                var doc = JsonDocument.Parse(body);
                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[OpenAI] Exception");
                return null;
            }
        }

        // ==================== GOOGLE GEMINI ====================
        private async Task<string?> CallGemini(string systemPrompt, string userContent, string apiKey, string? model = null)
        {
            if (string.IsNullOrEmpty(apiKey)) return null;
            var modelName = model ?? "gemini-2.0-flash";

            try
            {
                var requestBody = new
                {
                    system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                    contents = new[]
                    {
                        new { role = "user", parts = new[] { new { text = userContent } } }
                    },
                    generationConfig = new { temperature = 0.5 }
                };

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                };

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[Gemini] Hata {Status}: {Body}", response.StatusCode, body.Length > 200 ? body[..200] : body);
                    return null;
                }

                var doc = JsonDocument.Parse(body);
                return doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Gemini] Exception");
                return null;
            }
        }

        // ==================== ANTHROPIC CLAUDE ====================
        private async Task<string?> CallAnthropic(string systemPrompt, string userContent, string apiKey, string? model = null)
        {
            if (string.IsNullOrEmpty(apiKey)) return null;
            var modelName = model ?? "claude-3-5-sonnet-20241022";

            try
            {
                var requestBody = new
                {
                    model = modelName,
                    max_tokens = 4096,
                    system = systemPrompt,
                    messages = new object[]
                    {
                        new { role = "user", content = userContent }
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
                {
                    Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                };
                request.Headers.Add("x-api-key", apiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[Anthropic] Hata {Status}: {Body}", response.StatusCode, body.Length > 200 ? body[..200] : body);
                    return null;
                }

                var doc = JsonDocument.Parse(body);
                return doc.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Anthropic] Exception");
                return null;
            }
        }

        // ==================== ANA CAGRI: PROVIDER SEÇİMİYLE ====================

        /// <summary>
        /// Kullanıcının seçtiği provider ile API çağrısı yapar.
        /// Provider "default" veya null ise varsayılan Cohere → SambaNova fallback.
        /// </summary>
        public async Task<string> CallWithProvider(string systemPrompt, string userContent,
            string? provider = null, string? apiKey = null, string? model = null)
        {
            // Kullanıcı kendi provider'ını ve API key'ini girdiyse direkt onu kullan
            if (!string.IsNullOrEmpty(provider) && provider != "default" && !string.IsNullOrEmpty(apiKey))
            {
                _logger.LogInformation("Kullanıcı özel AI Provider: {Provider}", provider);

                string? result = provider.ToLower() switch
                {
                    "openai" => await CallOpenAI(systemPrompt, userContent, apiKey, model),
                    "gemini" => await CallGemini(systemPrompt, userContent, apiKey, model),
                    "anthropic" => await CallAnthropic(systemPrompt, userContent, apiKey, model),
                    "cohere" => await CallCohere(systemPrompt, userContent, apiKey),
                    "sambanova" => await CallSambaNova(systemPrompt, userContent, apiKey),
                    _ => null
                };

                if (!string.IsNullOrEmpty(result))
                {
                    _logger.LogInformation("{Provider} başarılı yanıt verdi.", provider);
                    return result;
                }

                _logger.LogWarning("{Provider} başarısız, varsayılan fallback deneniyor...", provider);
            }

            // Varsayılan Fallback: Cohere → SambaNova
            _logger.LogInformation("AI istek başlatılıyor: Cohere (birincil)");
            var fallbackResult = await CallCohere(systemPrompt, userContent);
            if (!string.IsNullOrEmpty(fallbackResult))
            {
                _logger.LogInformation("Cohere başarılı yanıt verdi.");
                return fallbackResult;
            }

            _logger.LogInformation("Cohere başarısız, SambaNova deneniyor...");
            fallbackResult = await CallSambaNova(systemPrompt, userContent);
            if (!string.IsNullOrEmpty(fallbackResult))
            {
                _logger.LogInformation("SambaNova başarılı yanıt verdi.");
                return fallbackResult;
            }

            throw new HttpRequestException("Hiçbir AI servisi yanıt veremedi. Lütfen API anahtarınızı kontrol edin veya birkaç dakika sonra tekrar deneyin.");
        }

        // ==================== BAĞLANTI TESTİ ====================

        /// <summary>
        /// Kullanıcının girdiği API Key'in çalışıp çalışmadığını test eder.
        /// </summary>
        public async Task<(bool Success, string Message)> TestProviderConnection(string provider, string apiKey, string? model = null)
        {
            try
            {
                var result = await CallWithProvider(
                    "Sen bir test asistanısın. Sadece 'Bağlantı başarılı!' yaz.",
                    "Test",
                    provider, apiKey, model
                );

                return (!string.IsNullOrEmpty(result), "✅ Bağlantı başarılı! AI sağlayıcınız hazır.");
            }
            catch (Exception ex)
            {
                return (false, $"❌ Bağlantı başarısız: {ex.Message}");
            }
        }

        // ==================== PUBLIC METODLAR ====================

        public async Task<(List<AiSubTask> Tasks, string RawJson)> BreakdownProjectAsync(
            string projectDescription,
            int totalDurationValue = 7,
            string totalDurationUnit = "days",
            string periodType = "daily",
            int dailyWorkHours = 8,
            DateTime? startDate = null,
            string? programmingLanguage = null,
            string? provider = null, string? apiKey = null, string? model = null)
        {
            var start = startDate ?? DateTime.Now;
            
            // Toplam gün hesaplama
            int totalDays = totalDurationUnit.ToLower() switch
            {
                "days" => totalDurationValue,
                "weeks" => totalDurationValue * 7,
                "months" => totalDurationValue * 30,
                _ => totalDurationValue
            };

            // Toplam çalışma saati
            int totalWorkHours = totalDays * dailyWorkHours;

            // Saat başına görev dağılımı
            int idealTaskCount;
            if (periodType == "daily")
                idealTaskCount = Math.Min(totalDays, 15);
            else
            {
                int weekCount = Math.Max(1, (int)Math.Ceiling(totalDays / 7.0));
                idealTaskCount = Math.Min(weekCount * 2, 15);
            }

            idealTaskCount = Math.Max(2, idealTaskCount);
            int hoursPerTask = Math.Max(1, totalWorkHours / idealTaskCount);
            int maxTasks = Math.Min(15, idealTaskCount + 2);
            int minTasks = Math.Max(2, idealTaskCount - 1);

            // Programlama dili & kod üretimi
            string langSection = "";
            string codeJsonField = "";
            if (!string.IsNullOrEmpty(programmingLanguage) && programmingLanguage != "none")
            {
                langSection = $@"
TEKNOLOJİ DALLANDIRMASI:
- Kullanılacak teknoloji/dil: {programmingLanguage}
- Her görevi bu teknolojiye uygun alt dallara böl.
- 'techBranch' alanında görevin hangi katmana ait olduğunu yaz (örn: Backend, Frontend, Database, API, UI, Config, Test).
- 'starterCode' alanına, o görev için {programmingLanguage} dilinde çalıştırılabilir başlangıç kodu yaz.
  Kod gerçekçi, çalışabilir ve en az 8-15 satır olmalı. Yorum satırları ekle.
  Eğer görev kod gerektirmiyorsa (dokümantasyon, planlama gibi) boş string bırak.
- Görevleri teknoloji katmanlarına göre mantıklı sırala (önce veritabanı/model, sonra backend, sonra frontend)";

                codeJsonField = @",
      ""techBranch"": ""Backend"",
      ""starterCode"": ""// Başlangıç kodu""";
            }

            var systemPrompt = $@"Sen dünya çapında uzman bir teknik proje yöneticisi ve yazılım mimarısın.
Sana verilen proje tanımını analiz et ve alt görevlere böl.

⚠️ KESİN VE MUTLAK ZAMAN KURALLARI (İHLAL ETME!):

🕐 ZAMAN BÜTÇESİ:
- Proje toplam: {totalDays} gün
- Günlük çalışma: {dailyWorkHours} saat (ASLA AŞILAMAZ!)
- TOPLAM BÜTÇE: {totalWorkHours} saat ({totalDays}×{dailyWorkHours})
- Başlangıç: {start:yyyy-MM-dd}

📊 SAAT DAĞILIMI (EN KRİTİK KURAL!):
- Her görevin estimatedHours değeri 1 ile {dailyWorkHours} arasında olmalı!
- Tek bir görev asla {dailyWorkHours} saati geçemez!
- Tüm görevlerin estimatedHours toplamı TAM OLARAK {totalWorkHours} saat olmalı!
- Her görev yaklaşık {hoursPerTask} saat olsun.
- YANLIŞ: estimatedHours toplamı {totalWorkHours}'den fazla veya az olan görev listesi!

📅 DEADLINE (deadlineDays):
- İlk görev: deadlineDays = 1
- Son görev: deadlineDays = {totalDays}
- Aradakiler eşit aralıklarla dağıtılacak.
- HİÇBİR görev {totalDays} günü AŞAMAZ!

📋 GÖREV SAYISI: En az {minTasks}, en fazla {maxTasks} görev.
Periyot: {(periodType == "daily" ? "GÜNLÜK — Her güne dengeli görev." : "HAFTALIK — Her haftaya 1-2 görev.")}
{langSection}

🔗 BAĞIMLILIK (HİYERARŞİ):
- Projenin mantıklı sırayla yapılması için görevler arası bağımlılık kur.
- 'dependsOnIndex' alanı ile bu görevin başlaması için HANGİ görevin (0-indexed sıra) bitmesi gerektiğini belirt.
- İlk görevde 'dependsOnIndex' null olmalı.
- Diğer görevler bir önceki mantıksal göreve bağlı olmalı (örn: API yapılmadan UI yapılamaz). Asla kendisinden sonraki veya kendisine bağlı olan bir göreve bağımlı olmasın (döngüsel bağımlılık YASAK).

🔴 KONTROL: Görev listeni teslim etmeden önce estimatedHours toplamının {totalWorkHours} olduğunu doğrula!

SADECE JSON yanıt ver, başka bir şey YAZMA:
{{
  ""tasks"": [
    {{
      ""title"": ""Görev Adı"",
      ""description"": ""Detaylı açıklama"",
      ""estimatedHours"": {hoursPerTask},
      ""priority"": ""high"",
      ""deadlineDays"": 1,
      ""dependsOnIndex"": null{codeJsonField}
    }}
  ]
}}";

            var textContent = await CallWithProvider(systemPrompt, $"PROJE TANIMI:\n{projectDescription}", provider, apiKey, model);

            // Markdown temizleme
            var cleanJson = textContent.Trim();
            if (cleanJson.StartsWith("```json")) cleanJson = cleanJson[7..];
            if (cleanJson.StartsWith("```")) cleanJson = cleanJson[3..];
            if (cleanJson.EndsWith("```")) cleanJson = cleanJson[..^3];
            cleanJson = cleanJson.Trim();

            var result = JsonSerializer.Deserialize<AiBreakdownResult>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Tasks == null || result.Tasks.Count == 0)
                throw new InvalidOperationException("Geçerli görev listesi üretilemedi.");

            // 🔒 Güvenlik: Saatleri ve deadline'ları kesinlikle sınırla
            int totalAssigned = 0;
            for (int i = 0; i < result.Tasks.Count; i++)
            {
                var task = result.Tasks[i];
                
                // Deadline sınırı
                if (task.DeadlineDays > totalDays) task.DeadlineDays = totalDays;
                if (task.DeadlineDays < 1) task.DeadlineDays = 1;

                // Tek görev günlük saati aşamaz
                if (task.EstimatedHours > dailyWorkHours) task.EstimatedHours = dailyWorkHours;
                if (task.EstimatedHours < 1) task.EstimatedHours = 1;

                totalAssigned += task.EstimatedHours;
            }

            // Toplam saati bütçeye sığdır — aşıyorsa son görevlerden kes
            if (totalAssigned > totalWorkHours)
            {
                for (int i = result.Tasks.Count - 1; i >= 0 && totalAssigned > totalWorkHours; i--)
                {
                    int excess = totalAssigned - totalWorkHours;
                    int canReduce = Math.Min(excess, result.Tasks[i].EstimatedHours - 1);
                    if (canReduce > 0)
                    {
                        result.Tasks[i].EstimatedHours -= canReduce;
                        totalAssigned -= canReduce;
                    }
                }
            }

            // Eğer deadline'lar düzenli dağıtılmadıysa zorla dağıt
            double interval = (double)totalDays / result.Tasks.Count;
            for (int i = 0; i < result.Tasks.Count; i++)
            {
                int expectedDeadline = Math.Max(1, (int)Math.Ceiling((i + 1) * interval));
                if (expectedDeadline > totalDays) expectedDeadline = totalDays;
                result.Tasks[i].DeadlineDays = expectedDeadline;
            }

            return (result.Tasks, cleanJson);
        }

        public async Task<string> AskDeveloperQuestionAsync(string question,
            string? provider = null, string? apiKey = null, string? model = null)
        {
            var systemPrompt = @"Sen deneyimli, çözüm odaklı, usta bir yazılım geliştiricisin.
Sana yazılım, hata ayıklama, algoritma veya teknoloji soruları sorulacak.
Basit Türkçe sorulara da doğal ve samimi cevap ver.
Gerekiyorsa Markdown ve kod örnekleri kullan. Türkçe yaz.";

            return await CallWithProvider(systemPrompt, $"KULLANICI SORUSU:\n{question}", provider, apiKey, model);
        }

        public async Task<string> GenerateAcademicHomeworkAsync(string topic, string university, string department, string extra,
            string? provider = null, string? apiKey = null, string? model = null)
        {
            var systemPrompt = @"Sen uzman bir akademik araştırmacı ve yazarsın.
Görevin kullanıcıdan gelen konuyla ilgili geniş, akıcı, profesyonel bir araştırma metni oluşturmaktır.

KURALLAR:
1. Başlıklar için (#, ##, ###) kullan.
2. Giriş, Gelişme ve Sonuç bölümleri oluştur.
3. Mümkünse alt başlıklar, maddeler ve detaylı paragraflar kullan (en az 4-5 sayfalık bilgi yoğunluğu ver).
4. Sadece içeriği yaz, üniversite logosu veya kapak tasarımı yapma (bu tasarım HTML üzerinde yapılacaktır).
5. Eğer 'Ek İstek' varsa onu mutlaka dikkate al.";

            var userPrompt = $"Üretilecek Belge Konusu: {topic}\n" +
                             $"Üniversite/Kurum: {university}\n" +
                             $"Bölüm/Fakülte: {department}\n" +
                             $"Ek İstekler: {extra}";

            return await CallWithProvider(systemPrompt, userPrompt, provider, apiKey, model);
        }
    }
}
