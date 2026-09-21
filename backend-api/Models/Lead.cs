using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class Lead
{
    public long LeadId { get; set; }

    public string LeadCode { get; set; } = null!;

    public long? CustomerId { get; set; }

    public long? ProjectId { get; set; }

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

    public string Status { get; set; } = null!;

    public long? AssignedToUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? AssignedToUser { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<LeadService> LeadServices { get; set; } = new List<LeadService>();

    public virtual Project? Project { get; set; }

    public virtual ProjectType? ProjectType { get; set; }

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();

    public virtual ICollection<SiteVisit> SiteVisits { get; set; } = new List<SiteVisit>();
}
