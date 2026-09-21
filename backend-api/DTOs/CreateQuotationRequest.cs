namespace backend_api.DTOs;

public class CreateQuotationRequest
{
    public long? LeadId { get; set; }

    public long? ProjectId { get; set; }

    public DateOnly? QuotationDate { get; set; }

    public DateOnly? ValidUntil { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public string? Notes { get; set; }

    public long? CreatedByUserId { get; set; }

    public List<CreateQuotationItemRequest> Items { get; set; } = new();
}

public class CreateQuotationItemRequest
{
    public long? ProjectServiceId { get; set; }

    public string ItemType { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxPercent { get; set; }

    public string? Notes { get; set; }

    public int DisplayOrder { get; set; }
}