namespace PromocionBackend.Domain.Constants;

/// <summary>
/// Tipo de comisión: de Promoción (CP) o de Apelaciones (CA). Comparten la misma
/// estructura de 6 cargos, pero se registran y filtran por separado.
/// </summary>
public static class CommissionTypes
{
    public const string Cp = "cp";
    public const string Ca = "ca";

    public static readonly IReadOnlyList<string> All = [Cp, Ca];

    public static bool IsValid(string type) => All.Contains(type);

    /// <summary>¿Puede este rol administrar/operar comisiones o sesiones de revisión de este tipo?</summary>
    public static bool RoleMatchesType(string role, string type) => role switch
    {
        Roles.Cp => type == Cp,
        Roles.Ca => type == Ca,
        Roles.Admin => true,
        _ => false
    };
}

/// <summary>
/// Cargos por defecto de la comisión, en el orden en que aparecen en el acta oficial.
/// Se guardan como texto libre por miembro para poder ajustarlos sin migración.
/// </summary>
public static class CommissionCargos
{
    public static readonly IReadOnlyList<string> Defaults =
    [
        "Vicerrector/a Académico/a o su delegado/a, Presidente/a de la Comisión, con voto dirimente.",
        "Profesor/a designado/a por el Honorable Consejo Universitario de entre sus miembros.",
        "Profesor/a designado/a por el Honorable Consejo Universitario de la terna remitida por el Rector.",
        "Profesor/a designado/a por el Honorable Consejo Universitario de la terna remitida por la Asociación de Profesores de la Universidad Técnica de Ambato.",
        "Director/a de Talento Humano o su delegado/a, Secretario/a de la Comisión, con voz y voto.",
        "Director/a Financiero/a o su delegado/a, Asesor/a de la Comisión, con voz y sin voto."
    ];
}
