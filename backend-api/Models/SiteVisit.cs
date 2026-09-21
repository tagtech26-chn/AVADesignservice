using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class SiteVisit
{
    public long SiteVisitId { get; set; }

    public long? ProjectId { get; set; }

    public long? AssignedToUserId { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string Status { get; set; } = null!;

    public string? SiteCondition { get; set; }

    public string? Measurements { get; set; }

    public string? CustomerNotes { get; set; }

    public string? InternalNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long? LeadId { get; set; }

    public virtual User? AssignedToUser { get; set; }

    public virtual Lead? Lead { get; set; }

    public virtual Project? Project { get; set; }
}
