using PromocionBackend.Domain.Constants;
using PromocionBackend.Domain.Entities;
using PromocionBackend.Domain.Services;
using PromocionBackend.Domain.ValueObjects;

namespace PromocionBackend.Domain.Tests;

public class EligibilityEngineTests
{
    private static readonly DateOnly Today = new(2026, 7, 16);
    private static readonly DateOnly RankStart = new(2021, 9, 1);

    private static ProcessRequirement BaseConfig() => new()
    {
        FromPosition = PositionLadder.Auxiliar1,
        ToPosition = PositionLadder.Auxiliar2,
        MinYearsInPosition = 4,
        MinPublications = 1,
        MinEvaluationScorePct = 75,
        MinTrainingHours = 96,
        TrainingWindowYears = 3,
        MinPedagogicalTrainingPct = 25,
        RequiredLanguageLevel = "B1"
    };

    private static TeacherProfile EligibleProfile() => new()
    {
        CurrentPosition = PositionLadder.Auxiliar1,
        CurrentPositionStartDate = RankStart,
        ScorePercentage = 82.4m,
        Publications =
        [
            new PublicationRecord("PUB-1", "Artículo en el grado", new DateOnly(2023, 5, 10), "ES", "PUBLISHED")
        ],
        ReceivedTrainings =
        [
            new TrainingRecord("RT-1", "Pedagógica", "PEDAGOGICAL", new DateOnly(2024, 3, 1), 40),
            new TrainingRecord("RT-2", "Disciplinar", "DISCIPLINARY", new DateOnly(2025, 4, 15), 80)
        ],
        Languages =
        [
            new LanguageRecord("LAN-1", "EN", "B1", new DateOnly(2028, 6, 10))
        ]
    };

    [Fact]
    public void Evaluate_ProfileMeetsEverything_IsEligible()
    {
        var result = EligibilityEngine.Evaluate(EligibleProfile(), BaseConfig(), Today);

        Assert.True(result.IsEligible);
        Assert.All(result.Requirements, r => Assert.True(r.Met, $"{r.Code}: {r.Actual} vs {r.Required}"));
    }

    [Fact]
    public void Evaluate_InsufficientYears_FailsYearsRequirement()
    {
        var profile = EligibleProfile();
        var recentProfile = new TeacherProfile
        {
            CurrentPosition = profile.CurrentPosition,
            CurrentPositionStartDate = Today.AddYears(-3),
            ScorePercentage = profile.ScorePercentage,
            Publications = profile.Publications,
            ReceivedTrainings = profile.ReceivedTrainings,
            Languages = profile.Languages
        };

        var result = EligibilityEngine.Evaluate(recentProfile, BaseConfig(), Today);

        Assert.False(result.IsEligible);
        Assert.False(result.Requirements.Single(r => r.Code == "YEARS_IN_RANK").Met);
    }

    [Fact]
    public void Evaluate_PublicationsBeforeRankStart_DoNotCount()
    {
        var profile = new TeacherProfile
        {
            CurrentPosition = PositionLadder.Auxiliar1,
            CurrentPositionStartDate = RankStart,
            ScorePercentage = 90,
            Publications =
            [
                // Anterior al grado actual: no cuenta.
                new PublicationRecord("PUB-OLD", "Antigua", new DateOnly(2020, 1, 1), "ES", "PUBLISHED"),
                // No publicada: no cuenta.
                new PublicationRecord("PUB-DRAFT", "Borrador", new DateOnly(2024, 1, 1), "ES", "IN_REVIEW")
            ],
            ReceivedTrainings = EligibleProfile().ReceivedTrainings,
            Languages = EligibleProfile().Languages
        };

        var result = EligibilityEngine.Evaluate(profile, BaseConfig(), Today);

        var publications = result.Requirements.Single(r => r.Code == "PUBLICATIONS");
        Assert.False(publications.Met);
        Assert.StartsWith("0", publications.Actual);
    }

