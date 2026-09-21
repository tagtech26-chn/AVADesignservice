using backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectServicesController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public ProjectServicesController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // GET: api/ProjectServices/project/1
    // =========================================================
    [HttpGet("project/{projectId:long}")]
    public async Task<IActionResult> GetProjectServices(long projectId)
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
            .Select(ps => new
            {
                ps.ProjectServiceId,
                ps.ProjectId,
                ps.ServiceId,

                ServiceName = ps.Service.ServiceName,

                ps.ServiceOptionId,

                OptionCode = ps.ServiceOption != null
                    ? ps.ServiceOption.OptionCode
                    : null,

                OptionName = ps.ServiceOption != null
                    ? ps.ServiceOption.OptionName
                    : null,

                ps.CustomerRequirement,
                ps.Quantity,
                ps.Unit,
                ps.Status,
                ps.EstimatedAmount,
                ps.ApprovedAmount,
                ps.CustomerNotes,
                ps.InternalNotes,
                ps.CreatedAt,
                ps.UpdatedAt
            })
            .ToListAsync();

        return Ok(services);
    }

    // =========================================================
    // GET: api/ProjectServices/1
    // =========================================================
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetProjectService(long id)
    {
        var service = await _context.ProjectServices
            .Where(ps => ps.ProjectServiceId == id)
            .Include(ps => ps.Service)
            .Include(ps => ps.ServiceOption)
            .Select(ps => new
            {
                ps.ProjectServiceId,
                ps.ProjectId,
                ps.ServiceId,

                ServiceName = ps.Service.ServiceName,

                ps.ServiceOptionId,

                OptionCode = ps.ServiceOption != null
                    ? ps.ServiceOption.OptionCode
                    : null,

                OptionName = ps.ServiceOption != null
                    ? ps.ServiceOption.OptionName
                    : null,

                ps.CustomerRequirement,
                ps.Quantity,
                ps.Unit,
                ps.Status,
                ps.EstimatedAmount,
                ps.ApprovedAmount,
                ps.CustomerNotes,
                ps.InternalNotes,
                ps.CreatedAt,
                ps.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (service == null)
        {
            return NotFound(new
            {
                message = "Project service not found."
            });
        }

        return Ok(service);
    }

    // =========================================================
    // PUT: api/ProjectServices/1/status
    // =========================================================
    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(
        long id,
        [FromBody] UpdateProjectServiceStatusRequest request)
    {
        var service = await _context.ProjectServices
            .Include(ps => ps.Service)
            .FirstOrDefaultAsync(
                ps => ps.ProjectServiceId == id);

        if (service == null)
        {
            return NotFound(new
            {
                message = "Project service not found."
            });
        }

        // -----------------------------------------------------
        // Validate requested status
        // -----------------------------------------------------

        var newStatus =
            request.Status?.Trim().ToUpperInvariant();

        var allowedStatuses = new[]
        {
            "REQUESTED",
            "REVIEWING",
            "QUOTED",
            "APPROVED",
            "ASSIGNED",
            "IN_PROGRESS",
            "COMPLETED",
            "ON_HOLD",
            "CANCELLED"
        };

        if (string.IsNullOrWhiteSpace(newStatus) ||
            !allowedStatuses.Contains(newStatus))
        {
            return BadRequest(new
            {
                message = "Invalid project service status.",
                allowedStatuses
            });
        }

        // -----------------------------------------------------
        // Validate status transition
        // -----------------------------------------------------

        if (!IsValidTransition(
                service.Status,
                newStatus))
        {
            return BadRequest(new
            {
                message =
                    $"Project service cannot be changed from '{service.Status}' to '{newStatus}'."
            });
        }

        // -----------------------------------------------------
        // COMPLETED protection
        //
        // A service cannot be completed while any associated
        // project task remains incomplete.
        // -----------------------------------------------------

        if (newStatus == "COMPLETED")
        {
            var serviceTasks = await _context.ProjectTasks
                .Where(t =>
                    t.ProjectServiceId ==
                    service.ProjectServiceId)
                .OrderBy(t => t.ProjectTaskId)
                .ToListAsync();

            var incompleteTasks = serviceTasks
                .Where(t => t.Status != "COMPLETED")
                .ToList();

            if (incompleteTasks.Count > 0)
            {
                return BadRequest(new
                {
                    message =
                        "Project service cannot be completed because not all associated tasks are completed.",

                    projectServiceId =
                        service.ProjectServiceId,

                    serviceName =
                        service.Service.ServiceName,

                    taskCount =
                        serviceTasks.Count,

                    completedTaskCount =
                        serviceTasks.Count(
                            t => t.Status == "COMPLETED"),

                    remainingTaskCount =
                        incompleteTasks.Count,

                    incompleteTasks =
                        incompleteTasks.Select(t => new
                        {
                            t.ProjectTaskId,
                            t.TaskName,
                            t.Status,
                            t.ProgressPercent
                        })
                });
            }
        }

        // -----------------------------------------------------
        // Update service status
        // -----------------------------------------------------

        service.Status = newStatus;
        service.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Project service status updated successfully.",

            projectServiceId =
                service.ProjectServiceId,

            serviceName =
                service.Service.ServiceName,

            status =
                service.Status
        });
    }

    // =========================================================
    // Status transition rules
    // =========================================================
    private static bool IsValidTransition(
        string currentStatus,
        string newStatus)
    {
        if (currentStatus == newStatus)
        {
            return true;
        }

        return currentStatus switch
        {
            // -------------------------------------------------
            // REQUESTED
            // -------------------------------------------------
            "REQUESTED" =>
                newStatus == "REVIEWING" ||
                newStatus == "QUOTED" ||
                newStatus == "CANCELLED",

            // -------------------------------------------------
            // REVIEWING
            // -------------------------------------------------
            "REVIEWING" =>
                newStatus == "QUOTED" ||
                newStatus == "CANCELLED",

            // -------------------------------------------------
            // QUOTED
            // -------------------------------------------------
            "QUOTED" =>
                newStatus == "APPROVED" ||
                newStatus == "CANCELLED",

            // -------------------------------------------------
            // APPROVED
            // -------------------------------------------------
            "APPROVED" =>
                newStatus == "ASSIGNED" ||
                newStatus == "IN_PROGRESS" ||
                newStatus == "CANCELLED",

            // -------------------------------------------------
            // ASSIGNED
            // -------------------------------------------------
            "ASSIGNED" =>
                newStatus == "IN_PROGRESS" ||
                newStatus == "ON_HOLD" ||
                newStatus == "CANCELLED",

            // -------------------------------------------------
            // IN_PROGRESS
            // -------------------------------------------------
            "IN_PROGRESS" =>
                newStatus == "COMPLETED" ||
                newStatus == "ON_HOLD" ||
                newStatus == "CANCELLED",

            // -------------------------------------------------
            // ON_HOLD
            // -------------------------------------------------
            "ON_HOLD" =>
                newStatus == "IN_PROGRESS" ||
                newStatus == "CANCELLED",

            // -------------------------------------------------
            // COMPLETED
            // -------------------------------------------------
            "COMPLETED" =>
                false,

            // -------------------------------------------------
            // CANCELLED
            // -------------------------------------------------
            "CANCELLED" =>
                false,

            _ => false
        };
    }
}

// =============================================================
// Request DTO
// =============================================================
public class UpdateProjectServiceStatusRequest
{
    public string Status { get; set; } = null!;
}