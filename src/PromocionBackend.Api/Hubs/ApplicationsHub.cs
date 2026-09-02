using Microsoft.AspNetCore.SignalR;

namespace PromocionBackend.Api.Hubs;

public class ApplicationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SubscribeToApplicationUpdates()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "ApplicationUpdates");
    }

    public async Task UnsubscribeFromApplicationUpdates()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "ApplicationUpdates");
    }
}
