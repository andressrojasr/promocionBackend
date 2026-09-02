using PromocionBackend.Domain.Constants;
using PromocionBackend.Domain.Entities;

namespace PromocionBackend.Application.Services;

/// <summary>
/// Centraliza toda la lógica de filtrado y comparación de estados usando extension methods.
/// Elimina duplicación entre ApplicationService y DashboardService.
/// </summary>
public static class ApplicationStatusService
{
    /// <summary>Filtra aplicaciones en progreso (no finales).</summary>
    public static IQueryable<PromotionApplication> InProgress(this IQueryable<PromotionApplication> query) =>
        query.Where(a => a.Status != ApplicationStatus.ThRejected
            && a.Status != ApplicationStatus.Approved
            && a.Status != ApplicationStatus.Rejected);

    /// <summary>Filtra aplicaciones en estado final.</summary>
    public static IQueryable<PromotionApplication> Final(this IQueryable<PromotionApplication> query) =>
        query.Where(a => a.Status == ApplicationStatus.ThRejected
            || a.Status == ApplicationStatus.Approved
            || a.Status == ApplicationStatus.Rejected);

    /// <summary>Filtra aplicaciones aprobadas.</summary>
    public static IQueryable<PromotionApplication> Approved(this IQueryable<PromotionApplication> query) =>
        query.Where(a => a.Status == ApplicationStatus.Approved);

    /// <summary>Filtra aplicaciones rechazadas definitivamente.</summary>
    public static IQueryable<PromotionApplication> Rejected(this IQueryable<PromotionApplication> query) =>
        query.Where(a => a.Status == ApplicationStatus.Rejected);

    /// <summary>Filtra aplicaciones rechazadas por TH (estado final).</summary>
    public static IQueryable<PromotionApplication> ThRejected(this IQueryable<PromotionApplication> query) =>
        query.Where(a => a.Status == ApplicationStatus.ThRejected);

    /// <summary>Filtra aplicaciones pendientes de revisión TH.</summary>
    public static IQueryable<PromotionApplication> PendingTh(this IQueryable<PromotionApplication> query) =>
        query.Where(a => a.Status == ApplicationStatus.Submitted);

    /// <summary>Filtra aplicaciones pendientes de revisión CP.</summary>
    public static IQueryable<PromotionApplication> PendingCp(this IQueryable<PromotionApplication> query) =>
        query.Where(a => a.Status == ApplicationStatus.ThApproved);

    /// <summary>Filtra aplicaciones pendientes de revisión CA (apelaciones).</summary>
    public static IQueryable<PromotionApplication> PendingCa(this IQueryable<PromotionApplication> query) =>
        query.Where(a => a.Status == ApplicationStatus.Appealed);

    /// <summary>Filtra aplicaciones rechazadas por CP (apelables si está en plazo).</summary>
    public static IQueryable<PromotionApplication> CpRejected(this IQueryable<PromotionApplication> query) =>
        query.Where(a => a.Status == ApplicationStatus.CpRejected);

    /// <summary>Filtra aplicaciones por un estado específico.</summary>
    public static IQueryable<PromotionApplication> WithStatus(this IQueryable<PromotionApplication> query, ApplicationStatus? status)
    {
        if (status is null)
            return query;

        return query.Where(a => a.Status == status);
    }

    /// <summary>Filtra aplicaciones por un estado específico desde string (con validación).</summary>
    public static IQueryable<PromotionApplication> WithStatus(this IQueryable<PromotionApplication> query, string? statusString)
    {
        if (string.IsNullOrWhiteSpace(statusString))
            return query;

        if (!ApplicationStatusExtensions.TryFromStringValue(statusString, out var status))
            return query.Where(_ => false); // Retorna vacío si status es inválido

        return query.Where(a => a.Status == status);
    }
}
