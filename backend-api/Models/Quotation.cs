using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class Quotation
{
    public long QuotationId { get; set; }

    public string QuotationNumber { get; set; } = null!;

    public long? ProjectId { get; set; }

    public int RevisionNumber { get; set; }

    public DateOnly QuotationDate { get; set; }

    public DateOnly? ValidUntil { get; set; }

    public decimal SubTotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal GrandTotal { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public long? CreatedByUserId { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long? LeadId { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<CustomerApproval> CustomerApprovals { get; set; } = new List<CustomerApproval>();

    public virtual Lead? Lead { get; set; }

    public virtual Project? Project { get; set; }

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<QuotationItem> QuotationItems { get; set; } = new List<QuotationItem>();
}
