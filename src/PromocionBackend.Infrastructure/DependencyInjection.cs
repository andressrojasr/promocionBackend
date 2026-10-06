using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Infrastructure.Auth;
using PromocionBackend.Infrastructure.Configuration;
using PromocionBackend.Infrastructure.ExternalServices;
using PromocionBackend.Infrastructure.Pdf;
using PromocionBackend.Infrastructure.Persistence;
using QuestPDF.Infrastructure;

namespace PromocionBackend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        // El acta usa Times New Roman (como la plantilla); se toma de las fuentes del sistema.
        // Si no existe en el servidor, se degrada a otra fuente en lugar de fallar.
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

        services.AddSingleton<IActaPdfBuilder, ActaPdfBuilder>();

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<RoleSeedOptions>(configuration.GetSection(RoleSeedOptions.SectionName));
        services.Configure<DataSourceSettings>(configuration.GetSection(DataSourceSettings.SectionName));

        services.AddSingleton<IAppJwtIssuer, AppJwtIssuer>();
        services.AddSingleton<IExternalTokenReader, ExternalTokenReader>();

        services.AddHttpClient<IHrApiClient, HrApiClient>(client =>
        {
            var dataSourceSettings = configuration.GetSection(DataSourceSettings.SectionName).Get<DataSourceSettings>() ?? new DataSourceSettings();
            var baseUrl = dataSourceSettings.UseRealServices
                ? dataSourceSettings.RealServicesBaseUrl
                : (configuration["HrApi:BaseUrl"] ?? "http://localhost:5031");

            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}
