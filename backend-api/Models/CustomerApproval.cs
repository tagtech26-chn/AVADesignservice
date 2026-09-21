using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class CustomerApproval
{
    public long CustomerApprovalId { get; set; }

    public long ProjectId { get; set; }

    public long? QuotationId { get; set; }

    public long? DesignId { get; set; }

    public string ApprovalType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Comments { get; set; }

    public long? ApprovedByUserId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? ApprovedByUser { get; set; }

    public virtual Design? Design { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual Quotation? Quotation { get; set; }
}
