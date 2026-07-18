namespace PromocionBackend.Domain.Constants;

/// <summary>
/// Etapas de revisión de una postulación.
/// </summary>
public static class ReviewStages
{
    public const string Th = "th";
    public const string Cp = "cp";
    public const string Ca = "ca";
}

/// <summary>
/// Decisiones posibles en una revisión.
/// </summary>
public static class ReviewDecisions
{
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}
