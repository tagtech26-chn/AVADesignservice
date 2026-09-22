using backend_api.Data;
using backend_api.DTOs;
using backend_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SiteVisitsController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public SiteVisitsController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    // GET: api/SiteVisits
    [HttpGet]
    public async Task<IActionResult> GetSiteVisits()
    {
        var visits = await _context.SiteVisits
            .AsNoTracking()
            .Include(v => v.Lead)
            .Include(v => v.Project)
            .Include(v => v.AssignedToUser)
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new
            {
                siteVisitId = v.SiteVisitId,

                leadId = v.LeadId,
                leadCode = v.Lead != null
                    ? v.Lead.LeadCode
                    : null,

                projectId = v.ProjectId,

                assignedToUserId = v.AssignedToUserId,
                assignedToUserName = v.AssignedToUser != null
                    ? v.AssignedToUser.UserName
                    : null,

                scheduledAt = v.ScheduledAt,
                completedAt = v.CompletedAt,

                status = v.Status,

                siteCondition = v.SiteCondition,
                measurements = v.Measurements,
                customerNotes = v.CustomerNotes,
                internalNotes = v.InternalNotes,

                createdAt = v.CreatedAt,
                updatedAt = v.UpdatedAt
            })
            .ToListAsync();

        return Ok(visits);
    }

