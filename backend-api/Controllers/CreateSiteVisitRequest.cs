namespace backend_api.DTOs;

public class CreateSiteVisitRequest
{
    public long? LeadId { get; set; }

    public long? ProjectId { get; set; }

    public long? AssignedToUserId { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public string? Status { get; set; }

    public string? SiteCondition { get; set; }

    public string? Measurements { get; set; }

    public string? CustomerNotes { get; set; }

    public string? InternalNotes { get; set; }
}