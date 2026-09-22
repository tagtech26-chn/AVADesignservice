using backend_api.Data;
using backend_api.DTOs;
using backend_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeadsController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public LeadsController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    // GET: api/Leads
    [HttpGet]
    public async Task<IActionResult> GetLeads()
    {
        var leads = await _context.Leads
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new
            {
                leadId = l.LeadId,
                leadCode = l.LeadCode,
                customerId = l.CustomerId,
                projectId = l.ProjectId,
                leadSource = l.LeadSource,
                customerName = l.CustomerName,
                companyName = l.CompanyName,
                email = l.Email,
                mobile = l.Mobile,
                projectTypeId = l.ProjectTypeId,
                projectType = l.ProjectType != null
                    ? l.ProjectType.ProjectTypeName
                    : null,
                location = l.Location,
                approximateArea = l.ApproximateArea,
                areaUnit = l.AreaUnit,
                requirement = l.Requirement,
                status = l.Status,
                assignedToUserId = l.AssignedToUserId,
                createdAt = l.CreatedAt,
                updatedAt = l.UpdatedAt,

                services = l.LeadServices
                    .OrderBy(ls => ls.LeadServiceId)
                    .Select(ls => new
                    {
                        leadServiceId = ls.LeadServiceId,
                        serviceId = ls.ServiceId,
                        serviceName = ls.Service.ServiceName,
                        serviceOptionId = ls.ServiceOptionId,
                        optionCode = ls.ServiceOption != null
                            ? ls.ServiceOption.OptionCode
                            : null,
                        optionName = ls.ServiceOption != null
                            ? ls.ServiceOption.OptionName
                            : null,
                        requirement = ls.Requirement,
                        createdAt = ls.CreatedAt
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(leads);
    }

    [HttpPost("{id:long}/convert-to-project")]
    public async Task<IActionResult> ConvertToProject(
    long id,
    [FromBody] ConvertLeadToProjectRequest request)
    {
        if (request == null)
            return BadRequest("Conversion request is required.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // ---------------------------------------------------------
            // 1. Load Lead
            // ---------------------------------------------------------
            var lead = await _context.Leads
                .Include(l => l.LeadServices)
                    .ThenInclude(ls => ls.Service)
                .Include(l => l.LeadServices)
                    .ThenInclude(ls => ls.ServiceOption)
                .FirstOrDefaultAsync(l => l.LeadId == id);

            if (lead == null)
                return NotFound($"Lead {id} was not found.");

            // ---------------------------------------------------------
            // 2. Lead must be Qualified
            // ---------------------------------------------------------
            if (!string.Equals(
                    lead.Status,
                    "QUALIFIED",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    $"Lead {id} cannot be converted because its current status is '{lead.Status}'. " +
                    "Only QUALIFIED leads can be converted to a Project.");
            }

            // ---------------------------------------------------------
            // 3. Prevent duplicate conversion
            // ---------------------------------------------------------
            var existingProject = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.LeadId == lead.LeadId);

            if (existingProject != null)
            {
                return Conflict(new
                {
                    message = "This Lead has already been converted to a Project.",
                    leadId = lead.LeadId,
                    projectId = existingProject.ProjectId,
                    projectCode = existingProject.ProjectCode
                });
            }

            // ---------------------------------------------------------
            // 4. Find approved quotation
            // ---------------------------------------------------------
            Quotation? quotation;

            if (request.ApprovedQuotationId.HasValue)
            {
                quotation = await _context.Quotations
                    .FirstOrDefaultAsync(q =>
                        q.QuotationId == request.ApprovedQuotationId.Value &&
                        q.LeadId == lead.LeadId &&
                        q.Status == "APPROVED");

                if (quotation == null)
                {
                    return BadRequest(
                        $"Approved quotation {request.ApprovedQuotationId.Value} " +
                        $"was not found for Lead {lead.LeadId}.");
                }
            }
            else
            {
                quotation = await _context.Quotations
                    .Where(q =>
                        q.LeadId == lead.LeadId &&
                        q.Status == "APPROVED")
                    .OrderByDescending(q => q.ApprovedAt)
                    .ThenByDescending(q => q.QuotationId)
                    .FirstOrDefaultAsync();

                if (quotation == null)
                {
                    return BadRequest(
                        $"Lead {lead.LeadId} does not have an approved quotation.");
                }
            }

            // ---------------------------------------------------------
            // 5. Resolve / create Customer
            // ---------------------------------------------------------
            Customer? customer = null;

            if (lead.CustomerId.HasValue)
            {
                customer = await _context.Customers
                    .FirstOrDefaultAsync(c =>
                        c.CustomerId == lead.CustomerId.Value &&
                        c.IsActive);

                if (customer == null)
                {
                    return BadRequest(
                        $"Customer {lead.CustomerId.Value} linked to Lead {lead.LeadId} " +
                        "was not found or is inactive.");
                }
            }

            // Try to find an existing customer by mobile.
            if (customer == null &&
                !string.IsNullOrWhiteSpace(lead.Mobile))
            {
                var mobile = lead.Mobile.Trim();

                customer = await _context.Customers
                    .FirstOrDefaultAsync(c =>
                        c.IsActive &&
                        c.Mobile == mobile);
            }

            // Try email if mobile did not find a customer.
            if (customer == null &&
                !string.IsNullOrWhiteSpace(lead.Email))
            {
                var email = lead.Email.Trim();

                customer = await _context.Customers
                    .FirstOrDefaultAsync(c =>
                        c.IsActive &&
                        c.Email == email);
            }

            // Create customer if none exists.
            if (customer == null)
            {
                var customerType =
                    string.IsNullOrWhiteSpace(lead.CompanyName)
                        ? "INDIVIDUAL"
                        : "COMPANY";

                customer = new Customer
                {
                    CustomerType = customerType,
                    CustomerName = lead.CustomerName.Trim(),
                    CompanyName = string.IsNullOrWhiteSpace(lead.CompanyName)
                        ? null
                        : lead.CompanyName.Trim(),
                    Email = string.IsNullOrWhiteSpace(lead.Email)
                        ? null
                        : lead.Email.Trim(),
                    Mobile = string.IsNullOrWhiteSpace(lead.Mobile)
                        ? null
                        : lead.Mobile.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Customers.Add(customer);

                await _context.SaveChangesAsync();
            }

            // Link customer back to Lead if not already linked.
            if (lead.CustomerId != customer.CustomerId)
            {
                lead.CustomerId = customer.CustomerId;
                lead.UpdatedAt = DateTime.Now;
            }

            // ---------------------------------------------------------
            // 6. Validate Project Type
            // ---------------------------------------------------------
            if (!lead.ProjectTypeId.HasValue)
            {
                return BadRequest(
                    $"Lead {lead.LeadId} does not have a Project Type.");
            }

            var projectTypeExists = await _context.ProjectTypes
                .AnyAsync(pt =>
                    pt.ProjectTypeId == lead.ProjectTypeId.Value &&
                    pt.IsActive);

            if (!projectTypeExists)
            {
                return BadRequest(
                    $"Project Type {lead.ProjectTypeId.Value} was not found or is inactive.");
            }

            // ---------------------------------------------------------
            // 7. Create Project
            // ---------------------------------------------------------
            var projectCode =
                $"PRJ-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

            var projectName = string.IsNullOrWhiteSpace(request.ProjectName)
                ? (
                    !string.IsNullOrWhiteSpace(lead.CompanyName)
                        ? $"{lead.CompanyName} - {lead.CustomerName} Project"
                        : $"{lead.CustomerName} Project"
                  )
                : request.ProjectName.Trim();

            var project = new Project
            {
                ProjectCode = projectCode,
                CustomerId = customer.CustomerId,
                ProjectTypeId = lead.ProjectTypeId.Value,
                ProjectName = projectName,
                Description = lead.Requirement,
                SiteAddress = lead.Location,
                ApproximateArea = lead.ApproximateArea,
                AreaUnit = lead.AreaUnit,
                ExpectedStartDate = request.ExpectedStartDate,
                ExpectedEndDate = request.ExpectedEndDate,
                Status = "ENQUIRY",
                CreatedByUserId = request.CreatedByUserId,
                CreatedAt = DateTime.Now,
                LeadId = lead.LeadId,
                ApprovedQuotationId = quotation.QuotationId
            };

            _context.Projects.Add(project);

            await _context.SaveChangesAsync();

            // ---------------------------------------------------------
            // 8. Copy Lead Services → Project Services
            // ---------------------------------------------------------
            foreach (var leadService in lead.LeadServices)
            {
                var projectService = new ProjectService
                {
                    ProjectId = project.ProjectId,
                    ServiceId = leadService.ServiceId,
                    ServiceOptionId = leadService.ServiceOptionId,
                    CustomerRequirement = leadService.Requirement,
                    Status = "REQUESTED",
                    CreatedAt = DateTime.Now
                };

                _context.ProjectServices.Add(projectService);
            }

            await _context.SaveChangesAsync();

            // ---------------------------------------------------------
            // 9. Mark Lead as Converted
            // ---------------------------------------------------------
            lead.Status = "CONVERTED";
            lead.ProjectId = project.ProjectId;
            lead.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // ---------------------------------------------------------
            // 10. Commit
            // ---------------------------------------------------------
            await transaction.CommitAsync();

            // ---------------------------------------------------------
            // 11. Return result
            // ---------------------------------------------------------
            return Ok(new
            {
                message = "Lead converted to Project successfully.",
                leadId = lead.LeadId,
                leadCode = lead.LeadCode,
                customerId = customer.CustomerId,
                customerName = customer.CustomerName,
                projectId = project.ProjectId,
                projectCode = project.ProjectCode,
                projectName = project.ProjectName,
                projectStatus = project.Status,
                approvedQuotationId = quotation.QuotationId,
                quotationNumber = quotation.QuotationNumber,
                quotationGrandTotal = quotation.GrandTotal,
                serviceCount = lead.LeadServices.Count,
                leadStatus = lead.Status
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // PUT: api/Leads/1/status
    [HttpPut("{id:long}/status")]
public async Task<IActionResult> UpdateLeadStatus(
    long id,
    [FromBody] UpdateLeadStatusRequest request)
{
    if (request == null ||
        string.IsNullOrWhiteSpace(request.StatusCode))
    {
        return BadRequest("StatusCode is required.");
    }

    var statusCode = request.StatusCode.Trim().ToUpperInvariant();

    var status = await _context.LeadStatuses
        .AsNoTracking()
        .FirstOrDefaultAsync(s =>
            s.StatusCode == statusCode &&
            s.IsActive);

    if (status == null)
    {
        return BadRequest(
            $"Invalid or inactive lead status: {statusCode}");
    }

    var lead = await _context.Leads
        .FirstOrDefaultAsync(l => l.LeadId == id);

    if (lead == null)
        return NotFound($"Lead {id} was not found.");

    // Prevent unnecessary updates
    if (lead.Status == status.StatusCode)
    {
        return Ok(new
        {
            leadId = lead.LeadId,
            leadCode = lead.LeadCode,
            statusCode = status.StatusCode,
            statusName = status.StatusName,
            message = "Lead is already in this status."
        });
    }

    // A lead cannot be manually marked Converted
    // unless it has been linked to a Project.
   // A lead cannot be manually marked Converted
// unless it has been linked to a Project.
if (status.StatusCode == "CONVERTED" &&
    !lead.ProjectId.HasValue)
{
    return BadRequest(
        "A lead cannot be marked as Converted until it is linked to a project.");
}

// A lead can be moved to SITE_VISIT_SCHEDULED only when
// an actual active Site Visit has been created.
if (status.StatusCode == "SITE_VISIT_SCHEDULED")
{
    var scheduledSiteVisitExists = await _context.SiteVisits
        .AnyAsync(v =>
            v.LeadId == lead.LeadId &&
            (v.Status == "SCHEDULED" || v.Status == "IN_PROGRESS"));

    if (!scheduledSiteVisitExists)
    {
        return BadRequest(
            "A lead cannot be marked as Site Visit Scheduled until a Site Visit has been scheduled.");
    }
}

// A lead can be qualified only after a completed Site Visit.
if (status.StatusCode == "QUALIFIED")
{
    var completedSiteVisitExists = await _context.SiteVisits
        .AnyAsync(v =>
            v.LeadId == lead.LeadId &&
            v.Status == "COMPLETED");

    if (!completedSiteVisitExists)
    {
        return BadRequest(
            "A lead cannot be marked as Qualified until a Site Visit has been completed.");
    }
}

    var previousStatus = lead.Status;

    lead.Status = status.StatusCode;
    lead.UpdatedAt = DateTime.Now;

    await _context.SaveChangesAsync();

    return Ok(new
    {
        leadId = lead.LeadId,
        leadCode = lead.LeadCode,
        previousStatus,
        statusCode = status.StatusCode,
        statusName = status.StatusName,
        updatedAt = lead.UpdatedAt
    });
}

    // GET: api/Leads/5
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetLead(long id)
    {
        var lead = await _context.Leads
            .AsNoTracking()
            .Where(l => l.LeadId == id)
            .Select(l => new
            {
                leadId = l.LeadId,
                leadCode = l.LeadCode,
                customerId = l.CustomerId,
                projectId = l.ProjectId,
                leadSource = l.LeadSource,
                customerName = l.CustomerName,
                companyName = l.CompanyName,
                email = l.Email,
                mobile = l.Mobile,
                projectTypeId = l.ProjectTypeId,
                projectType = l.ProjectType != null
                    ? l.ProjectType.ProjectTypeName
                    : null,
                location = l.Location,
                approximateArea = l.ApproximateArea,
                areaUnit = l.AreaUnit,
                requirement = l.Requirement,
                status = l.Status,
                assignedToUserId = l.AssignedToUserId,
                createdAt = l.CreatedAt,
                updatedAt = l.UpdatedAt,

                services = l.LeadServices
                    .OrderBy(ls => ls.LeadServiceId)
                    .Select(ls => new
                    {
                        leadServiceId = ls.LeadServiceId,
                        serviceId = ls.ServiceId,
                        serviceName = ls.Service.ServiceName,
                        serviceOptionId = ls.ServiceOptionId,
                        optionCode = ls.ServiceOption != null
                            ? ls.ServiceOption.OptionCode
                            : null,
                        optionName = ls.ServiceOption != null
                            ? ls.ServiceOption.OptionName
                            : null,
                        requirement = ls.Requirement,
                        createdAt = ls.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (lead == null)
            return NotFound();

        return Ok(lead);
    }

// PUT: api/Leads/1/assignment
[HttpPut("{id:long}/assignment")]
public async Task<IActionResult> AssignLead(
    long id,
    [FromBody] AssignLeadRequest request)
{
    if (request == null || request.AssignedToUserId <= 0)
        return BadRequest("AssignedToUserId is required.");

    var lead = await _context.Leads
        .FirstOrDefaultAsync(l => l.LeadId == id);

    if (lead == null)
        return NotFound($"Lead {id} was not found.");

    // Only active users can receive Leads.
    var user = await _context.Users
        .AsNoTracking()
        .Where(u =>
            u.UserId == request.AssignedToUserId &&
            u.IsActive)
        .Select(u => new
        {
            u.UserId,
            u.UserName,
            u.Email,
            Roles = u.UserRoles
                .Where(ur => ur.Role.IsActive)
                .Select(ur => ur.Role.RoleName)
                .ToList()
        })
        .FirstOrDefaultAsync();

    if (user == null)
    {
        return BadRequest(
            $"Active user {request.AssignedToUserId} was not found.");
    }

    // Roles allowed to handle Leads.
    var allowedRoles = new[]
    {
        "Super Admin",
        "Admin",
        "Sales",
        "Designer",
        "Project Coordinator"
    };

    var hasAllowedRole = user.Roles
        .Any(role => allowedRoles.Contains(role));

    if (!hasAllowedRole)
    {
        return BadRequest(
            $"User '{user.UserName}' does not have a role permitted to handle Leads.");
    }

    var previousAssignedToUserId = lead.AssignedToUserId;

    lead.AssignedToUserId = user.UserId;
    lead.UpdatedAt = DateTime.Now;

    await _context.SaveChangesAsync();

    return Ok(new
    {
        leadId = lead.LeadId,
        leadCode = lead.LeadCode,
        previousAssignedToUserId,
        assignedToUserId = user.UserId,
        assignedToUserName = user.UserName,
        assignedToUserEmail = user.Email,
        assignedToRoles = user.Roles,
        updatedAt = lead.UpdatedAt
    });
}

    // POST: api/Leads
    // Creates Lead + LeadServices in one transaction
    [HttpPost]
    public async Task<IActionResult> CreateLead(
        [FromBody] CreateLeadRequest request)
    {
        if (request == null)
            return BadRequest("Request is required.");

        if (string.IsNullOrWhiteSpace(request.CustomerName))
            return BadRequest("Customer name is required.");

        if (request.ProjectTypeId.HasValue)
        {
            var projectTypeExists = await _context.ProjectTypes
                .AnyAsync(p =>
                    p.ProjectTypeId == request.ProjectTypeId.Value &&
                    p.IsActive);

            if (!projectTypeExists)
                return BadRequest("Invalid project type.");
        }

        // Remove duplicate service selections
        var duplicateServices = request.Services
            .GroupBy(x => new
            {
                x.ServiceId,
                x.ServiceOptionId
            })
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateServices.Any())
            return BadRequest(
                "The same service and option cannot be selected more than once.");

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // Validate all selected services
            foreach (var selected in request.Services)
            {
                var service = await _context.Services
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s =>
                        s.ServiceId == selected.ServiceId &&
                        s.IsActive);

                if (service == null)
                {
                    await transaction.RollbackAsync();

                    return BadRequest(
                        $"Invalid or inactive service: {selected.ServiceId}");
                }

                // If an option was selected, make sure it belongs
                // to this service and is available to customers.
                if (selected.ServiceOptionId.HasValue)
                {
                    var option = await _context.ServiceOptions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(o =>
                            o.ServiceOptionId ==
                            selected.ServiceOptionId.Value &&
                            o.ServiceId == selected.ServiceId &&
                            o.IsActive &&
                            o.IsAvailableToCustomer);

                    if (option == null)
                    {
                        await transaction.RollbackAsync();

                        return BadRequest(
                            $"Invalid service option {selected.ServiceOptionId} " +
                            $"for service {selected.ServiceId}.");
                    }
                }
            }

            // Generate a unique LeadCode
            string leadCode;

            do
            {
                leadCode =
                    $"LEAD-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
            }
            while (await _context.Leads
                .AnyAsync(l => l.LeadCode == leadCode));

            var lead = new Lead
            {
                LeadCode = leadCode,
                LeadSource = request.LeadSource,
                CustomerName = request.CustomerName.Trim(),
                CompanyName = request.CompanyName,
                Email = request.Email,
                Mobile = request.Mobile,
                ProjectTypeId = request.ProjectTypeId,
                Location = request.Location,
                ApproximateArea = request.ApproximateArea,
                AreaUnit = request.AreaUnit,
                Requirement = request.Requirement,
                Status = "New",
                CreatedAt = DateTime.Now
            };

            _context.Leads.Add(lead);

            await _context.SaveChangesAsync();

            // Create LeadServices
            foreach (var selected in request.Services)
            {
                var leadService = new LeadService
                {
                    LeadId = lead.LeadId,
                    ServiceId = selected.ServiceId,
                    ServiceOptionId = selected.ServiceOptionId,
                    Requirement = selected.Requirement,
                    CreatedAt = DateTime.Now
                };

                _context.LeadServices.Add(leadService);
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return CreatedAtAction(
                nameof(GetLead),
                new { id = lead.LeadId },
                new
                {
                    leadId = lead.LeadId,
                    leadCode = lead.LeadCode,
                    status = lead.Status,
                    createdAt = lead.CreatedAt,
                    serviceCount = request.Services.Count
                });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // PUT: api/Leads/5
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateLead(
        long id,
        [FromBody] Lead lead)
    {
        if (id != lead.LeadId)
            return BadRequest("Lead ID mismatch.");

        var existingLead = await _context.Leads
            .FirstOrDefaultAsync(l => l.LeadId == id);

        if (existingLead == null)
            return NotFound();

        existingLead.LeadCode = lead.LeadCode;
        existingLead.CustomerId = lead.CustomerId;
        existingLead.ProjectId = lead.ProjectId;
        existingLead.LeadSource = lead.LeadSource;
        existingLead.CustomerName = lead.CustomerName;
        existingLead.CompanyName = lead.CompanyName;
        existingLead.Email = lead.Email;
        existingLead.Mobile = lead.Mobile;
        existingLead.ProjectTypeId = lead.ProjectTypeId;
        existingLead.Location = lead.Location;
        existingLead.ApproximateArea = lead.ApproximateArea;
        existingLead.AreaUnit = lead.AreaUnit;
        existingLead.Requirement = lead.Requirement;
        existingLead.Status = lead.Status;
        existingLead.AssignedToUserId = lead.AssignedToUserId;
        existingLead.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(existingLead);
    }
}