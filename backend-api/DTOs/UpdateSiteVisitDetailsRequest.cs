namespace backend_api.DTOs;

public class UpdateSiteVisitDetailsRequest
{
    public string? SiteCondition { get; set; }

    public string? Measurements { get; set; }

    public string? CustomerNotes { get; set; }

    public string? InternalNotes { get; set; }
}