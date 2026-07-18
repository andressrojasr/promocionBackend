using System.Globalization;
using PromocionBackend.Domain.Constants;
using PromocionBackend.Domain.Entities;
using PromocionBackend.Domain.ValueObjects;

namespace PromocionBackend.Domain.Services;

/// <summary>
/// Motor de elegibilidad: evalúa la hoja de vida de un docente contra la
/// configuración de requisitos de su transición de escalafón. Servicio puro,
/// sin dependencias de infraestructura, para facilitar las pruebas unitarias.
/// </summary>
public static class EligibilityEngine
{
    private const double DaysPerYear = 365.25;
    private const double DaysPerMonth = 30.44;
    private const string SpanishLanguageCode = "ES";
    private const string PublishedStatus = "PUBLISHED";
    private const string PedagogicalCategory = "PEDAGOGICAL";

    /// <summary>Roles de proyecto que cuentan como dirección o codirección.</summary>
    private static readonly HashSet<string> DirectionRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "DIRECTOR", "CO_DIRECTOR", "COORDINATOR", "COORDINATOR_SUBROGANTE"
    };

    /// <summary>Roles con multiplicador x2 (coordinador/director principal).</summary>
    private static readonly HashSet<string> PrincipalCoordinatorRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "DIRECTOR", "COORDINATOR"
    };

    /// <summary>Roles con multiplicador x1.5 (coordinador subrogante / codirector).</summary>
    private static readonly HashSet<string> SubroganteCoordinatorRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "CO_DIRECTOR", "COORDINATOR_SUBROGANTE"
    };

    private static readonly HashSet<string> ThesisDirectionRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "DIRECTOR", "CO_DIRECTOR"
    };

    /// <summary>Orden de niveles del Marco Común Europeo de Referencia.</summary>
    private static readonly Dictionary<string, int> CefrOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["A1"] = 1, ["A2"] = 2, ["B1"] = 3, ["B2"] = 4, ["C1"] = 5, ["C2"] = 6
    };

    public static EligibilityResult Evaluate(TeacherProfile profile, ProcessRequirement config, DateOnly today)
    {
        var evaluations = new List<RequirementEvaluation>();
        var rankStart = profile.CurrentPositionStartDate;

        evaluations.Add(EvaluateYearsInRank(rankStart, config, today));
        evaluations.Add(EvaluatePublications(profile, config, rankStart));

        if (config.MinPublicationsInOtherLanguage > 0)
        {
            evaluations.Add(EvaluatePublicationsInOtherLanguage(profile, config, rankStart));
        }

        evaluations.Add(EvaluateScore(profile, config));
        evaluations.Add(EvaluateTrainingHours(profile, config, today));

        if (config.MinPedagogicalTrainingPct is { } pedagogicalPct)
        {
            evaluations.Add(EvaluatePedagogicalHours(profile, config, pedagogicalPct, today));
        }

        if (config.MinGivenTrainingHours is { } minGivenHours)
        {
            evaluations.Add(EvaluateGivenTrainingHours(profile, minGivenHours));
        }

        if (config.MinProjectMonths is { } minProjectMonths)
        {
            evaluations.Add(EvaluateProjectMonths(profile, config, minProjectMonths, rankStart, today));
        }

        if (config.MinInternationalProjects is { } minInternational)
        {
            evaluations.Add(EvaluateInternationalProjects(profile, config, minInternational, rankStart, today));
        }

        if (config.MinDoctoralTheses is { } minTheses)
        {
            evaluations.Add(EvaluateDoctoralTheses(profile, minTheses));
        }

        if (config.MinDoctoralThesesInRank is { } minThesesInRank)
        {
            evaluations.Add(EvaluateDoctoralThesesInRank(profile, minThesesInRank, rankStart));
        }

        if (!string.IsNullOrWhiteSpace(config.RequiredLanguageLevel))
        {
            evaluations.Add(EvaluateLanguage(profile, config.RequiredLanguageLevel, today));
        }

        return new EligibilityResult
        {
            FromPosition = config.FromPosition,
            ToPosition = config.ToPosition,
            IsEligible = evaluations.All(e => e.Met),
            Requirements = evaluations
        };
    }

    private static RequirementEvaluation EvaluateYearsInRank(DateOnly rankStart, ProcessRequirement config, DateOnly today)
    {
        var years = (today.DayNumber - rankStart.DayNumber) / DaysPerYear;

        return new RequirementEvaluation(
            Code: "YEARS_IN_RANK",
            Label: $"Experiencia mínima como {PositionLadder.Label(config.FromPosition)}",
            Required: $"{config.MinYearsInPosition} años",
            Actual: $"{Format(years)} años",
            Met: years >= config.MinYearsInPosition,
            Detail: $"En el grado actual desde el {rankStart:yyyy-MM-dd}.");
    }

    private static RequirementEvaluation EvaluatePublications(TeacherProfile profile, ProcessRequirement config, DateOnly rankStart)
    {
        var count = PublicationsInRank(profile, rankStart).Count();

        return new RequirementEvaluation(
            Code: "PUBLICATIONS",
            Label: "Obras de relevancia o artículos indexados publicados durante el grado actual",
            Required: $"{config.MinPublications} publicaciones",
            Actual: $"{count} publicaciones",
            Met: count >= config.MinPublications,
            Detail: "Se cuentan publicaciones en estado PUBLISHED con fecha posterior al inicio del grado actual.");
    }

    private static RequirementEvaluation EvaluatePublicationsInOtherLanguage(TeacherProfile profile, ProcessRequirement config, DateOnly rankStart)
    {
        var count = PublicationsInRank(profile, rankStart)
            .Count(p => !string.Equals(p.Language, SpanishLanguageCode, StringComparison.OrdinalIgnoreCase));

        return new RequirementEvaluation(
            Code: "PUBLICATIONS_OTHER_LANGUAGE",
            Label: "Publicaciones en un idioma diferente a la lengua materna",
            Required: $"{config.MinPublicationsInOtherLanguage} publicaciones",
            Actual: $"{count} publicaciones",
            Met: count >= config.MinPublicationsInOtherLanguage,
            Detail: "Del total de publicaciones del grado actual, las realizadas en un idioma distinto al castellano.");
    }

    private static RequirementEvaluation EvaluateScore(TeacherProfile profile, ProcessRequirement config)
    {
        var score = profile.ScorePercentage ?? 0m;

        return new RequirementEvaluation(
            Code: "EVALUATION_SCORE",
            Label: "Puntaje mínimo de la evaluación integral de desempeño",
            Required: $"{Format(config.MinEvaluationScorePct)} %",
            Actual: $"{Format(score)} %",
            Met: score >= config.MinEvaluationScorePct,
            Detail: "Promedio de la evaluación integral en los últimos periodos académicos registrados.");
    }

    private static RequirementEvaluation EvaluateTrainingHours(TeacherProfile profile, ProcessRequirement config, DateOnly today)
    {
        var hours = TrainingsInWindow(profile, config.TrainingWindowYears, today).Sum(t => t.Hours);

        return new RequirementEvaluation(
            Code: "TRAINING_HOURS",
            Label: $"Horas de capacitación y actualización en los últimos {config.TrainingWindowYears} años",
            Required: $"{config.MinTrainingHours} horas",
            Actual: $"{hours} horas",
            Met: hours >= config.MinTrainingHours,
            Detail: $"Capacitaciones recibidas con fecha de finalización dentro de los últimos {config.TrainingWindowYears} años.");
    }

    private static RequirementEvaluation EvaluatePedagogicalHours(TeacherProfile profile, ProcessRequirement config, decimal pedagogicalPct, DateOnly today)
    {
        var requiredHours = Math.Ceiling(config.MinTrainingHours * pedagogicalPct / 100m);
        var pedagogicalHours = TrainingsInWindow(profile, config.TrainingWindowYears, today)
            .Where(t => string.Equals(t.Category, PedagogicalCategory, StringComparison.OrdinalIgnoreCase))
            .Sum(t => t.Hours);

        return new RequirementEvaluation(
            Code: "PEDAGOGICAL_HOURS",
            Label: "Horas de actualización pedagógica",
            Required: $"{Format(requiredHours)} horas ({Format(pedagogicalPct)} % de las horas exigidas)",
            Actual: $"{pedagogicalHours} horas",
            Met: pedagogicalHours >= requiredHours,
            Detail: "Horas de capacitación de categoría pedagógica dentro de la ventana de capacitación.");
    }

    private static RequirementEvaluation EvaluateGivenTrainingHours(TeacherProfile profile, int minGivenHours)
    {
        var hours = profile.GivenTrainings.Sum(t => t.Hours);

        return new RequirementEvaluation(
            Code: "GIVEN_TRAINING_HOURS",
            Label: "Horas de capacitación y actualización impartida",
            Required: $"{minGivenHours} horas",
            Actual: $"{hours} horas",
            Met: hours >= minGivenHours,
            Detail: "Total de horas de capacitación profesional y/o pedagógica impartida.");
    }

    private static RequirementEvaluation EvaluateProjectMonths(TeacherProfile profile, ProcessRequirement config, int minProjectMonths, DateOnly rankStart, DateOnly today)
    {
        var totalMonths = QualifyingProjects(profile, config, rankStart, today)
            .Sum(p => p.EffectiveMonths);

        var scopeText = config.ProjectRoleScope == ProjectRoleScopes.Direction
            ? "Se cuenta únicamente la dirección o codirección de proyectos"
            : "Se cuenta la participación en cualquier rol del proyecto";
        var multiplierText = config.ApplyRoleMultipliers
            ? " El tiempo como coordinador principal vale el doble y como subrogante 1.5 veces."
            : string.Empty;

        return new RequirementEvaluation(
            Code: "PROJECT_MONTHS",
            Label: "Tiempo en proyectos de investigación y/o vinculación durante el grado actual",
            Required: $"{minProjectMonths} meses",
            Actual: $"{Format(totalMonths)} meses",
            Met: totalMonths >= minProjectMonths,
            Detail: $"{scopeText}, considerando solo el tiempo dentro del grado actual.{multiplierText}");
    }

    private static RequirementEvaluation EvaluateInternationalProjects(TeacherProfile profile, ProcessRequirement config, int minInternational, DateOnly rankStart, DateOnly today)
    {
        var count = QualifyingProjects(profile, config, rankStart, today)
            .Count(p => !string.Equals(p.Project.Country, "EC", StringComparison.OrdinalIgnoreCase));

        return new RequirementEvaluation(
            Code: "INTERNATIONAL_PROJECTS",
            Label: "Proyectos con investigadores, instituciones o redes de investigación extranjeros",
            Required: $"{minInternational} proyectos",
            Actual: $"{count} proyectos",
            Met: count >= minInternational,
            Detail: "Proyectos del grado actual vinculados con contrapartes fuera del Ecuador.");
    }

    private static RequirementEvaluation EvaluateDoctoralTheses(TeacherProfile profile, int minTheses)
    {
        var count = DirectedTheses(profile).Count();

        return new RequirementEvaluation(
            Code: "DOCTORAL_THESES",
            Label: "Tesis de doctorado dirigidas o codirigidas",
            Required: $"{minTheses} tesis",
            Actual: $"{count} tesis",
            Met: count >= minTheses,
            Detail: "Tesis doctorales en las que participó como director o codirector.");
    }

    private static RequirementEvaluation EvaluateDoctoralThesesInRank(TeacherProfile profile, int minThesesInRank, DateOnly rankStart)
    {
        var count = DirectedTheses(profile)
            .Count(t => t.ApprovalDate is { } approval && approval >= rankStart);

        return new RequirementEvaluation(
            Code: "THESES_IN_RANK",
            Label: "Tesis de doctorado dirigidas durante el grado actual",
            Required: $"{minThesesInRank} tesis",
            Actual: $"{count} tesis",
            Met: count >= minThesesInRank,
            Detail: "Tesis con fecha de aprobación posterior al inicio del grado actual.");
    }

    private static RequirementEvaluation EvaluateLanguage(TeacherProfile profile, string requiredLevel, DateOnly today)
    {
        var requiredOrder = CefrOrder.GetValueOrDefault(requiredLevel, int.MaxValue);

        var best = profile.Languages
            .Where(l => !string.Equals(l.Language, SpanishLanguageCode, StringComparison.OrdinalIgnoreCase))
            .Where(l => l.ExpirationDate is null || l.ExpirationDate >= today)
            .Select(l => new { Record = l, Order = CefrOrder.GetValueOrDefault(l.Level, 0) })
            .OrderByDescending(l => l.Order)
            .FirstOrDefault();

        var actual = best is null
            ? "Sin certificación vigente"
            : $"{best.Record.Language} {best.Record.Level}";

        return new RequirementEvaluation(
            Code: "LANGUAGE_LEVEL",
            Label: "Nivel de un idioma distinto al castellano debidamente certificado",
            Required: $"Nivel {requiredLevel} (MCER)",
            Actual: actual,
            Met: best is not null && best.Order >= requiredOrder,
            Detail: "Certificaciones vigentes bajo el Marco Común Europeo de Referencia.");
    }

    // ---------- Consultas auxiliares ----------

    private static IEnumerable<PublicationRecord> PublicationsInRank(TeacherProfile profile, DateOnly rankStart) =>
        profile.Publications.Where(p =>
            string.Equals(p.Status, PublishedStatus, StringComparison.OrdinalIgnoreCase) &&
            p.PublicationDate is { } date && date >= rankStart);

    private static IEnumerable<TrainingRecord> TrainingsInWindow(TeacherProfile profile, int windowYears, DateOnly today)
    {
        var cutoff = today.AddYears(-windowYears);
        return profile.ReceivedTrainings.Where(t => t.EndDate is { } end && end >= cutoff);
    }

    private static IEnumerable<ThesisRecord> DirectedTheses(TeacherProfile profile) =>
        profile.DoctoralTheses.Where(t => ThesisDirectionRoles.Contains(t.Role));

    private sealed record QualifyingProject(ProjectRecord Project, double EffectiveMonths);

    /// <summary>
    /// Proyectos que cuentan para los requisitos: filtrados por alcance de rol y con
    /// meses efectivos calculados como el solapamiento con el grado actual, aplicando
    /// el multiplicador reglamentario cuando corresponde.
    /// </summary>
    private static IEnumerable<QualifyingProject> QualifyingProjects(TeacherProfile profile, ProcessRequirement config, DateOnly rankStart, DateOnly today)
    {
        foreach (var project in profile.ResearchProjects)
        {
            if (config.ProjectRoleScope == ProjectRoleScopes.Direction && !DirectionRoles.Contains(project.Role))
            {
                continue;
            }

            var start = project.StartDate ?? rankStart;
            var end = project.EndDate ?? today;

            var overlapStart = start > rankStart ? start : rankStart;
            var overlapEnd = end < today ? end : today;

            if (overlapEnd <= overlapStart)
            {
                continue;
            }

            var months = MonthsBetween(overlapStart, overlapEnd);

            if (config.ApplyRoleMultipliers)
            {
                if (PrincipalCoordinatorRoles.Contains(project.Role))
                {
                    months *= 2.0;
                }
                else if (SubroganteCoordinatorRoles.Contains(project.Role))
                {
                    months *= 1.5;
                }
            }

            yield return new QualifyingProject(project, months);
        }
    }

    /// <summary>
    /// Meses calendario entre dos fechas, con fracción por días. Un año calendario
    /// completo cuenta exactamente como 12 meses.
    /// </summary>
    private static double MonthsBetween(DateOnly start, DateOnly end) =>
        (end.Year - start.Year) * 12 + (end.Month - start.Month) + (end.Day - start.Day) / DaysPerMonth;

    private static string Format(double value) =>
        Math.Round(value, 1).ToString("0.#", CultureInfo.InvariantCulture);

    private static string Format(decimal value) =>
        Math.Round(value, 1).ToString("0.#", CultureInfo.InvariantCulture);
}
