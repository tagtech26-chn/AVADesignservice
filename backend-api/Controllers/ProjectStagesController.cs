using backend_api.Data;
using backend_api.DTOs;
using backend_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectStagesController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public ProjectStagesController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    // GET: api/ProjectStages/project/1
    [HttpGet("project/{projectId:long}")]
    public async Task<IActionResult> GetProjectStages(long projectId)
    {
        var projectExists = await _context.Projects
            .AnyAsync(p => p.ProjectId == projectId);

        if (!projectExists)
        {
            return NotFound(new
            {
                message = "Project not found."
            });
        }

        var stages = await _context.ProjectStages
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new
            {
                s.ProjectStageId,
                s.ProjectId,
                s.StageCode,
                s.StageName,
                s.DisplayOrder,
                s.Status,
                s.ProgressPercent,
                s.StartDate,
                s.EndDate,
                s.Notes,
                s.CreatedAt,
                s.UpdatedAt
            })
            .ToListAsync();

        return Ok(stages);
    }

    // POST: api/ProjectStages/project/1/initialize
    [HttpPost("project/{projectId:long}/initialize")]
    public async Task<IActionResult> InitializeProjectStages(
        long projectId,
        [FromBody] InitializeProjectStagesRequest request)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
            {
                return NotFound(new
                {
                    message = "Project not found."
                });
            }

            // Prevent duplicate initialization
            var existingStages = await _context.ProjectStages
                .Where(s => s.ProjectId == projectId)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            if (existingStages.Count > 0)
            {
                return Conflict(new
                {
                    message = "Project stages have already been initialized.",
                    projectId,
                    stageCount = existingStages.Count
                });
            }

            // Determine whether the project originated from a lead
            // and whether that lead already has a completed site visit.
            bool siteAssessmentCompleted = false;

            if (project.LeadId.HasValue)
            {
                siteAssessmentCompleted = await _context.SiteVisits
                    .AnyAsync(v =>
                        v.LeadId == project.LeadId.Value &&
                        v.Status == "COMPLETED");
            }

            var today = DateOnly.FromDateTime(DateTime.Now);

            var stages = new List<ProjectStage>
            {
                new ProjectStage
                {
                    ProjectId = projectId,
                    StageCode = "SITE_ASSESSMENT",
                    StageName = "Site Assessment",
                    DisplayOrder = 1,
                    Status = siteAssessmentCompleted ? "COMPLETED" : "IN_PROGRESS",
                    ProgressPercent = siteAssessmentCompleted ? 100 : 0,
                    StartDate = today,
                    EndDate = siteAssessmentCompleted ? today : null,
                    CreatedAt = DateTime.Now
                },

                new ProjectStage
                {
                    ProjectId = projectId,
                    StageCode = "DESIGN",
                    StageName = "Design",
                    DisplayOrder = 2,
                    Status = siteAssessmentCompleted ? "IN_PROGRESS" : "PENDING",
                    ProgressPercent = 0,
                    StartDate = siteAssessmentCompleted ? today : null,
                    CreatedAt = DateTime.Now
                },

                new ProjectStage
                {
                    ProjectId = projectId,
                    StageCode = "CUSTOMER_REVIEW",
                    StageName = "Customer Review",
                    DisplayOrder = 3,
                    Status = "PENDING",
                    ProgressPercent = 0,
                    CreatedAt = DateTime.Now
                },

                new ProjectStage
                {
                    ProjectId = projectId,
                    StageCode = "PLANNING",
                    StageName = "Planning",
                    DisplayOrder = 4,
                    Status = "PENDING",
                    ProgressPercent = 0,
                    CreatedAt = DateTime.Now
                },

                new ProjectStage
                {
                    ProjectId = projectId,
                    StageCode = "EXECUTION",
                    StageName = "Execution",
                    DisplayOrder = 5,
                    Status = "PENDING",
                    ProgressPercent = 0,
                    CreatedAt = DateTime.Now
                },

                new ProjectStage
                {
                    ProjectId = projectId,
                    StageCode = "INSPECTION",
                    StageName = "Inspection",
                    DisplayOrder = 6,
                    Status = "PENDING",
                    ProgressPercent = 0,
                    CreatedAt = DateTime.Now
                },

                new ProjectStage
                {
                    ProjectId = projectId,
                    StageCode = "HANDOVER",
                    StageName = "Handover",
                    DisplayOrder = 7,
                    Status = "PENDING",
                    ProgressPercent = 0,
                    CreatedAt = DateTime.Now
                }
            };

            _context.ProjectStages.AddRange(stages);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Project stages initialized successfully.",
                projectId,
                stageCount = stages.Count,
                siteAssessmentCompleted,
                stages = stages.Select(s => new
                {
                    s.ProjectStageId,
                    s.StageCode,
                    s.StageName,
                    s.DisplayOrder,
                    s.Status,
                    s.ProgressPercent
                })
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}