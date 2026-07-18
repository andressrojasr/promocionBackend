namespace PromocionBackend.Domain.Constants;

/// <summary>
/// Tipos de ítems de evidencia que un docente puede adjuntar a su postulación.
/// Corresponden a las secciones de la hoja de vida entregada por el sistema de RRHH.
/// </summary>
public static class ApplicationItemTypes
{
    public const string Publication = "publication";
    public const string ReceivedTraining = "received_training";
    public const string GivenTraining = "given_training";
    public const string ResearchProject = "research_project";
    public const string DoctoralThesis = "doctoral_thesis";
    public const string Language = "language";
    public const string Experience = "experience";

    public static readonly IReadOnlyList<string> All =
        [Publication, ReceivedTraining, GivenTraining, ResearchProject, DoctoralThesis, Language, Experience];

    public static bool IsValid(string itemType) => All.Contains(itemType);
}

/// <summary>
/// Alcance de los roles que cuentan para el requisito de tiempo en proyectos.
/// </summary>
public static class ProjectRoleScopes
{
    /// <summary>Cuenta la participación en cualquier rol.</summary>
    public const string Any = "any";

    /// <summary>Cuenta solo la dirección o codirección de proyectos.</summary>
    public const string Direction = "direction";
}
