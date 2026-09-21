using backend_api.Data;
using backend_api.DTOs;
using backend_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectTasksController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public ProjectTasksController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    // GET: api/ProjectTasks/project/1
    [HttpGet("project/{projectId:long}")]
    public async Task<IActionResult> GetProjectTasks(long projectId)
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

        var tasks = await _context.ProjectTasks
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.ProjectStageId)
            .ThenBy(t => t.ProjectTaskId)
            .Select(t => new
            {
                t.ProjectTaskId,
                t.ProjectId,
                t.ProjectStageId,

                StageCode = t.ProjectStage != null
                    ? t.ProjectStage.StageCode
                    : null,

                StageName = t.ProjectStage != null
                    ? t.ProjectStage.StageName
                    : null,

                t.ProjectServiceId,

                ServiceName = t.ProjectService != null
                    ? t.ProjectService.Service.ServiceName
                    : null,

                t.TaskName,
                t.Description,

                t.AssignedToUserId,

                AssignedUserName = t.AssignedToUser != null
                    ? t.AssignedToUser.UserName
                    : null,

                t.AssignedResourceId,

                AssignedResourceName = t.AssignedResource != null
                    ? t.AssignedResource.ResourceName
                    : null,

                t.Status,
                t.Priority,
                t.StartDate,
                t.DueDate,
                t.CompletedDate,
                t.ProgressPercent,
                t.Notes,
                t.CreatedAt,
                t.UpdatedAt,

                // Dependency
                t.DependsOnProjectTaskId,

                DependsOnTaskName = t.DependsOnProjectTask != null
                    ? t.DependsOnProjectTask.TaskName
                    : null,

                DependsOnTaskStatus = t.DependsOnProjectTask != null
                    ? t.DependsOnProjectTask.Status
                    : null
            })
            .ToListAsync();

        return Ok(tasks);
    }


    // GET: api/ProjectTasks/1
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetProjectTask(long id)
    {
        var task = await _context.ProjectTasks
            .Where(t => t.ProjectTaskId == id)
            .Select(t => new
            {
                t.ProjectTaskId,
                t.ProjectId,
                t.ProjectStageId,

                StageCode = t.ProjectStage != null
                    ? t.ProjectStage.StageCode
                    : null,

                StageName = t.ProjectStage != null
                    ? t.ProjectStage.StageName
                    : null,

                t.ProjectServiceId,

                ServiceName = t.ProjectService != null
                    ? t.ProjectService.Service.ServiceName
                    : null,

                t.TaskName,
                t.Description,

                t.AssignedToUserId,

                AssignedUserName = t.AssignedToUser != null
                    ? t.AssignedToUser.UserName
                    : null,

                t.AssignedResourceId,

                AssignedResourceName = t.AssignedResource != null
                    ? t.AssignedResource.ResourceName
                    : null,

                t.Status,
                t.Priority,
                t.StartDate,
                t.DueDate,
                t.CompletedDate,
                t.ProgressPercent,
                t.Notes,
                t.CreatedAt,
                t.UpdatedAt,

                // Dependency
                t.DependsOnProjectTaskId,

                DependsOnTaskName = t.DependsOnProjectTask != null
                    ? t.DependsOnProjectTask.TaskName
                    : null,

                DependsOnTaskStatus = t.DependsOnProjectTask != null
                    ? t.DependsOnProjectTask.Status
                    : null
            })
            .FirstOrDefaultAsync();

        if (task == null)
        {
            return NotFound(new
            {
                message = "Project task not found."
            });
        }

        return Ok(task);
    }


    // POST: api/ProjectTasks/project/1
    [HttpPost("project/{projectId:long}")]
    public async Task<IActionResult> CreateProjectTask(
        long projectId,
        [FromBody] CreateProjectTaskRequest request)
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

        if (string.IsNullOrWhiteSpace(request.TaskName))
        {
            return BadRequest(new
            {
                message = "TaskName is required."
            });
        }


        // Validate Project Stage
        if (request.ProjectStageId.HasValue)
        {
            var stageExists = await _context.ProjectStages
                .AnyAsync(s =>
                    s.ProjectStageId == request.ProjectStageId.Value &&
                    s.ProjectId == projectId);

            if (!stageExists)
            {
                return BadRequest(new
                {
                    message = "The selected ProjectStage does not belong to this project."
                });
            }
        }


        // Validate Project Service
        if (request.ProjectServiceId.HasValue)
        {
            var serviceExists = await _context.ProjectServices
                .AnyAsync(s =>
                    s.ProjectServiceId == request.ProjectServiceId.Value &&
                    s.ProjectId == projectId);

            if (!serviceExists)
            {
                return BadRequest(new
                {
                    message = "The selected ProjectService does not belong to this project."
                });
            }
        }


        // Validate dependency
        if (request.DependsOnProjectTaskId.HasValue)
        {
            var dependencyTask = await _context.ProjectTasks
                .FirstOrDefaultAsync(t =>
                    t.ProjectTaskId == request.DependsOnProjectTaskId.Value);

            if (dependencyTask == null)
            {
                return BadRequest(new
                {
                    message = "The prerequisite task does not exist."
                });
            }

            if (dependencyTask.ProjectId != projectId)
            {
                return BadRequest(new
                {
                    message = "The prerequisite task does not belong to this project."
                });
            }
        }


        // Validate assigned user
        if (request.AssignedToUserId.HasValue)
        {
            var userExists = await _context.Users
                .AnyAsync(u =>
                    u.UserId == request.AssignedToUserId.Value &&
                    u.IsActive);

            if (!userExists)
            {
                return BadRequest(new
                {
                    message = "The selected user does not exist or is inactive."
                });
            }
        }


        // Validate assigned resource
        if (request.AssignedResourceId.HasValue)
        {
            var resourceExists = await _context.Resources
                .AnyAsync(r =>
                    r.ResourceId == request.AssignedResourceId.Value &&
                    r.IsActive);

            if (!resourceExists)
            {
                return BadRequest(new
                {
                    message = "The selected resource does not exist or is inactive."
                });
            }
        }


        // Validate priority
        var priority = string.IsNullOrWhiteSpace(request.Priority)
            ? "MEDIUM"
            : request.Priority.Trim().ToUpperInvariant();

        var allowedPriorities = new[]
        {
            "LOW",
            "MEDIUM",
            "HIGH",
            "URGENT"
        };

        if (!allowedPriorities.Contains(priority))
        {
            return BadRequest(new
            {
                message = "Invalid priority.",
                allowedPriorities
            });
        }


        var now = DateTime.Now;

        var task = new ProjectTask
        {
            ProjectId = projectId,
            ProjectStageId = request.ProjectStageId,
            ProjectServiceId = request.ProjectServiceId,

            TaskName = request.TaskName.Trim(),
            Description = request.Description,

            AssignedToUserId = request.AssignedToUserId,
            AssignedResourceId = request.AssignedResourceId,

            Status = "PENDING",
            Priority = priority,

            StartDate = request.StartDate,
            DueDate = request.DueDate,

            ProgressPercent = 0,

            Notes = request.Notes,

            // Dependency
            DependsOnProjectTaskId = request.DependsOnProjectTaskId,

            CreatedAt = now
        };

        _context.ProjectTasks.Add(task);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetProjectTask),
            new { id = task.ProjectTaskId },
            new
            {
                message = "Project task created successfully.",
                taskId = task.ProjectTaskId,
                projectId = task.ProjectId,
                taskName = task.TaskName,
                status = task.Status,
                priority = task.Priority,
                dependsOnProjectTaskId = task.DependsOnProjectTaskId
            });
    }


    // ============================================================
    // HANDOVER GATE
    // Project-level Handover tasks cannot start until all applicable
    // project services have been completed.
    // ============================================================

    private async Task<IActionResult?> ValidateHandoverGate(ProjectTask task)
    {
        if (!IsHandoverTask(task))
        {
            return null;
        }

        var projectServices = await _context.ProjectServices
            .Where(ps => ps.ProjectId == task.ProjectId)
            .Select(ps => new
            {
                ps.ProjectServiceId,
                ps.Status,
                ServiceName = ps.Service.ServiceName
            })
            .ToListAsync();

        if (projectServices.Count == 0)
        {
            return BadRequest(new
            {
                message = "Handover cannot start because the project has no services."
            });
        }

        var incompleteServices = projectServices
            .Where(ps => !string.Equals(
                ps.Status,
                "COMPLETED",
                StringComparison.OrdinalIgnoreCase))
            .Select(ps => new
            {
                ps.ProjectServiceId,
                ps.ServiceName,
                ps.Status
            })
            .ToList();

        if (incompleteServices.Count > 0)
        {
            return BadRequest(new
            {
                message =
                    "Handover cannot start until all applicable project services are completed.",
                taskId = task.ProjectTaskId,
                taskName = task.TaskName,
                totalProjectServices = projectServices.Count,
                incompleteServiceCount = incompleteServices.Count,
                incompleteServices
            });
        }

        return null;
    }

    private static bool IsHandoverTask(ProjectTask task)
    {
        return task.ProjectServiceId == null &&
               !string.IsNullOrWhiteSpace(task.TaskName) &&
               task.TaskName.Trim().ToUpperInvariant() is
                   "FINAL CUSTOMER INSPECTION" or
                   "SNAG / PUNCH LIST" or
                   "SNAG RECTIFICATION" or
                   "FINAL VERIFICATION" or
                   "DOCUMENTS / WARRANTY HANDOVER" or
                   "FINAL PAYMENT CLEARANCE" or
                   "CUSTOMER HANDOVER ACCEPTANCE";
    }


    // PUT: api/ProjectTasks/1/status
    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(
        long id,
        [FromBody] UpdateProjectTaskStatusRequest request)
    {
        var task = await _context.ProjectTasks
            .FirstOrDefaultAsync(t => t.ProjectTaskId == id);

        if (task == null)
        {
            return NotFound(new
            {
                message = "Project task not found."
            });
        }


        var newStatus = request.Status?.Trim().ToUpperInvariant();

        var allowedStatuses = new[]
        {
            "PENDING",
            "IN_PROGRESS",
            "BLOCKED",
            "COMPLETED",
            "CANCELLED"
        };

        if (string.IsNullOrWhiteSpace(newStatus) ||
            !allowedStatuses.Contains(newStatus))
        {
            return BadRequest(new
            {
                message = "Invalid task status.",
                allowedStatuses
            });
        }


        // ============================================================
        // HANDOVER GATE
        // The first and all project-level Handover tasks are blocked
        // until every applicable project service is COMPLETED.
        // ============================================================

        if (newStatus == "IN_PROGRESS" || newStatus == "COMPLETED")
        {
            var handoverGateResult = await ValidateHandoverGate(task);

            if (handoverGateResult != null)
            {
                return handoverGateResult;
            }
        }


        // ============================================================
        // DEPENDENCY CHECK
        // A task cannot start or complete until its prerequisite
        // task has been completed.
        // ============================================================

        if ((newStatus == "IN_PROGRESS" ||
             newStatus == "COMPLETED") &&
            task.DependsOnProjectTaskId.HasValue)
        {
            var prerequisite = await _context.ProjectTasks
                .FirstOrDefaultAsync(t =>
                    t.ProjectTaskId ==
                    task.DependsOnProjectTaskId.Value);

            if (prerequisite == null)
            {
                return BadRequest(new
                {
                    message = "The prerequisite task no longer exists.",
                    prerequisiteTaskId =
                        task.DependsOnProjectTaskId
                });
            }

            if (prerequisite.Status != "COMPLETED")
            {
                return BadRequest(new
                {
                    message =
                        "This task cannot start until its prerequisite task is completed.",

                    taskId = task.ProjectTaskId,
                    taskName = task.TaskName,

                    prerequisiteTaskId =
                        prerequisite.ProjectTaskId,

                    prerequisiteTaskName =
                        prerequisite.TaskName,

                    prerequisiteStatus =
                        prerequisite.Status
                });
            }
        }


        // Complete task
        if (newStatus == "COMPLETED")
        {
            task.ProgressPercent = 100;

            task.CompletedDate =
                DateOnly.FromDateTime(DateTime.Now);
        }


        // Re-open completed task
        else if (task.Status == "COMPLETED" &&
                 newStatus != "COMPLETED")
        {
            task.CompletedDate = null;
        }


        // Start task
        if (newStatus == "IN_PROGRESS" &&
            !task.StartDate.HasValue)
        {
            task.StartDate =
                DateOnly.FromDateTime(DateTime.Now);
        }


        task.Status = newStatus;
        task.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Project task status updated successfully.",

            taskId = task.ProjectTaskId,

            status = task.Status,

            progressPercent =
                task.ProgressPercent,

            startDate =
                task.StartDate,

            completedDate =
                task.CompletedDate,

            dependsOnProjectTaskId =
                task.DependsOnProjectTaskId
        });
    }


    // PUT: api/ProjectTasks/1/progress
    [HttpPut("{id:long}/progress")]
    public async Task<IActionResult> UpdateProgress(
        long id,
        [FromBody] UpdateProjectTaskProgressRequest request)
    {
        var task = await _context.ProjectTasks
            .FirstOrDefaultAsync(t => t.ProjectTaskId == id);

        if (task == null)
        {
            return NotFound(new
            {
                message = "Project task not found."
            });
        }


        if (request.ProgressPercent < 0 ||
            request.ProgressPercent > 100)
        {
            return BadRequest(new
            {
                message =
                    "ProgressPercent must be between 0 and 100."
            });
        }


        // ============================================================
        // HANDOVER GATE
        // Any progress above 0 means the Handover task is starting.
        // All applicable project services must already be COMPLETED.
        // ============================================================

        if (request.ProgressPercent > 0)
        {
            var handoverGateResult = await ValidateHandoverGate(task);

            if (handoverGateResult != null)
            {
                return handoverGateResult;
            }
        }


        // ============================================================
        // DEPENDENCY CHECK
        // Any progress above 0 means the task is starting.
        // Therefore the prerequisite must already be completed.
        // ============================================================

        if (request.ProgressPercent > 0 &&
            task.DependsOnProjectTaskId.HasValue)
        {
            var prerequisite = await _context.ProjectTasks
                .FirstOrDefaultAsync(t =>
                    t.ProjectTaskId ==
                    task.DependsOnProjectTaskId.Value);

            if (prerequisite == null)
            {
                return BadRequest(new
                {
                    message =
                        "The prerequisite task no longer exists.",

                    prerequisiteTaskId =
                        task.DependsOnProjectTaskId
                });
            }

            if (prerequisite.Status != "COMPLETED")
            {
                return BadRequest(new
                {
                    message =
                        "Progress cannot be started until the prerequisite task is completed.",

                    taskId = task.ProjectTaskId,

                    taskName = task.TaskName,

                    prerequisiteTaskId =
                        prerequisite.ProjectTaskId,

                    prerequisiteTaskName =
                        prerequisite.TaskName,

                    prerequisiteStatus =
                        prerequisite.Status
                });
            }
        }


        task.ProgressPercent =
            request.ProgressPercent;

        task.Notes =
            request.Notes ?? task.Notes;

        task.UpdatedAt =
            DateTime.Now;


        // 100% = completed
        if (request.ProgressPercent >= 100)
        {
            task.ProgressPercent = 100;

            task.Status = "COMPLETED";

            task.CompletedDate =
                DateOnly.FromDateTime(DateTime.Now);
        }


        // > 0% = in progress
        else if (request.ProgressPercent > 0 &&
                 task.Status == "PENDING")
        {
            task.Status = "IN_PROGRESS";

            if (!task.StartDate.HasValue)
            {
                task.StartDate =
                    DateOnly.FromDateTime(DateTime.Now);
            }
        }


        await _context.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Project task progress updated successfully.",

            taskId = task.ProjectTaskId,

            status = task.Status,

            progressPercent =
                task.ProgressPercent,

            startDate =
                task.StartDate,

            completedDate =
                task.CompletedDate,

            dependsOnProjectTaskId =
                task.DependsOnProjectTaskId
        });
    }
}