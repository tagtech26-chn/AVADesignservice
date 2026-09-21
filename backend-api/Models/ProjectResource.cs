using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class ProjectResource
{
    public long ProjectResourceId { get; set; }

    public long ProjectId { get; set; }

    public long? ProjectServiceId { get; set; }

    public long ResourceId { get; set; }

    public long? AssignedByUserId { get; set; }

    public string AssignmentStatus { get; set; } = null!;

    public decimal? AgreedAmount { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? ScopeOfWork { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? AssignedByUser { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual ProjectService? ProjectService { get; set; }

    public virtual Resource Resource { get; set; } = null!;
}
