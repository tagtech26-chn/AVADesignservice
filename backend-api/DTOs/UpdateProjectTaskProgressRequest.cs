namespace backend_api.DTOs;

public class UpdateProjectTaskProgressRequest
{
    public decimal ProgressPercent { get; set; }

    public string? Notes { get; set; }
}