    [Fact]
    public void Evaluate_PublicationsInOtherLanguage_CountsOnlyNonSpanish()
    {
        var config = BaseConfig();
        config.MinPublications = 2;
        config.MinPublicationsInOtherLanguage = 1;

        var profile = new TeacherProfile
        {
            CurrentPosition = PositionLadder.Auxiliar1,
            CurrentPositionStartDate = RankStart,
            ScorePercentage = 90,
            Publications =
            [
                new PublicationRecord("PUB-ES", "En español", new DateOnly(2023, 1, 1), "ES", "PUBLISHED"),
                new PublicationRecord("PUB-EN", "In English", new DateOnly(2024, 1, 1), "EN", "PUBLISHED")
            ],
            ReceivedTrainings = EligibleProfile().ReceivedTrainings,
            Languages = EligibleProfile().Languages
        };

        var result = EligibilityEngine.Evaluate(profile, config, Today);

        Assert.True(result.Requirements.Single(r => r.Code == "PUBLICATIONS_OTHER_LANGUAGE").Met);
    }

    [Fact]
    public void Evaluate_TrainingOutsideWindow_DoesNotCount()
    {
        var profile = new TeacherProfile
        {
            CurrentPosition = PositionLadder.Auxiliar1,
            CurrentPositionStartDate = RankStart,
            ScorePercentage = 90,
            Publications = EligibleProfile().Publications,
            ReceivedTrainings =
            [
                // Terminó hace más de 3 años: fuera de la ventana.
                new TrainingRecord("RT-OLD", "Vieja", "DISCIPLINARY", Today.AddYears(-4), 200)
            ],
            Languages = EligibleProfile().Languages
        };

        var result = EligibilityEngine.Evaluate(profile, BaseConfig(), Today);

        Assert.False(result.Requirements.Single(r => r.Code == "TRAINING_HOURS").Met);
    }

    [Fact]
    public void Evaluate_PedagogicalHours_ComparedAgainstPercentageOfRequiredHours()
    {
        // 25 % de 96 horas exigidas = 24 horas pedagógicas mínimas.
        var config = BaseConfig();

        var profile = new TeacherProfile
        {
            CurrentPosition = PositionLadder.Auxiliar1,
            CurrentPositionStartDate = RankStart,
            ScorePercentage = 90,
            Publications = EligibleProfile().Publications,
            ReceivedTrainings =
            [
                new TrainingRecord("RT-P", "Pedagógica", "PEDAGOGICAL", new DateOnly(2025, 1, 1), 23),
                new TrainingRecord("RT-D", "Disciplinar", "DISCIPLINARY", new DateOnly(2025, 2, 1), 100)
            ],
            Languages = EligibleProfile().Languages
        };

        var result = EligibilityEngine.Evaluate(profile, config, Today);

        Assert.False(result.Requirements.Single(r => r.Code == "PEDAGOGICAL_HOURS").Met);

        profile = new TeacherProfile
        {
            CurrentPosition = profile.CurrentPosition,
            CurrentPositionStartDate = profile.CurrentPositionStartDate,
            ScorePercentage = profile.ScorePercentage,
            Publications = profile.Publications,
            ReceivedTrainings =
            [
                new TrainingRecord("RT-P", "Pedagógica", "PEDAGOGICAL", new DateOnly(2025, 1, 1), 24),
                new TrainingRecord("RT-D", "Disciplinar", "DISCIPLINARY", new DateOnly(2025, 2, 1), 100)
            ],
            Languages = profile.Languages
        };

        result = EligibilityEngine.Evaluate(profile, config, Today);

        Assert.True(result.Requirements.Single(r => r.Code == "PEDAGOGICAL_HOURS").Met);
    }

    [Fact]
    public void Evaluate_ProjectMonths_AppliesCoordinatorMultipliers()
    {
        var config = BaseConfig();
        config.MinProjectMonths = 24;
        config.ApplyRoleMultipliers = true;
        config.ProjectRoleScope = ProjectRoleScopes.Any;

        // 12 meses reales como COORDINATOR (principal) x2 = 24 meses efectivos.
        var profile = new TeacherProfile
        {
            CurrentPosition = PositionLadder.Auxiliar1,
            CurrentPositionStartDate = RankStart,
            ScorePercentage = 90,
            Publications = EligibleProfile().Publications,
            ReceivedTrainings = EligibleProfile().ReceivedTrainings,
            Languages = EligibleProfile().Languages,
            ResearchProjects =
            [
                new ProjectRecord("RES-1", "Proyecto", new DateOnly(2023, 1, 1), new DateOnly(2024, 1, 1), "COORDINATOR", "EC")
            ]
        };

        var result = EligibilityEngine.Evaluate(profile, config, Today);

        Assert.True(result.Requirements.Single(r => r.Code == "PROJECT_MONTHS").Met);
    }

