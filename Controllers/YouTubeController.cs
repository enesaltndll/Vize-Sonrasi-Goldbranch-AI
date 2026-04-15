using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GoldBranchAI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class YouTubeController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public YouTubeController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpGet("Search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { error = "Arama kelimesi boş olamaz." });
            }

            var apiKey = _config["YouTube:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey == "BURAYA_GIRILECEK")
            {
                return StatusCode(500, new { error = "YouTube API Key tanımlanmamış. Lütfen appsettings.json dosyasını güncelleyin." });
            }

            try
            {
                // YouTube Data API v3 Search endpoint
                var url = $"https://www.googleapis.com/youtube/v3/search?part=snippet&q={Uri.EscapeDataString(query)}&type=video&maxResults=5&key={apiKey}";
                
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new { error = "YouTube API hatası: " + response.ReasonPhrase });
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var items = doc.RootElement.GetProperty("items");

                var results = new List<object>();
                foreach (var item in items.EnumerateArray())
                {
                    var id = item.GetProperty("id").GetProperty("videoId").GetString();
                    var snippet = item.GetProperty("snippet");
                    var title = snippet.GetProperty("title").GetString();
                    var channel = snippet.GetProperty("channelTitle").GetString();
                    var thumb = snippet.GetProperty("thumbnails").GetProperty("default").GetProperty("url").GetString();

                    results.Add(new
                    {
                        Id = id,
                        Name = title,
                        Artist = channel, // Kanal adını sanatçı yerine kullanıyoruz
                        ImageUrl = thumb
                    });
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
