using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class Design
{
    public long DesignId { get; set; }

    public long ProjectId { get; set; }

    public long? ProjectServiceId { get; set; }

    public string DesignType { get; set; } = null!;

    public int VersionNumber { get; set; }

    public string? DesignTitle { get; set; }

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public long? CreatedByUserId { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<CustomerApproval> CustomerApprovals { get; set; } = new List<CustomerApproval>();

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual Project Project { get; set; } = null!;

    public virtual ProjectService? ProjectService { get; set; }
}
