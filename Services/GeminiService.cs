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
    }

    public class AiBreakdownResult
    {
        [JsonPropertyName("tasks")]
        public List<AiSubTask> Tasks { get; set; } = new();
    }

    /// <summary>
    /// AI Servisi: Birincil Cohere, Yedek SambaNova
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

        // ==================== COHERE (Birincil) ====================
        private async Task<string?> CallCohere(string systemPrompt, string userContent)
        {
            var apiKey = _configuration["CohereApi:ApiKey"];
            var model = _configuration["CohereApi:Model"] ?? "command-a-03-2025";
            if (string.IsNullOrEmpty(apiKey)) return null;

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
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

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

        // ==================== SAMBANOVA (Yedek) ====================
        private async Task<string?> CallSambaNova(string systemPrompt, string userContent)
        {
            var apiKey = _configuration["SambaNovaApi:ApiKey"];
            var model = _configuration["SambaNovaApi:Model"] ?? "Meta-Llama-3.3-70B-Instruct";
            if (string.IsNullOrEmpty(apiKey)) return null;

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
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

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

        // ==================== ANA CAGRI (Fallback Mantigi) ====================
        private async Task<string> CallWithFallback(string systemPrompt, string userContent)
        {
            // 1. Cohere (birincil)
            _logger.LogInformation("AI istek baslatiliyor: Cohere (birincil)");
            var result = await CallCohere(systemPrompt, userContent);
            if (!string.IsNullOrEmpty(result))
            {
                _logger.LogInformation("Cohere basarili yanit verdi.");
                return result;
            }

            // 2. SambaNova (yedek)
            _logger.LogInformation("Cohere basarisiz, SambaNova deneniyor...");
            result = await CallSambaNova(systemPrompt, userContent);
            if (!string.IsNullOrEmpty(result))
            {
                _logger.LogInformation("SambaNova basarili yanit verdi.");
                return result;
            }

            throw new HttpRequestException("Her iki AI servisi de su an yanit veremiyor. Lutfen birkaC dakika sonra tekrar deneyin.");
        }

        // ==================== PUBLIC METODLAR ====================

        public async Task<(List<AiSubTask> Tasks, string RawJson)> BreakdownProjectAsync(string projectDescription)
        {
            var systemPrompt = @"Sen ust duzey bir teknik proje yoneticisisin. Sana verilen proje tanimini analiz et ve onu yazilim gelistirme alt gorevlerine bol. 

ONEMLI KURALLAR:
1. Projenin genel buyuklugunu ve zorlugunu hayal et.
2. Gorevler mantiksal siraya gore dizilmeli.
3. 'estimatedHours': 1-16 arasi.
4. 'deadlineDays': Bugunden itibaren kac gun.
5. Oncelik: ""high"", ""medium"", ""low"".
6. En az 3, en fazla 15 gorev uret.

SADECE JSON formatinda yanit ver:
{
  ""tasks"": [
    {
      ""title"": ""Ornek Gorev"",
      ""description"": ""Detayli aciklama"",
      ""estimatedHours"": 6,
      ""priority"": ""high"",
      ""deadlineDays"": 3
    }
  ]
}";

            var textContent = await CallWithFallback(systemPrompt, $"PROJE TANIMI:\n{projectDescription}");

            // Markdown temizleme
            var cleanJson = textContent.Trim();
            if (cleanJson.StartsWith("```json")) cleanJson = cleanJson[7..];
            if (cleanJson.StartsWith("```")) cleanJson = cleanJson[3..];
            if (cleanJson.EndsWith("```")) cleanJson = cleanJson[..^3];
            cleanJson = cleanJson.Trim();

            var result = JsonSerializer.Deserialize<AiBreakdownResult>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Tasks == null || result.Tasks.Count == 0)
                throw new InvalidOperationException("Gecerli gorev listesi uretilemedi.");

            return (result.Tasks, cleanJson);
        }

        public async Task<string> AskDeveloperQuestionAsync(string question)
        {
            var systemPrompt = @"Sen deneyimli, cozum odakli, usta bir yazilim gelisiticisisin.
Sana yazilim, hata ayiklama, algoritma veya teknoloji sorulari sorulacak.
Basit Turkce sorulara da dogal ve samimi cevap ver.
Gerekiyorsa Markdown ve kod ornekleri kullan. Turkce yaz.";

            return await CallWithFallback(systemPrompt, $"KULLANICI SORUSU:\n{question}");
        }

        public async Task<string> GenerateAcademicHomeworkAsync(string topic, string university, string department, string extra)
        {
            var systemPrompt = @"Sen uzman bir akademik arastirmaci ve yazarsin.
Gorevin kullanicidan gelen konuyla ilgili genis, akici, profesyonel bir arastirma metni olusturmaktir.

KURALLAR:
1. Basliklar icin (#, ##, ###) kullan.
2. Giris, Gelisme ve Sonuc bolumleri olustur.
3. Mumkunse alt basliklar, maddeler ve detayli paragraflar kullan (en az 4-5 sayfalik bilgi yoğunlugu ver).
4. Sadece icerigi yaz, universite logosu veya kapak tasarimi yapma (bu tasarim HTML uzerinde yapilacaktir).
5. Eger 'Ek Istek' varsa onu mutlaka dikkate al.";

            var userPrompt = $"Uretilecek Belge Konusu: {topic}\n" +
                             $"Universite/Kurum: {university}\n" +
                             $"Bolum/Fakulte: {department}\n" +
                             $"Ek Istekler: {extra}";

            return await CallWithFallback(systemPrompt, userPrompt);
        }
    }
}
