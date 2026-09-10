namespace PromocionBackend.Infrastructure.Configuration;

public class DataSourceSettings
{
    public const string SectionName = "DataSource";

    public bool UseRealServices { get; set; } = false;
    public string StaticCedula { get; set; } = "MOCKFULL01";
    public string RealServicesBaseUrl { get; set; } = "https://serviciospruebas.uta.edu.ec";
}
