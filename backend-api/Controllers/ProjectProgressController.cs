using backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectProgressController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public ProjectProgressController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    // GET: api/ProjectProgress/project/1
    [HttpGet("project/{projectId:long}")]
    public async Task<IActionResult> GetProjectProgress(long projectId)
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

        var stages = await _context.ProjectStages
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();

        var tasks = await _context.ProjectTasks
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        // ---------------------------------------------------------
        // 1. Synchronize stage progress from tasks
        // ---------------------------------------------------------

        foreach (var stage in stages)
        {
            var stageTasks = tasks
                .Where(t => t.ProjectStageId == stage.ProjectStageId)
                .ToList();

            // If there are no tasks for this stage, retain its
            // existing status/progress. This is important because
            // future stages may not have tasks yet.
            if (stageTasks.Count == 0)
            {
                continue;
            }

            var progress = Math.Round(
                stageTasks.Average(t => t.ProgressPercent),
                2);

            stage.ProgressPercent = progress;

            if (progress >= 100)
            {
                stage.ProgressPercent = 100;
                stage.Status = "COMPLETED";

                if (!stage.StartDate.HasValue)
                {
                    var earliestTaskStart = stageTasks
                        .Where(t => t.StartDate.HasValue)
                        .Select(t => t.StartDate!.Value)
                        .OrderBy(d => d)
                        .FirstOrDefault();

                    stage.StartDate =
                        earliestTaskStart == default
                            ? DateOnly.FromDateTime(DateTime.Now)
                            : earliestTaskStart;
                }

                if (!stage.EndDate.HasValue)
                {
                    var latestCompletedDate = stageTasks
                        .Where(t => t.CompletedDate.HasValue)
                        .Select(t => t.CompletedDate!.Value)
                        .OrderByDescending(d => d)
                        .FirstOrDefault();

                    stage.EndDate =
                        latestCompletedDate == default
                            ? DateOnly.FromDateTime(DateTime.Now)
                            : latestCompletedDate;
                }
            }
            else if (progress > 0)
            {
                stage.Status = "IN_PROGRESS";

                if (!stage.StartDate.HasValue)
                {
                    stage.StartDate =
                        DateOnly.FromDateTime(DateTime.Now);
                }

                stage.EndDate = null;
            }
            else
            {
                stage.Status = "PENDING";
                stage.EndDate = null;
            }

            stage.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        // ---------------------------------------------------------
        // 2. Build stage results
        // ---------------------------------------------------------

        var stageResults = stages.Select(stage =>
        {
            var stageTasks = tasks
                .Where(t =>
                    t.ProjectStageId ==
                    stage.ProjectStageId)
                .ToList();

            return new
            {
                stage.ProjectStageId,
                stage.StageCode,
                stage.StageName,
                stage.DisplayOrder,
                stage.Status,
                stage.ProgressPercent,
                stage.StartDate,
                stage.EndDate,

                TaskCount = stageTasks.Count,

                CompletedTaskCount = stageTasks.Count(
                    t => t.Status == "COMPLETED")
            };
        }).ToList();

        // ---------------------------------------------------------
        // 3. Calculate PROJECT progress
        //
        // Every initialized project stage participates.
        // ---------------------------------------------------------

        decimal projectProgress = stageResults.Count == 0
            ? 0
            : Math.Round(
                stageResults.Average(
                    s => s.ProgressPercent),
                2);

        // ---------------------------------------------------------
        // 4. Project-level counts
        // ---------------------------------------------------------

        var completedStages = stageResults.Count(
            s => s.Status == "COMPLETED");

        var inProgressStages = stageResults.Count(
            s => s.Status == "IN_PROGRESS");

        var pendingStages = stageResults.Count(
            s => s.Status == "PENDING");

        var stagesWithTasks = stageResults.Count(
            s => s.TaskCount > 0);

        var totalTaskCount = tasks.Count;

        var completedTaskCount = tasks.Count(
            t => t.Status == "COMPLETED");

        decimal taskProgress = totalTaskCount == 0
            ? 0
            : Math.Round(
                tasks.Average(
                    t => t.ProgressPercent),
                2);

        // ---------------------------------------------------------
        // 5. PROJECT COMPLETION RULE
        //
        // A project becomes COMPLETED only when:
        //   - all initialized stages are COMPLETED
        //   - all project tasks are COMPLETED
        //   - all project services are COMPLETED
        //   - Handover stage is COMPLETED
        //
        // This keeps 100% progress separate from the actual
        // Projects.Status value while allowing the application to
        // finalize the project automatically.
        // ---------------------------------------------------------

        var projectServices = await _context.ProjectServices
            .Where(ps => ps.ProjectId == projectId)
            .Select(ps => new
            {
                ps.ProjectServiceId,
                ps.Status
            })
            .ToListAsync();

        var allStagesCompleted =
            stageResults.Count > 0 &&
            completedStages == stageResults.Count;

        var allTasksCompleted =
            totalTaskCount > 0 &&
            completedTaskCount == totalTaskCount;

        var allServicesCompleted =
            projectServices.Count > 0 &&
            projectServices.All(ps =>
                string.Equals(
                    ps.Status,
                    "COMPLETED",
                    StringComparison.OrdinalIgnoreCase));

        var handoverStageCompleted =
            stageResults.Any(s =>
                s.StageCode == "HANDOVER" &&
                s.Status == "COMPLETED");

        var projectCompletionReady =
            allStagesCompleted &&
            allTasksCompleted &&
            allServicesCompleted &&
            handoverStageCompleted;

        if (projectCompletionReady &&
            !string.Equals(
                project.Status,
                "COMPLETED",
                StringComparison.OrdinalIgnoreCase))
        {
            project.Status = "COMPLETED";

            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            projectId,

            // Overall project progress across ALL stages
            projectProgress,

            // Current project status
            projectStatus = project.Status,

            // Completion readiness
            projectCompletionReady,
            allStagesCompleted,
            allTasksCompleted,
            allServicesCompleted,
            handoverStageCompleted,

            // Stage information
            stageCount = stageResults.Count,
            stagesWithTasks,

            completedStages,
            inProgressStages,
            pendingStages,

            // Task information
            totalTaskCount,
            completedTaskCount,
            taskProgress,

            // Service information
            totalProjectServices = projectServices.Count,
            completedProjectServices = projectServices.Count(
                ps => string.Equals(
                    ps.Status,
                    "COMPLETED",
                    StringComparison.OrdinalIgnoreCase)),

            stages = stageResults
        });
    }

    // GET: api/ProjectProgress/project/1/services
    [HttpGet("project/{projectId:long}/services")]
    public async Task<IActionResult> GetProjectServiceProgress(long projectId)
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

        var services = await _context.ProjectServices
            .Where(ps => ps.ProjectId == projectId)
            .Include(ps => ps.Service)
            .Include(ps => ps.ServiceOption)
            .OrderBy(ps => ps.ProjectServiceId)
            .ToListAsync();

        var tasks = await _context.ProjectTasks
            .Where(t =>
                t.ProjectId == projectId &&
                t.ProjectServiceId.HasValue)
            .ToListAsync();

        var results = services.Select(service =>
        {
            var serviceTasks = tasks
                .Where(t =>
                    t.ProjectServiceId ==
                    service.ProjectServiceId)
                .ToList();

            decimal progress = serviceTasks.Count == 0
                ? 0
                : Math.Round(
                    serviceTasks.Average(
                        t => t.ProgressPercent),
                    2);

            return new
            {
                service.ProjectServiceId,
                service.ProjectId,
                service.ServiceId,

                ServiceName =
                    service.Service.ServiceName,

                service.ServiceOptionId,

                OptionCode =
                    service.ServiceOption?.OptionCode,

                OptionName =
                    service.ServiceOption?.OptionName,

                service.Status,
                service.CustomerRequirement,

                TaskCount = serviceTasks.Count,

                CompletedTaskCount =
                    serviceTasks.Count(
                        t => t.Status == "COMPLETED"),

                ProgressPercent = progress
            };
        }).ToList();

        return Ok(results);
    }
}