    [Fact]
    public void Evaluate_ProjectMonths_WithoutMultiplier_SameProjectFails()
    {
        var config = BaseConfig();
        config.MinProjectMonths = 24;
        config.ApplyRoleMultipliers = false;

        var profile = new TeacherProfile
        {
            CurrentPosition = PositionLadder.Auxiliar1,
            CurrentPositionStartDate = RankStart,
            ScorePercentage = 90,
            Publications = EligibleProfile().Publications,
            ReceivedTrainings = EligibleProfile().ReceivedTrainings,
            Languages = EligibleProfile().Languages,
            ResearchProjects =
            [
                new ProjectRecord("RES-1", "Proyecto", new DateOnly(2023, 1, 1), new DateOnly(2024, 1, 1), "COORDINATOR", "EC")
            ]
        };

        var result = EligibilityEngine.Evaluate(profile, config, Today);

        Assert.False(result.Requirements.Single(r => r.Code == "PROJECT_MONTHS").Met);
    }

    [Fact]
    public void Evaluate_DirectionScope_ExcludesResearcherRole()
    {
        var config = BaseConfig();
        config.MinProjectMonths = 12;
        config.ProjectRoleScope = ProjectRoleScopes.Direction;

        var profile = new TeacherProfile
        {
            CurrentPosition = PositionLadder.Auxiliar1,
            CurrentPositionStartDate = RankStart,
            ScorePercentage = 90,
            Publications = EligibleProfile().Publications,
            ReceivedTrainings = EligibleProfile().ReceivedTrainings,
            Languages = EligibleProfile().Languages,
            ResearchProjects =
            [
                // Participación sin dirección: no cuenta en alcance "direction".
                new ProjectRecord("RES-1", "Proyecto", new DateOnly(2023, 1, 1), new DateOnly(2025, 1, 1), "RESEARCHER", "EC")
            ]
        };

        var result = EligibilityEngine.Evaluate(profile, config, Today);

        Assert.False(result.Requirements.Single(r => r.Code == "PROJECT_MONTHS").Met);
    }

    [Fact]
    public void Evaluate_ProjectOverlap_OnlyCountsTimeWithinRank()
    {
        var config = BaseConfig();
        config.MinProjectMonths = 24;

        // Proyecto de 4 años, pero solo ~12 meses caen dentro del grado actual.
        var profile = new TeacherProfile
        {
            CurrentPosition = PositionLadder.Auxiliar1,
            CurrentPositionStartDate = new DateOnly(2025, 7, 1),
            ScorePercentage = 90,
            Publications = EligibleProfile().Publications,
            ReceivedTrainings = EligibleProfile().ReceivedTrainings,
            Languages = EligibleProfile().Languages,
            ResearchProjects =
            [
                new ProjectRecord("RES-1", "Proyecto largo", new DateOnly(2022, 1, 1), new DateOnly(2026, 7, 1), "RESEARCHER", "EC")
            ]
        };

        var result = EligibilityEngine.Evaluate(profile, config, Today);

        Assert.False(result.Requirements.Single(r => r.Code == "PROJECT_MONTHS").Met);
    }

