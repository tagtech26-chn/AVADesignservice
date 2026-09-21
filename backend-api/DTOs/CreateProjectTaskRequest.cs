namespace backend_api.DTOs;

public class CreateProjectTaskRequest
{
    public long? ProjectStageId { get; set; }

    public long? ProjectServiceId { get; set; }

    public string TaskName { get; set; } = null!;

    public string? Description { get; set; }

    public long? AssignedToUserId { get; set; }

    public long? AssignedResourceId { get; set; }

    public string? Priority { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public string? Notes { get; set; }

    public long? DependsOnProjectTaskId { get; set; }
}