using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class ProjectTask
{
    public long ProjectTaskId { get; set; }

    public long ProjectId { get; set; }

    public long? ProjectStageId { get; set; }

    public long? ProjectServiceId { get; set; }

    public string TaskName { get; set; } = null!;

    public string? Description { get; set; }

    public long? AssignedToUserId { get; set; }

    public long? AssignedResourceId { get; set; }

    public string Status { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public DateOnly? StartDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateOnly? CompletedDate { get; set; }

    public decimal ProgressPercent { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long? DependsOnProjectTaskId { get; set; }

    public virtual Resource? AssignedResource { get; set; }

    public virtual User? AssignedToUser { get; set; }

    public virtual ProjectTask? DependsOnProjectTask { get; set; }

    public virtual ICollection<ProjectTask> InverseDependsOnProjectTask { get; set; } = new List<ProjectTask>();

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<ProjectProgress> ProjectProgresses { get; set; } = new List<ProjectProgress>();

    public virtual ProjectService? ProjectService { get; set; }

    public virtual ProjectStage? ProjectStage { get; set; }
}
