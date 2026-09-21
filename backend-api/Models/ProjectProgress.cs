using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class ProjectProgress
{
    public long ProjectProgressId { get; set; }

    public long ProjectId { get; set; }

    public long? ProjectStageId { get; set; }

    public long? ProjectTaskId { get; set; }

    public decimal ProgressPercent { get; set; }

    public string? UpdateTitle { get; set; }

    public string? UpdateDescription { get; set; }

    public long? UpdatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual ProjectStage? ProjectStage { get; set; }

    public virtual ProjectTask? ProjectTask { get; set; }

    public virtual User? UpdatedByUser { get; set; }
}
