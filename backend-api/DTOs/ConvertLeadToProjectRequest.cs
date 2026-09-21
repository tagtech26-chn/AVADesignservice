namespace backend_api.DTOs;

public class ConvertLeadToProjectRequest
{
    public long? ApprovedQuotationId { get; set; }
    public string? ProjectName { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateOnly? ExpectedStartDate { get; set; }
    public DateOnly? ExpectedEndDate { get; set; }
}