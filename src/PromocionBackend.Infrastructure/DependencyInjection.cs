using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Infrastructure.Auth;
using PromocionBackend.Infrastructure.ExternalServices;
using PromocionBackend.Infrastructure.Persistence;

namespace PromocionBackend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<RoleSeedOptions>(configuration.GetSection(RoleSeedOptions.SectionName));

        services.AddSingleton<IAppJwtIssuer, AppJwtIssuer>();
        services.AddSingleton<IExternalTokenReader, ExternalTokenReader>();

        services.AddHttpClient<IHrApiClient, HrApiClient>(client =>
        {
            var baseUrl = configuration["HrApi:BaseUrl"] ?? "http://localhost:5031";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}
