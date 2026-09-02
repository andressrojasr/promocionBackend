using Microsoft.AspNetCore.SignalR;
using PromocionBackend.Api.Hubs;
using PromocionBackend.Application.DTOs.Applications;

namespace PromocionBackend.Api.Services;

public class ApplicationNotificationService(IHubContext<ApplicationsHub> hubContext)
{
    public async Task NotifyApplicationUpdatedAsync(ApplicationSummaryDto application)
    {
        await hubContext.Clients
            .Group("ApplicationUpdates")
            .SendAsync("ApplicationUpdated", application);
    }

    public async Task NotifyApplicationStatusChangedAsync(Guid applicationId, string newStatus)
    {
        await hubContext.Clients
            .Group("ApplicationUpdates")
            .SendAsync("ApplicationStatusChanged", new { applicationId, newStatus });
    }
}
