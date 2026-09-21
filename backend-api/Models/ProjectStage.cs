using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class ProjectStage
{
    public long ProjectStageId { get; set; }

    public long ProjectId { get; set; }

    public string StageCode { get; set; } = null!;

    public string StageName { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public string Status { get; set; } = null!;

    public decimal ProgressPercent { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<ProjectProgress> ProjectProgresses { get; set; } = new List<ProjectProgress>();

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();
}
