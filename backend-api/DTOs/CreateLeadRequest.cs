namespace backend_api.DTOs;

public class CreateLeadRequest
{
    public string? LeadSource { get; set; }
    public string CustomerName { get; set; } = null!;
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public long? ProjectTypeId { get; set; }
    public string? Location { get; set; }
    public decimal? ApproximateArea { get; set; }
    public string? AreaUnit { get; set; }
    public string? Requirement { get; set; }
    public List<CreateLeadServiceRequest> Services { get; set; } = new();
}

public class CreateLeadServiceRequest
{
    public long ServiceId { get; set; }
    public long? ServiceOptionId { get; set; }
    public string? Requirement { get; set; }
}
