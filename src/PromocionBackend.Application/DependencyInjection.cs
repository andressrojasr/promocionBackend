using Microsoft.Extensions.DependencyInjection;
using PromocionBackend.Application.Services;

namespace PromocionBackend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<SessionService>();
        services.AddScoped<UserAdminService>();
        services.AddScoped<ProcessService>();
        services.AddScoped<EligibilityService>();
        services.AddScoped<ApplicationService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<TeacherProfileService>();
        services.AddScoped<DashboardService>();

        return services;
    }
}
