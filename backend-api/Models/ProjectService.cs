using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class ProjectService
{
    public long ProjectServiceId { get; set; }

    public long ProjectId { get; set; }

    public long ServiceId { get; set; }

    public long? ServiceOptionId { get; set; }

    public string? CustomerRequirement { get; set; }

    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }

    public string Status { get; set; } = null!;

    public decimal? EstimatedAmount { get; set; }

    public decimal? ApprovedAmount { get; set; }

    public string? CustomerNotes { get; set; }

    public string? InternalNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Design> Designs { get; set; } = new List<Design>();

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<ProjectResource> ProjectResources { get; set; } = new List<ProjectResource>();

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();

    public virtual ICollection<QuotationItem> QuotationItems { get; set; } = new List<QuotationItem>();

    public virtual Service Service { get; set; } = null!;

    public virtual ServiceOption? ServiceOption { get; set; }
}
