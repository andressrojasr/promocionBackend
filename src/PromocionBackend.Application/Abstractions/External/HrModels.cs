namespace PromocionBackend.Application.Abstractions.External;

/// <summary>
/// Contrato de datos del sistema de RRHH de la UTA (servicios simulados).
/// Las fechas viajan como cadenas "yyyy-MM-dd"; el snapshot se persiste con esta misma forma.
/// </summary>
public class HrTeacherDetails
{
    public string TeacherId { get; set; } = string.Empty;
    public string IdentificationType { get; set; } = string.Empty;
    public string Identification { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Orcid { get; set; } = string.Empty;
    public HrDependency Dependency { get; set; } = new();
    public string EmploymentRelationship { get; set; } = string.Empty;
    public string CurrentPosition { get; set; } = string.Empty;
    public string CurrentPositionStartDate { get; set; } = string.Empty;
    public string EvaluationDate { get; set; } = string.Empty;
    public List<HrExperience> Experience { get; set; } = [];
    public List<HrPublication> Publications { get; set; } = [];
    public List<HrTraining> ReceivedTrainings { get; set; } = [];
    public List<HrTraining> GivenTrainings { get; set; } = [];
    public List<HrResearchProject> ResearchProjects { get; set; } = [];
    public List<HrDoctoralThesis> DoctoralTheses { get; set; } = [];
    public List<HrLanguageCertification> Languages { get; set; } = [];
    public HrPerformanceScore? Score { get; set; }
}

public class HrDependency
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class HrExperience
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int Years { get; set; }
    public int Months { get; set; }
    public string KnowledgeArea { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string SupportingDocumentUrl { get; set; } = string.Empty;
}

public class HrPublication
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Journal { get; set; } = string.Empty;
    public string KnowledgeArea { get; set; } = string.Empty;
    public string PublicationDate { get; set; } = string.Empty;
    public string Doi { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string IndexingDatabase { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string SupportingDocumentUrl { get; set; } = string.Empty;
}

public class HrTraining
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string TrainingCategory { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int Hours { get; set; }
    public string KnowledgeArea { get; set; } = string.Empty;
    public string Modality { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string SupportingDocumentUrl { get; set; } = string.Empty;
}

public class HrResearchProject
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int Months { get; set; }
    public string Role { get; set; } = string.Empty;
    public string KnowledgeArea { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string SupportingDocumentUrl { get; set; } = string.Empty;
}

public class HrDoctoralThesis
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string ApprovalDate { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string KnowledgeArea { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string SupportingDocumentUrl { get; set; } = string.Empty;
}

public class HrLanguageCertification
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string ReferenceFramework { get; set; } = string.Empty;
    public string CertifyingInstitution { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string IssueDate { get; set; } = string.Empty;
    public string ExpirationDate { get; set; } = string.Empty;
    public string SupportingDocumentUrl { get; set; } = string.Empty;
}

public class HrPerformanceScore
{
    public string Type { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public string SupportingDocumentUrl { get; set; } = string.Empty;
}
