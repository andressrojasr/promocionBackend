using System.Text.Json.Serialization;

namespace PromocionBackend.Application.Abstractions.External;

/// <summary>
/// Contrato de datos del sistema de RRHH de la UTA (servicios simulados).
/// Las fechas viajan como cadenas "yyyy-MM-dd"; el snapshot se persiste con esta misma forma.
/// </summary>
public class HrTeacherDetails
{
    [JsonPropertyName("teacherId")]
    public string TeacherId { get; set; } = string.Empty;

    [JsonPropertyName("identificationType")]
    public string IdentificationType { get; set; } = string.Empty;

    [JsonPropertyName("identification")]
    public string Identification { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("orcid")]
    public string Orcid { get; set; } = string.Empty;

    [JsonPropertyName("dependency")]
    public HrDependency Dependency { get; set; } = new();

    [JsonPropertyName("employmentRelationship")]
    public string EmploymentRelationship { get; set; } = string.Empty;

    [JsonPropertyName("currentPosition")]
    public string CurrentPosition { get; set; } = string.Empty;

    [JsonPropertyName("currentPositionStartDate")]
    public string CurrentPositionStartDate { get; set; } = string.Empty;

    [JsonPropertyName("evaluationDate")]
    public string EvaluationDate { get; set; } = string.Empty;

    [JsonPropertyName("experience")]
    public List<HrExperience> Experience { get; set; } = [];

    [JsonPropertyName("publications")]
    public List<HrPublication> Publications { get; set; } = [];

    [JsonPropertyName("receivedTrainings")]
    public List<HrTraining> ReceivedTrainings { get; set; } = [];

    [JsonPropertyName("givenTrainings")]
    public List<HrTraining> GivenTrainings { get; set; } = [];

    [JsonPropertyName("researchProjects")]
    public List<HrResearchProject> ResearchProjects { get; set; } = [];

    [JsonPropertyName("doctoralTheses")]
    public List<HrDoctoralThesis> DoctoralTheses { get; set; } = [];

    [JsonPropertyName("languages")]
    public List<HrLanguageCertification> Languages { get; set; } = [];

    [JsonPropertyName("score")]
    public HrPerformanceScore? Score { get; set; }
}

public class HrDependency
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Departamento tal como lo devuelve el servicio real de la UTA
/// (GET WsUtaSystem/api/v1/rh/vw-departments/by-type/{tipo}); el tipo 128 son las facultades.
/// </summary>
public class HrDepartment
{
    [JsonPropertyName("departmentID")]
    public int DepartmentID { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("departmentName")]
    public string DepartmentName { get; set; } = string.Empty;

    [JsonPropertyName("shortName")]
    public string? ShortName { get; set; }

    [JsonPropertyName("departmentTypeID")]
    public int DepartmentTypeID { get; set; }

    [JsonPropertyName("departmentTypeName")]
    public string? DepartmentTypeName { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}

/// <summary>Ficha liviana de un docente/autoridad, usada para búsqueda (p.ej. al integrar comisiones).</summary>
public class HrTeacherSummary
{
    [JsonPropertyName("teacherId")]
    public string TeacherId { get; set; } = string.Empty;

    [JsonPropertyName("identificationType")]
    public string IdentificationType { get; set; } = string.Empty;

    [JsonPropertyName("identification")]
    public string Identification { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("dependency")]
    public HrDependency Dependency { get; set; } = new();
}

public class HrExperience
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("institution")]
    public string Institution { get; set; } = string.Empty;

    [JsonPropertyName("position")]
    public string Position { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("startDate")]
    public string StartDate { get; set; } = string.Empty;

    [JsonPropertyName("endDate")]
    public string EndDate { get; set; } = string.Empty;

    [JsonPropertyName("years")]
    public int Years { get; set; }

    [JsonPropertyName("months")]
    public int Months { get; set; }

    [JsonPropertyName("knowledgeArea")]
    public string KnowledgeArea { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("supportingDocumentUrl")]
    public string SupportingDocumentUrl { get; set; } = string.Empty;
}

public class HrPublication
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("journal")]
    public string Journal { get; set; } = string.Empty;

    [JsonPropertyName("knowledgeArea")]
    public string KnowledgeArea { get; set; } = string.Empty;

    [JsonPropertyName("publicationDate")]
    public string PublicationDate { get; set; } = string.Empty;

    [JsonPropertyName("doi")]
    public string Doi { get; set; } = string.Empty;

    [JsonPropertyName("link")]
    public string Link { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    [JsonPropertyName("indexingDatabase")]
    public string IndexingDatabase { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("supportingDocumentUrl")]
    public string SupportingDocumentUrl { get; set; } = string.Empty;
}

public class HrTraining
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("trainingCategory")]
    public string TrainingCategory { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("institution")]
    public string Institution { get; set; } = string.Empty;

    [JsonPropertyName("startDate")]
    public string StartDate { get; set; } = string.Empty;

    [JsonPropertyName("endDate")]
    public string EndDate { get; set; } = string.Empty;

    [JsonPropertyName("hours")]
    public int Hours { get; set; }

    [JsonPropertyName("knowledgeArea")]
    public string KnowledgeArea { get; set; } = string.Empty;

    [JsonPropertyName("modality")]
    public string Modality { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("supportingDocumentUrl")]
    public string SupportingDocumentUrl { get; set; } = string.Empty;
}

public class HrResearchProject
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("projectCode")]
    public string ProjectCode { get; set; } = string.Empty;

    [JsonPropertyName("institution")]
    public string Institution { get; set; } = string.Empty;

    [JsonPropertyName("startDate")]
    public string StartDate { get; set; } = string.Empty;

    [JsonPropertyName("endDate")]
    public string EndDate { get; set; } = string.Empty;

    [JsonPropertyName("months")]
    public int Months { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("knowledgeArea")]
    public string KnowledgeArea { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("supportingDocumentUrl")]
    public string SupportingDocumentUrl { get; set; } = string.Empty;
}

public class HrDoctoralThesis
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("institution")]
    public string Institution { get; set; } = string.Empty;

    [JsonPropertyName("approvalDate")]
    public string ApprovalDate { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("knowledgeArea")]
    public string KnowledgeArea { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("supportingDocumentUrl")]
    public string SupportingDocumentUrl { get; set; } = string.Empty;
}

public class HrLanguageCertification
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public string Level { get; set; } = string.Empty;

    [JsonPropertyName("referenceFramework")]
    public string ReferenceFramework { get; set; } = string.Empty;

    [JsonPropertyName("certifyingInstitution")]
    public string CertifyingInstitution { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("issueDate")]
    public string IssueDate { get; set; } = string.Empty;

    [JsonPropertyName("expirationDate")]
    public string ExpirationDate { get; set; } = string.Empty;

    [JsonPropertyName("supportingDocumentUrl")]
    public string SupportingDocumentUrl { get; set; } = string.Empty;
}

public class HrPerformanceScore
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("period")]
    public string Period { get; set; } = string.Empty;

    [JsonPropertyName("percentage")]
    public decimal Percentage { get; set; }

    [JsonPropertyName("supportingDocumentUrl")]
    public string SupportingDocumentUrl { get; set; } = string.Empty;
}
