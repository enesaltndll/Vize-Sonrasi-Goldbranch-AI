using System.Collections.Concurrent;
using System.Text.Json;

namespace GoldBranchAI.Services
{
    public class LocalizationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _env;
        private static readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new();

        public LocalizationService(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env)
        {
            _httpContextAccessor = httpContextAccessor;
            _env = env;
        }

        private string GetCurrentLang()
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx != null && ctx.Request.Cookies.TryGetValue("lang_pref", out var cookieLang))
            {
                if (cookieLang == "en") return "en";
            }
            return "tr";
        }

        private Dictionary<string, string>? LoadDictionary(string lang)
        {
            if (_cache.TryGetValue(lang, out var cached))
                return cached;

            string filePath = Path.Combine(_env.ContentRootPath, "Locales", $"{lang}.json");

            if (!File.Exists(filePath)) return null;

            try
            {
                string json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    _cache[lang] = dict;
                    return dict;
                }
            }
            catch { }

            return null;
        }

        public string Get(string key)
        {
            string lang = GetCurrentLang();
            var dict = LoadDictionary(lang);
            if (dict != null && dict.TryGetValue(key, out var val))
                return val;

            // Fallback to TR if EN key not found
            if (lang != "tr")
            {
                var trDict = LoadDictionary("tr");
                if (trDict != null && trDict.TryGetValue(key, out var trVal))
                    return trVal;
            }

            return key;
        }

        /// <summary>
        /// Reload cache (call after locale file changes)
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
        }
    }
}