    [Fact]
    public void Evaluate_InternationalProjects_CountsNonEcuadorianCountry()
    {
        var config = BaseConfig();
        config.MinProjectMonths = 6;
        config.MinInternationalProjects = 1;

        var profile = new TeacherProfile
        {
            CurrentPosition = PositionLadder.Auxiliar1,
            CurrentPositionStartDate = RankStart,
            ScorePercentage = 90,
            Publications = EligibleProfile().Publications,
            ReceivedTrainings = EligibleProfile().ReceivedTrainings,
            Languages = EligibleProfile().Languages,
            ResearchProjects =
            [
                new ProjectRecord("RES-EC", "Nacional", new DateOnly(2023, 1, 1), new DateOnly(2024, 1, 1), "DIRECTOR", "EC"),
                new ProjectRecord("RES-US", "Internacional", new DateOnly(2024, 1, 1), new DateOnly(2025, 1, 1), "DIRECTOR", "US")
            ]
        };

        var result = EligibilityEngine.Evaluate(profile, config, Today);

        Assert.True(result.Requirements.Single(r => r.Code == "INTERNATIONAL_PROJECTS").Met);
    }

    [Fact]
    public void Evaluate_DoctoralTheses_InRankCountsByApprovalDate()
    {
        var config = BaseConfig();
        config.MinDoctoralTheses = 2;
        config.MinDoctoralThesesInRank = 1;

        var profile = new TeacherProfile
        {
            CurrentPosition = PositionLadder.Auxiliar1,
            CurrentPositionStartDate = RankStart,
            ScorePercentage = 90,
            Publications = EligibleProfile().Publications,
            ReceivedTrainings = EligibleProfile().ReceivedTrainings,
            Languages = EligibleProfile().Languages,
            DoctoralTheses =
            [
                new ThesisRecord("T-1", "Antes del grado", new DateOnly(2020, 5, 20), "DIRECTOR"),
                new ThesisRecord("T-2", "Durante el grado", new DateOnly(2024, 3, 15), "CO_DIRECTOR"),
                // Rol de tribunal: no cuenta como dirección.
                new ThesisRecord("T-3", "Como tribunal", new DateOnly(2024, 6, 1), "COMMITTEE_MEMBER")
            ]
        };

        var result = EligibilityEngine.Evaluate(profile, config, Today);

        Assert.True(result.Requirements.Single(r => r.Code == "DOCTORAL_THESES").Met);
        Assert.True(result.Requirements.Single(r => r.Code == "THESES_IN_RANK").Met);
        Assert.Equal("2 tesis", result.Requirements.Single(r => r.Code == "DOCTORAL_THESES").Actual);
    }

    [Theory]
    [InlineData("B1", true)]
    [InlineData("B2", true)]
    [InlineData("A2", false)]
    public void Evaluate_LanguageLevel_UsesCefrOrdering(string certifiedLevel, bool expectedMet)
    {
        var profile = new TeacherProfile
        {
            CurrentPosition = PositionLadder.Auxiliar1,
            CurrentPositionStartDate = RankStart,
            ScorePercentage = 90,
            Publications = EligibleProfile().Publications,
            ReceivedTrainings = EligibleProfile().ReceivedTrainings,
            Languages = [new LanguageRecord("LAN-1", "EN", certifiedLevel, new DateOnly(2028, 1, 1))]
        };

        var result = EligibilityEngine.Evaluate(profile, BaseConfig(), Today);

        Assert.Equal(expectedMet, result.Requirements.Single(r => r.Code == "LANGUAGE_LEVEL").Met);
    }

    [Fact]
    public void Evaluate_ExpiredLanguageCertification_DoesNotCount()
    {
        var profile = new TeacherProfile
        {
            CurrentPosition = PositionLadder.Auxiliar1,
            CurrentPositionStartDate = RankStart,
            ScorePercentage = 90,
            Publications = EligibleProfile().Publications,
            ReceivedTrainings = EligibleProfile().ReceivedTrainings,
            Languages = [new LanguageRecord("LAN-1", "EN", "C2", Today.AddDays(-1))]
        };

        var result = EligibilityEngine.Evaluate(profile, BaseConfig(), Today);

        Assert.False(result.Requirements.Single(r => r.Code == "LANGUAGE_LEVEL").Met);
    }

    [Fact]
    public void Evaluate_NoLanguageRequirement_OmitsLanguageEvaluation()
    {
        var config = BaseConfig();
        config.RequiredLanguageLevel = null;

        var result = EligibilityEngine.Evaluate(EligibleProfile(), config, Today);

        Assert.DoesNotContain(result.Requirements, r => r.Code == "LANGUAGE_LEVEL");
    }
}
