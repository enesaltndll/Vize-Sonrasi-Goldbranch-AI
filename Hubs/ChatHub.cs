using Microsoft.AspNetCore.SignalR;

namespace GoldBranchAI.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly Dictionary<string, string> _onlineUsers = new Dictionary<string, string>();

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                _onlineUsers[Context.ConnectionId] = userId;
                await Clients.All.SendAsync("UserStatusChanged", userId, true);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_onlineUsers.TryRemove(Context.ConnectionId, out var userId))
            {
                // Eğer kullanıcı başka bir sekmeden hala bağlı değilse offline yap
                if (!_onlineUsers.Values.Contains(userId))
                {
                    await Clients.All.SendAsync("UserStatusChanged", userId, false);
                }
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