// PUT: api/SiteVisits/1/status
[HttpPut("{id:long}/status")]
public async Task<IActionResult> UpdateSiteVisitStatus(
    long id,
    [FromBody] UpdateSiteVisitStatusRequest request)
{
    if (request == null || string.IsNullOrWhiteSpace(request.Status))
        return BadRequest("Status is required.");

    var requestedStatus = request.Status.Trim().ToUpperInvariant();

    var allowedStatuses = new[]
    {
        "SCHEDULED",
        "IN_PROGRESS",
        "COMPLETED",
        "CANCELLED"
    };

    if (!allowedStatuses.Contains(requestedStatus))
    {
        return BadRequest(
            $"Invalid Site Visit status '{request.Status}'. " +
            $"Allowed statuses: {string.Join(", ", allowedStatuses)}.");
    }

    var siteVisit = await _context.SiteVisits
        .Include(v => v.Lead)
        .FirstOrDefaultAsync(v => v.SiteVisitId == id);

    if (siteVisit == null)
        return NotFound($"Site Visit {id} was not found.");

    var currentStatus = siteVisit.Status.ToUpperInvariant();

    var validTransition =
        (currentStatus == "SCHEDULED" &&
            requestedStatus is "IN_PROGRESS" or "CANCELLED")
        ||
        (currentStatus == "IN_PROGRESS" &&
            requestedStatus is "COMPLETED" or "CANCELLED")
        ||
        (currentStatus == "COMPLETED" &&
            requestedStatus == "COMPLETED")
        ||
        (currentStatus == "CANCELLED" &&
            requestedStatus == "CANCELLED");

    if (!validTransition)
    {
        return BadRequest(
            $"Invalid status transition from '{siteVisit.Status}' " +
            $"to '{requestedStatus}'.");
    }

    var previousStatus = siteVisit.Status;

    siteVisit.Status = requestedStatus;
    siteVisit.UpdatedAt = DateTime.Now;

    if (requestedStatus == "COMPLETED" &&
        siteVisit.CompletedAt == null)
    {
        siteVisit.CompletedAt = DateTime.Now;
    }

    await _context.SaveChangesAsync();

    return Ok(new
    {
        siteVisitId = siteVisit.SiteVisitId,
        leadId = siteVisit.LeadId,
        projectId = siteVisit.ProjectId,
        previousStatus,
        status = siteVisit.Status,
        completedAt = siteVisit.CompletedAt,
        updatedAt = siteVisit.UpdatedAt
    });
}
    // PUT: api/SiteVisits/1/details
    [HttpPut("{id:long}/details")]
    public async Task<IActionResult> UpdateSiteVisitDetails(
        long id,
        [FromBody] UpdateSiteVisitDetailsRequest request)
    {
        if (request == null)
            return BadRequest("Request is required.");

        var siteVisit = await _context.SiteVisits
            .FirstOrDefaultAsync(v => v.SiteVisitId == id);

        if (siteVisit == null)
            return NotFound($"Site Visit {id} was not found.");

        siteVisit.SiteCondition = request.SiteCondition;
        siteVisit.Measurements = request.Measurements;
        siteVisit.CustomerNotes = request.CustomerNotes;
        siteVisit.InternalNotes = request.InternalNotes;
        siteVisit.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            siteVisitId = siteVisit.SiteVisitId,
            leadId = siteVisit.LeadId,
            projectId = siteVisit.ProjectId,
            status = siteVisit.Status,
            siteCondition = siteVisit.SiteCondition,
            measurements = siteVisit.Measurements,
            customerNotes = siteVisit.CustomerNotes,
            internalNotes = siteVisit.InternalNotes,
            updatedAt = siteVisit.UpdatedAt
        });
    }

    // GET: api/SiteVisits/5
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetSiteVisit(long id)
    {
        var visit = await _context.SiteVisits
            .AsNoTracking()
            .Include(v => v.Lead)
            .Include(v => v.Project)
            .Include(v => v.AssignedToUser)
            .Where(v => v.SiteVisitId == id)
            .Select(v => new
            {
                siteVisitId = v.SiteVisitId,

                leadId = v.LeadId,
                leadCode = v.Lead != null
                    ? v.Lead.LeadCode
                    : null,
                customerName = v.Lead != null
                    ? v.Lead.CustomerName
                    : null,

                projectId = v.ProjectId,

                assignedToUserId = v.AssignedToUserId,
                assignedToUserName = v.AssignedToUser != null
                    ? v.AssignedToUser.UserName
                    : null,

                scheduledAt = v.ScheduledAt,
                completedAt = v.CompletedAt,

                status = v.Status,

                siteCondition = v.SiteCondition,
                measurements = v.Measurements,
                customerNotes = v.CustomerNotes,
                internalNotes = v.InternalNotes,

                createdAt = v.CreatedAt,
                updatedAt = v.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (visit == null)
            return NotFound($"Site Visit {id} was not found.");

        return Ok(visit);
    }

    // POST: api/SiteVisits
    [HttpPost]
    public async Task<IActionResult> CreateSiteVisit(
        [FromBody] CreateSiteVisitRequest request)
    {
        if (request == null)
            return BadRequest("Request is required.");

        if (request.LeadId == null && request.ProjectId == null)
            return BadRequest(
                "Either LeadId or ProjectId is required.");

        if (request.LeadId != null)
        {
            var leadExists = await _context.Leads
                .AnyAsync(l => l.LeadId == request.LeadId.Value);

            if (!leadExists)
                return BadRequest(
                    $"Lead {request.LeadId.Value} was not found.");
        }

        if (request.ProjectId != null)
        {
            var projectExists = await _context.Projects
                .AnyAsync(p => p.ProjectId == request.ProjectId.Value);

            if (!projectExists)
                return BadRequest(
                    $"Project {request.ProjectId.Value} was not found.");
        }

        if (request.AssignedToUserId != null)
        {
            var userExists = await _context.Users
                .AnyAsync(u =>
                    u.UserId == request.AssignedToUserId.Value &&
                    u.IsActive);

            if (!userExists)
                return BadRequest(
                    $"Active user {request.AssignedToUserId.Value} was not found.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            Lead? lead = null;

            if (request.LeadId.HasValue)
            {
                lead = await _context.Leads
                    .FirstOrDefaultAsync(l => l.LeadId == request.LeadId.Value);

                if (lead == null)
                {
                    return BadRequest(
                        $"Lead {request.LeadId.Value} was not found.");
                }

                var leadStatus = lead.Status.ToUpperInvariant();

                if (leadStatus is not "CONTACTED"
                    and not "SITE_VISIT_REQUIRED"
                    and not "SITE_VISIT_SCHEDULED")
                {
                    return BadRequest(
                        $"Lead {lead.LeadCode} cannot be scheduled for a Site Visit from status '{lead.Status}'.");
                }

                var activeVisitExists = await _context.SiteVisits
                    .AnyAsync(v =>
                        v.LeadId == lead.LeadId &&
                        (v.Status == "SCHEDULED" || v.Status == "IN_PROGRESS"));

                if (activeVisitExists)
                {
                    return Conflict(
                        $"Lead {lead.LeadCode} already has an active Site Visit.");
                }
            }

            var siteVisit = new SiteVisit
            {
                LeadId = request.LeadId,
                ProjectId = request.ProjectId,
                AssignedToUserId = request.AssignedToUserId,

                ScheduledAt = request.ScheduledAt,

                Status = string.IsNullOrWhiteSpace(request.Status)
                    ? "SCHEDULED"
                    : request.Status.Trim().ToUpperInvariant(),

                SiteCondition = request.SiteCondition,
                Measurements = request.Measurements,
                CustomerNotes = request.CustomerNotes,
                InternalNotes = request.InternalNotes,

                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.SiteVisits.Add(siteVisit);

            // Scheduling the Site Visit also moves the Lead into
            // SITE_VISIT_SCHEDULED. The two changes are committed together.
            if (lead != null)
            {
                lead.Status = "SITE_VISIT_SCHEDULED";
                lead.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction(
                nameof(GetSiteVisit),
                new { id = siteVisit.SiteVisitId },
                new
                {
                    siteVisitId = siteVisit.SiteVisitId,
                    leadId = siteVisit.LeadId,
                    projectId = siteVisit.ProjectId,
                    assignedToUserId = siteVisit.AssignedToUserId,
                    scheduledAt = siteVisit.ScheduledAt,
                    status = siteVisit.Status,
                    createdAt = siteVisit.CreatedAt
                });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}