using System.Text;
using System.Text.Json;

namespace GoldBranchAI.Services
{
    public class TelegramService
    {
        private readonly HttpClient _httpClient;
        private readonly string _botToken = "8952079382:AAGyoifTYN0hCmATdxOWEcFvStZhT0iRw1c"; 

        public TelegramService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> SendMessageAsync(string chatId, string message)
        {
            if (string.IsNullOrEmpty(chatId) || _botToken == "YOUR_BOT_TOKEN_HERE") return false;

            try
            {
                var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
                var payload = new
                {
                    chat_id = chatId,
                    text = message,
                    parse_mode = "HTML"
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
