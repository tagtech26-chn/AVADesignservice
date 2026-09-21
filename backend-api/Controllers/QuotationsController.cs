using backend_api.Data;
using backend_api.DTOs;
using backend_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuotationsController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public QuotationsController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // GET: api/Quotations
    // ============================================================

    [HttpGet]
    public async Task<IActionResult> GetQuotations()
    {
        var quotations = await _context.Quotations
            .AsNoTracking()
            .Include(q => q.Lead)
            .Include(q => q.Project)
            .Include(q => q.CreatedByUser)
            .Include(q => q.QuotationItems)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new
            {
                quotationId = q.QuotationId,
                quotationNumber = q.QuotationNumber,

                leadId = q.LeadId,

                leadCode = q.Lead != null
                    ? q.Lead.LeadCode
                    : null,

                customerName = q.Lead != null
                    ? q.Lead.CustomerName
                    : null,

                projectId = q.ProjectId,

                revisionNumber = q.RevisionNumber,

                quotationDate = q.QuotationDate,

                validUntil = q.ValidUntil,

                subTotal = q.SubTotal,

                discountAmount = q.DiscountAmount,

                taxAmount = q.TaxAmount,

                grandTotal = q.GrandTotal,

                status = q.Status,

                notes = q.Notes,

                createdByUserId = q.CreatedByUserId,

                createdByUserName = q.CreatedByUser != null
                    ? q.CreatedByUser.UserName
                    : null,

                sentAt = q.SentAt,

                approvedAt = q.ApprovedAt,

                createdAt = q.CreatedAt,

                updatedAt = q.UpdatedAt,

                itemCount = q.QuotationItems.Count
            })
            .ToListAsync();

        return Ok(quotations);
    }

    // ============================================================
    // GET: api/Quotations/1
    // ============================================================

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetQuotation(long id)
    {
        var quotation = await _context.Quotations
            .AsNoTracking()
            .Include(q => q.Lead)
            .Include(q => q.Project)
            .Include(q => q.CreatedByUser)
            .Include(q => q.QuotationItems)
            .Where(q => q.QuotationId == id)
            .Select(q => new
            {
                quotationId = q.QuotationId,
                quotationNumber = q.QuotationNumber,

                leadId = q.LeadId,

                leadCode = q.Lead != null
                    ? q.Lead.LeadCode
                    : null,

                customerName = q.Lead != null
                    ? q.Lead.CustomerName
                    : null,

                projectId = q.ProjectId,

                revisionNumber = q.RevisionNumber,

                quotationDate = q.QuotationDate,

                validUntil = q.ValidUntil,

                subTotal = q.SubTotal,

                discountAmount = q.DiscountAmount,

                taxAmount = q.TaxAmount,

                grandTotal = q.GrandTotal,

                status = q.Status,

                notes = q.Notes,

                createdByUserId = q.CreatedByUserId,

                createdByUserName = q.CreatedByUser != null
                    ? q.CreatedByUser.UserName
                    : null,

                sentAt = q.SentAt,

                approvedAt = q.ApprovedAt,

                createdAt = q.CreatedAt,

                updatedAt = q.UpdatedAt,

                items = q.QuotationItems
                    .OrderBy(i => i.DisplayOrder)
                    .ThenBy(i => i.QuotationItemId)
                    .Select(i => new
                    {
                        quotationItemId = i.QuotationItemId,
                        projectServiceId = i.ProjectServiceId,
                        itemType = i.ItemType,
                        description = i.Description,
                        quantity = i.Quantity,
                        unit = i.Unit,
                        unitPrice = i.UnitPrice,
                        discountAmount = i.DiscountAmount,
                        taxPercent = i.TaxPercent,
                        taxAmount = i.TaxAmount,
                        lineTotal = i.LineTotal,
                        displayOrder = i.DisplayOrder,
                        notes = i.Notes
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (quotation == null)
            return NotFound($"Quotation {id} was not found.");

        return Ok(quotation);
    }

    // ============================================================
    // PUT: api/Quotations/{id}/status
    // ============================================================

    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(
        long id,
        [FromBody] UpdateQuotationStatusRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Status))
            return BadRequest("Status is required.");

        var quotation = await _context.Quotations
            .FirstOrDefaultAsync(q => q.QuotationId == id);

        if (quotation == null)
            return NotFound($"Quotation {id} was not found.");

        var newStatus = request.Status.Trim().ToUpperInvariant();
        var currentStatus = quotation.Status.ToUpperInvariant();

        var allowedTransitions = new Dictionary<string, string[]>
        {
            ["DRAFT"] = new[]
            {
                "SENT",
                "CANCELLED"
            },

            ["SENT"] = new[]
            {
                "CUSTOMER_REVIEW",
                "CANCELLED"
            },

            ["CUSTOMER_REVIEW"] = new[]
            {
                "APPROVED",
                "REVISION_REQUESTED",
                "REJECTED"
            },

            ["REVISION_REQUESTED"] = new[]
            {
                "DRAFT",
                "CANCELLED"
            },

            ["APPROVED"] = Array.Empty<string>(),

            ["REJECTED"] = Array.Empty<string>(),

            ["EXPIRED"] = Array.Empty<string>(),

            ["CANCELLED"] = Array.Empty<string>()
        };

        if (!allowedTransitions.ContainsKey(currentStatus))
        {
            return BadRequest(
                $"Quotation {id} has an unsupported current status '{quotation.Status}'.");
        }

        if (!allowedTransitions[currentStatus].Contains(newStatus))
        {
            return BadRequest(
                $"Quotation cannot be changed from '{quotation.Status}' to '{newStatus}'.");
        }

        var previousStatus = quotation.Status;

        quotation.Status = newStatus;
        quotation.UpdatedAt = DateTime.Now;

        if (newStatus == "SENT" && quotation.SentAt == null)
        {
            quotation.SentAt = DateTime.Now;
        }

        if (newStatus == "APPROVED" && quotation.ApprovedAt == null)
        {
            quotation.ApprovedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            quotationId = quotation.QuotationId,
            quotationNumber = quotation.QuotationNumber,
            previousStatus,
            status = quotation.Status,
            sentAt = quotation.SentAt,
            approvedAt = quotation.ApprovedAt,
            updatedAt = quotation.UpdatedAt
        });
    }

    // ============================================================
    // POST: api/Quotations
    // ============================================================

    [HttpPost]
    public async Task<IActionResult> CreateQuotation(
        [FromBody] CreateQuotationRequest request)
    {
        if (request == null)
            return BadRequest("Request is required.");

        if (request.LeadId == null && request.ProjectId == null)
            return BadRequest(
                "Either LeadId or ProjectId is required.");

        if (request.LeadId != null)
        {
            var lead = await _context.Leads
                .FirstOrDefaultAsync(l => l.LeadId == request.LeadId.Value);

            if (lead == null)
                return BadRequest(
                    $"Lead {request.LeadId.Value} was not found.");

            if (lead.Status != "QUALIFIED")
            {
                return BadRequest(
                    $"Lead {lead.LeadId} must be Qualified before creating a quotation.");
            }
        }

        if (request.ProjectId != null)
        {
            var projectExists = await _context.Projects
                .AnyAsync(p => p.ProjectId == request.ProjectId.Value);

            if (!projectExists)
                return BadRequest(
                    $"Project {request.ProjectId.Value} was not found.");
        }

        if (request.Items == null || request.Items.Count == 0)
            return BadRequest("At least one quotation item is required.");

        if (request.CreatedByUserId != null)
        {
            var userExists = await _context.Users
                .AnyAsync(u =>
                    u.UserId == request.CreatedByUserId.Value &&
                    u.IsActive);

            if (!userExists)
                return BadRequest(
                    $"Active user {request.CreatedByUserId.Value} was not found.");
        }

        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ItemType))
                return BadRequest("Each quotation item requires ItemType.");

            if (string.IsNullOrWhiteSpace(item.Description))
                return BadRequest("Each quotation item requires Description.");

            if (item.Quantity.HasValue && item.Quantity.Value < 0)
                return BadRequest("Quantity cannot be negative.");

            if (item.UnitPrice < 0)
                return BadRequest("UnitPrice cannot be negative.");

            if (item.DiscountAmount < 0)
                return BadRequest("Item discount cannot be negative.");

            if (item.TaxPercent < 0)
                return BadRequest("TaxPercent cannot be negative.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var quotationNumber =
                $"QUO-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

            while (await _context.Quotations
                .AnyAsync(q => q.QuotationNumber == quotationNumber))
            {
                quotationNumber =
                    $"QUO-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
            }

            decimal subTotal = 0;
            decimal totalItemDiscount = 0;
            decimal totalItemTax = 0;

            var quotation = new Quotation
            {
                QuotationNumber = quotationNumber,

                LeadId = request.LeadId,

                ProjectId = request.ProjectId,

                RevisionNumber = 1,

                QuotationDate =
                    request.QuotationDate ??
                    DateOnly.FromDateTime(DateTime.Today),

                ValidUntil = request.ValidUntil,

                Status = "DRAFT",

                DiscountAmount = request.DiscountAmount,

                TaxAmount = request.TaxAmount,

                Notes = request.Notes,

                CreatedByUserId = request.CreatedByUserId,

                CreatedAt = DateTime.Now,

                UpdatedAt = DateTime.Now
            };

            _context.Quotations.Add(quotation);

            await _context.SaveChangesAsync();

            foreach (var item in request.Items)
            {
                var quantity = item.Quantity ?? 1;

                var grossAmount =
                    quantity * item.UnitPrice;

                var lineDiscount =
                    item.DiscountAmount;

                var taxableAmount =
                    Math.Max(
                        0,
                        grossAmount - lineDiscount);

                var lineTax =
                    Math.Round(
                        taxableAmount *
                        item.TaxPercent /
                        100m,
                        2);

                var lineTotal =
                    taxableAmount + lineTax;

                subTotal += grossAmount;

                totalItemDiscount += lineDiscount;

                totalItemTax += lineTax;

                var quotationItem = new QuotationItem
                {
                    QuotationId =
                        quotation.QuotationId,

                    ProjectServiceId =
                        item.ProjectServiceId,

                    ItemType =
                        item.ItemType.Trim(),

                    Description =
                        item.Description.Trim(),

                    Quantity =
                        quantity,

                    Unit =
                        item.Unit,

                    UnitPrice =
                        item.UnitPrice,

                    DiscountAmount =
                        lineDiscount,

                    TaxPercent =
                        item.TaxPercent,

                    TaxAmount =
                        lineTax,

                    LineTotal =
                        lineTotal,

                    DisplayOrder =
                        item.DisplayOrder,

                    Notes =
                        item.Notes
                };

                _context.QuotationItems.Add(
                    quotationItem);
            }

            await _context.SaveChangesAsync();

            quotation.SubTotal =
                subTotal;

            quotation.DiscountAmount =
                totalItemDiscount +
                request.DiscountAmount;

            quotation.TaxAmount =
                totalItemTax +
                request.TaxAmount;

            quotation.GrandTotal =
                subTotal -
                quotation.DiscountAmount +
                quotation.TaxAmount;

            if (quotation.GrandTotal < 0)
                quotation.GrandTotal = 0;

            quotation.UpdatedAt =
                DateTime.Now;

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return CreatedAtAction(
                nameof(GetQuotation),
                new
                {
                    id = quotation.QuotationId
                },
                new
                {
                    quotationId =
                        quotation.QuotationId,

                    quotationNumber =
                        quotation.QuotationNumber,

                    leadId =
                        quotation.LeadId,

                    projectId =
                        quotation.ProjectId,

                    revisionNumber =
                        quotation.RevisionNumber,

                    status =
                        quotation.Status,

                    subTotal =
                        quotation.SubTotal,

                    discountAmount =
                        quotation.DiscountAmount,

                    taxAmount =
                        quotation.TaxAmount,

                    grandTotal =
                        quotation.GrandTotal,

                    quotationDate =
                        quotation.QuotationDate,

                    validUntil =
                        quotation.ValidUntil,

                    itemCount =
                        request.Items.Count
                });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}