using backend_api.Data;
using backend_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DesignsController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public DesignsController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // GET: api/Designs/project/1
    // =========================================================
    [HttpGet("project/{projectId:long}")]
    public async Task<IActionResult> GetProjectDesigns(long projectId)
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

        var designs = await _context.Designs
            .Where(d => d.ProjectId == projectId)
            .Include(d => d.ProjectService)
                .ThenInclude(ps => ps!.Service)
            .OrderByDescending(d => d.VersionNumber)
            .ThenByDescending(d => d.CreatedAt)
            .Select(d => new
            {
                d.DesignId,
                d.ProjectId,
                d.ProjectServiceId,

                ServiceName = d.ProjectService != null
                    ? d.ProjectService.Service.ServiceName
                    : null,

                d.DesignType,
                d.VersionNumber,
                d.DesignTitle,
                d.Description,
                d.Status,
                d.CreatedByUserId,
                d.SubmittedAt,
                d.ApprovedAt,
                d.CreatedAt,
                d.UpdatedAt
            })
            .ToListAsync();

        return Ok(designs);
    }

    // =========================================================
    // GET: api/Designs/1
    // =========================================================
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetDesign(long id)
    {
        var design = await _context.Designs
            .Where(d => d.DesignId == id)
            .Include(d => d.ProjectService)
                .ThenInclude(ps => ps!.Service)
            .Select(d => new
            {
                d.DesignId,
                d.ProjectId,
                d.ProjectServiceId,

                ServiceName = d.ProjectService != null
                    ? d.ProjectService.Service.ServiceName
                    : null,

                d.DesignType,
                d.VersionNumber,
                d.DesignTitle,
                d.Description,
                d.Status,
                d.CreatedByUserId,
                d.SubmittedAt,
                d.ApprovedAt,
                d.CreatedAt,
                d.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (design == null)
        {
            return NotFound(new
            {
                message = "Design not found."
            });
        }

        return Ok(design);
    }

    // =========================================================
    // POST: api/Designs
    //
    // Creates a new design in DRAFT status.
    // =========================================================
    [HttpPost]
    public async Task<IActionResult> CreateDesign(
        [FromBody] CreateDesignRequest request)
    {
        if (request.ProjectId <= 0)
        {
            return BadRequest(new
            {
                message = "ProjectId is required."
            });
        }

        var projectExists = await _context.Projects
            .AnyAsync(p => p.ProjectId == request.ProjectId);

        if (!projectExists)
        {
            return BadRequest(new
            {
                message = "Project not found."
            });
        }

        // -----------------------------------------------------
        // Validate ProjectService if supplied
        // -----------------------------------------------------

        if (request.ProjectServiceId.HasValue)
        {
            var serviceBelongsToProject =
                await _context.ProjectServices
                    .AnyAsync(ps =>
                        ps.ProjectServiceId ==
                        request.ProjectServiceId.Value &&
                        ps.ProjectId ==
                        request.ProjectId);

            if (!serviceBelongsToProject)
            {
                return BadRequest(new
                {
                    message =
                        "The selected ProjectService does not belong to this project."
                });
            }
        }

        // -----------------------------------------------------
        // Determine next version number
        //
        // Versioning is per ProjectService when supplied.
        // Otherwise it is per Project.
        // -----------------------------------------------------

        var versionQuery = _context.Designs
            .Where(d => d.ProjectId == request.ProjectId);

        if (request.ProjectServiceId.HasValue)
        {
            versionQuery = versionQuery.Where(d =>
                d.ProjectServiceId ==
                request.ProjectServiceId.Value);
        }
        else
        {
            versionQuery = versionQuery.Where(d =>
                d.ProjectServiceId == null);
        }

        var latestVersion =
            await versionQuery
                .Select(d => (int?)d.VersionNumber)
                .MaxAsync() ?? 0;

        var design = new Design
        {
            ProjectId = request.ProjectId,

            ProjectServiceId =
                request.ProjectServiceId,

            DesignType =
                request.DesignType.Trim(),

            VersionNumber =
                latestVersion + 1,

            DesignTitle =
                string.IsNullOrWhiteSpace(
                    request.DesignTitle)
                    ? null
                    : request.DesignTitle.Trim(),

            Description =
                string.IsNullOrWhiteSpace(
                    request.Description)
                    ? null
                    : request.Description.Trim(),

            Status = "DRAFT",

            CreatedByUserId =
                request.CreatedByUserId,

            CreatedAt = DateTime.Now
        };

        _context.Designs.Add(design);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetDesign),
            new { id = design.DesignId },
            new
            {
                message = "Design created successfully.",
                designId = design.DesignId,
                projectId = design.ProjectId,
                projectServiceId =
                    design.ProjectServiceId,
                designType = design.DesignType,
                versionNumber =
                    design.VersionNumber,
                status = design.Status,
                designTitle = design.DesignTitle
            });
    }

    // =========================================================
    // PUT: api/Designs/1/submit
    //
    // DRAFT -> SUBMITTED
    // =========================================================
    [HttpPut("{id:long}/submit")]
    public async Task<IActionResult> SubmitDesign(long id)
    {
        var design = await _context.Designs
            .FirstOrDefaultAsync(d =>
                d.DesignId == id);

        if (design == null)
        {
            return NotFound(new
            {
                message = "Design not found."
            });
        }

        if (design.Status != "DRAFT")
        {
            return BadRequest(new
            {
                message =
                    $"Design cannot be submitted from status '{design.Status}'.",
                allowedCurrentStatus = "DRAFT"
            });
        }

        design.Status = "SUBMITTED";
        design.SubmittedAt = DateTime.Now;
        design.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Design submitted successfully.",
            designId = design.DesignId,
            versionNumber = design.VersionNumber,
            status = design.Status,
            submittedAt = design.SubmittedAt
        });
    }

    // =========================================================
    // PUT: api/Designs/1/review
    //
    // SUBMITTED -> CUSTOMER_REVIEW
    // =========================================================
    [HttpPut("{id:long}/review")]
    public async Task<IActionResult> SendForCustomerReview(
        long id)
    {
        var design = await _context.Designs
            .Include(d => d.ProjectService)
                .ThenInclude(ps => ps!.Service)
            .FirstOrDefaultAsync(d =>
                d.DesignId == id);

        if (design == null)
        {
            return NotFound(new
            {
                message = "Design not found."
            });
        }

        if (design.Status != "SUBMITTED")
        {
            return BadRequest(new
            {
                message =
                    $"Design cannot be sent for customer review from status '{design.Status}'.",
                allowedCurrentStatus = "SUBMITTED"
            });
        }

        design.Status = "CUSTOMER_REVIEW";
        design.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Design sent for customer review successfully.",

            designId =
                design.DesignId,

            projectId =
                design.ProjectId,

            projectServiceId =
                design.ProjectServiceId,

            serviceName =
                design.ProjectService?.Service?.ServiceName,

            versionNumber =
                design.VersionNumber,

            status =
                design.Status,

            submittedAt =
                design.SubmittedAt
        });
    }
}

// =============================================================
// Request DTO
// =============================================================
public class CreateDesignRequest
{
    public long ProjectId { get; set; }

    public long? ProjectServiceId { get; set; }

    public string DesignType { get; set; } = null!;

    public string? DesignTitle { get; set; }

    public string? Description { get; set; }

    public long? CreatedByUserId { get; set; }
}