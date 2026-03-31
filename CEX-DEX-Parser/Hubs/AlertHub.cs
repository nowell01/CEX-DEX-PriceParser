using Microsoft.AspNetCore.SignalR;

namespace CEX_DEX_Parser.Hubs
{
    public class AlertHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }
    }
}
