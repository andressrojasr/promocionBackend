namespace PromocionBackend.Application.DTOs.Teachers;

public record TeacherSummaryDto(
    string TeacherId,
    string Identification,
    string FullName,
    string? FacultyId,
    string? FacultyName);
