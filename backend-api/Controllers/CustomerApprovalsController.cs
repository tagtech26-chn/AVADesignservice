using backend_api.Data;
using backend_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerApprovalsController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public CustomerApprovalsController(
        AVADesignServicesDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // GET: api/CustomerApprovals/project/1
    // =========================================================
    [HttpGet("project/{projectId:long}")]
    public async Task<IActionResult> GetProjectApprovals(
        long projectId)
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

        var approvals = await _context.CustomerApprovals
            .Where(a => a.ProjectId == projectId)
            .Include(a => a.Design)
            .Include(a => a.Quotation)
            .Include(a => a.ApprovedByUser)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.CustomerApprovalId,
                a.ProjectId,
                a.QuotationId,
                a.DesignId,
                a.ApprovalType,
                a.Status,
                a.Comments,
                a.ApprovedByUserId,
                ApprovedByUserName =
                    a.ApprovedByUser != null
                        ? a.ApprovedByUser.UserName
                        : null,
                a.ApprovedAt,
                a.CreatedAt,

                DesignTitle =
                    a.Design != null
                        ? a.Design.DesignTitle
                        : null,

                DesignVersion =
                    a.Design != null
                        ? a.Design.VersionNumber
                        : (int?)null,

                DesignStatus =
                    a.Design != null
                        ? a.Design.Status
                        : null
            })
            .ToListAsync();

        return Ok(approvals);
    }

    // =========================================================
    // GET: api/CustomerApprovals/1
    // =========================================================
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetApproval(long id)
    {
        var approval = await _context.CustomerApprovals
            .Where(a =>
                a.CustomerApprovalId == id)
            .Include(a => a.Design)
            .Include(a => a.Quotation)
            .Include(a => a.ApprovedByUser)
            .Select(a => new
            {
                a.CustomerApprovalId,
                a.ProjectId,
                a.QuotationId,
                a.DesignId,
                a.ApprovalType,
                a.Status,
                a.Comments,
                a.ApprovedByUserId,

                ApprovedByUserName =
                    a.ApprovedByUser != null
                        ? a.ApprovedByUser.UserName
                        : null,

                a.ApprovedAt,
                a.CreatedAt,

                DesignTitle =
                    a.Design != null
                        ? a.Design.DesignTitle
                        : null,

                DesignVersion =
                    a.Design != null
                        ? a.Design.VersionNumber
                        : (int?)null,

                DesignStatus =
                    a.Design != null
                        ? a.Design.Status
                        : null
            })
            .FirstOrDefaultAsync();

        if (approval == null)
        {
            return NotFound(new
            {
                message = "Customer approval not found."
            });
        }

        return Ok(approval);
    }

    // =========================================================
    // POST: api/CustomerApprovals
    //
    // Creates a PENDING customer approval.
    //
    // Currently intended primarily for DESIGN approvals.
    // =========================================================
    [HttpPost]
    public async Task<IActionResult> CreateApproval(
        [FromBody] CreateCustomerApprovalRequest request)
    {
        var approvalType =
            request.ApprovalType?
                .Trim()
                .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(approvalType))
        {
            return BadRequest(new
            {
                message = "ApprovalType is required."
            });
        }

        var allowedTypes = new[]
        {
            "DESIGN",
            "QUOTATION",
            "MATERIAL",
            "PROJECT",
            "OTHER"
        };

        if (!allowedTypes.Contains(approvalType))
        {
            return BadRequest(new
            {
                message = "Invalid approval type.",
                allowedTypes
            });
        }

        // -----------------------------------------------------
        // Project must exist
        // -----------------------------------------------------

        var projectExists = await _context.Projects
            .AnyAsync(p =>
                p.ProjectId == request.ProjectId);

        if (!projectExists)
        {
            return BadRequest(new
            {
                message = "Project not found."
            });
        }

        // -----------------------------------------------------
        // DESIGN approval validation
        // -----------------------------------------------------

        Design? design = null;

        if (approvalType == "DESIGN")
        {
            if (!request.DesignId.HasValue)
            {
                return BadRequest(new
                {
                    message =
                        "DesignId is required for a DESIGN approval."
                });
            }

            design = await _context.Designs
                .FirstOrDefaultAsync(d =>
                    d.DesignId ==
                    request.DesignId.Value);

            if (design == null)
            {
                return BadRequest(new
                {
                    message = "Design not found."
                });
            }

            if (design.ProjectId != request.ProjectId)
            {
                return BadRequest(new
                {
                    message =
                        "The selected design does not belong to this project."
                });
            }

            // A design must actually be in customer review.
            if (design.Status != "CUSTOMER_REVIEW")
            {
                return BadRequest(new
                {
                    message =
                        $"Customer approval cannot be created because the design is currently '{design.Status}'.",

                    requiredDesignStatus =
                        "CUSTOMER_REVIEW"
                });
            }

            // Prevent duplicate pending approval for the same
            // design version.
            var pendingApprovalExists =
                await _context.CustomerApprovals
                    .AnyAsync(a =>
                        a.DesignId ==
                        request.DesignId.Value &&
                        a.ApprovalType == "DESIGN" &&
                        a.Status == "PENDING");

            if (pendingApprovalExists)
            {
                return Conflict(new
                {
                    message =
                        "A pending customer approval already exists for this design.",

                    designId =
                        request.DesignId.Value
                });
            }
        }

        // -----------------------------------------------------
        // QUOTATION validation
        // -----------------------------------------------------

        if (approvalType == "QUOTATION")
        {
            if (!request.QuotationId.HasValue)
            {
                return BadRequest(new
                {
                    message =
                        "QuotationId is required for a QUOTATION approval."
                });
            }

            var quotationExists =
                await _context.Quotations
                    .AnyAsync(q =>
                        q.QuotationId ==
                        request.QuotationId.Value &&
                        (
                            q.ProjectId ==
                            request.ProjectId ||

                            q.LeadId != null
                        ));

            if (!quotationExists)
            {
                return BadRequest(new
                {
                    message =
                        "Quotation not found or not associated with the project."
                });
            }
        }

        // -----------------------------------------------------
        // Create approval
        // -----------------------------------------------------

        var approval = new CustomerApproval
        {
            ProjectId = request.ProjectId,

            QuotationId =
                request.QuotationId,

            DesignId =
                request.DesignId,

            ApprovalType =
                approvalType,

            Status = "PENDING",

            Comments =
                string.IsNullOrWhiteSpace(request.Comments)
                    ? null
                    : request.Comments.Trim(),

            CreatedAt = DateTime.Now
        };

        _context.CustomerApprovals.Add(approval);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetApproval),
            new { id = approval.CustomerApprovalId },
            new
            {
                message =
                    "Customer approval request created successfully.",

                customerApprovalId =
                    approval.CustomerApprovalId,

                projectId =
                    approval.ProjectId,

                designId =
                    approval.DesignId,

                quotationId =
                    approval.QuotationId,

                approvalType =
                    approval.ApprovalType,

                status =
                    approval.Status
            });
    }

    // =========================================================
    // PUT: api/CustomerApprovals/1/approve
    //
    // PENDING -> APPROVED
    //
    // For DESIGN:
    //   CustomerApproval -> APPROVED
    //   Design -> APPROVED
    //   Customer Design Approval Task -> COMPLETED
    // =========================================================
    [HttpPut("{id:long}/approve")]
    public async Task<IActionResult> Approve(
        long id,
        [FromBody] ApproveCustomerApprovalRequest request)
    {
        var approval = await _context.CustomerApprovals
            .Include(a => a.Design)
            .FirstOrDefaultAsync(a =>
                a.CustomerApprovalId == id);

        if (approval == null)
        {
            return NotFound(new
            {
                message = "Customer approval not found."
            });
        }

        if (approval.Status != "PENDING")
        {
            return BadRequest(new
            {
                message =
                    $"Approval cannot be approved from status '{approval.Status}'.",

                requiredStatus = "PENDING"
            });
        }

        // -----------------------------------------------------
        // DESIGN approval
        // -----------------------------------------------------

        if (approval.ApprovalType == "DESIGN")
        {
            if (approval.Design == null)
            {
                return BadRequest(new
                {
                    message =
                        "The design associated with this approval was not found."
                });
            }

            if (approval.Design.Status != "CUSTOMER_REVIEW")
            {
                return BadRequest(new
                {
                    message =
                        $"The design cannot be approved because its current status is '{approval.Design.Status}'.",

                    requiredDesignStatus =
                        "CUSTOMER_REVIEW"
                });
            }

            approval.Status = "APPROVED";
            approval.Comments =
                string.IsNullOrWhiteSpace(request.Comments)
                    ? approval.Comments
                    : request.Comments.Trim();

            approval.ApprovedByUserId =
                request.ApprovedByUserId;

            approval.ApprovedAt = DateTime.Now;

            // Update design
            approval.Design.Status = "APPROVED";
            approval.Design.ApprovedAt = DateTime.Now;
            approval.Design.UpdatedAt = DateTime.Now;

            // -------------------------------------------------
            // Complete Customer Design Approval task
            //
            // Find the task associated with this design's
            // ProjectService and customer approval step.
            // -------------------------------------------------

            var approvalTask = await _context.ProjectTasks
                .Where(t =>
                    t.ProjectId ==
                    approval.ProjectId &&

                    t.ProjectServiceId ==
                    approval.Design.ProjectServiceId &&

                    t.TaskName ==
                    "Customer Design Approval")
                .OrderByDescending(t => t.ProjectTaskId)
                .FirstOrDefaultAsync();

            if (approvalTask != null)
            {
                // The task should be IN_PROGRESS before approval.
                if (approvalTask.Status == "IN_PROGRESS" ||
                    approvalTask.Status == "PENDING")
                {
                    approvalTask.Status = "COMPLETED";
                    approvalTask.ProgressPercent = 100;
                    approvalTask.CompletedDate =
                        DateOnly.FromDateTime(DateTime.Now);
                    approvalTask.UpdatedAt = DateTime.Now;
                }
            }
        }
        else
        {
            // -------------------------------------------------
            // Non-design approval types
            // -------------------------------------------------

            approval.Status = "APPROVED";

            approval.Comments =
                string.IsNullOrWhiteSpace(request.Comments)
                    ? approval.Comments
                    : request.Comments.Trim();

            approval.ApprovedByUserId =
                request.ApprovedByUserId;

            approval.ApprovedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Customer approval approved successfully.",

            customerApprovalId =
                approval.CustomerApprovalId,

            approvalType =
                approval.ApprovalType,

            status =
                approval.Status,

            designId =
                approval.DesignId,

            approvedAt =
                approval.ApprovedAt
        });
    }

    // =========================================================
    // PUT: api/CustomerApprovals/1/revision
    //
    // PENDING -> REVISION_REQUESTED
    //
    // For DESIGN:
    //   CustomerApproval -> REVISION_REQUESTED
    //   Design -> REVISION_REQUIRED
    //   Task #5 remains IN_PROGRESS
    // =========================================================
    [HttpPut("{id:long}/revision")]
    public async Task<IActionResult> RequestRevision(
        long id,
        [FromBody] RevisionCustomerApprovalRequest request)
    {
        var approval = await _context.CustomerApprovals
            .Include(a => a.Design)
            .FirstOrDefaultAsync(a =>
                a.CustomerApprovalId == id);

        if (approval == null)
        {
            return NotFound(new
            {
                message = "Customer approval not found."
            });
        }

        if (approval.Status != "PENDING")
        {
            return BadRequest(new
            {
                message =
                    $"Revision cannot be requested from status '{approval.Status}'.",

                requiredStatus = "PENDING"
            });
        }

        if (string.IsNullOrWhiteSpace(request.Comments))
        {
            return BadRequest(new
            {
                message =
                    "Comments are required when requesting a revision."
            });
        }

        // -----------------------------------------------------
        // DESIGN revision
        // -----------------------------------------------------

        if (approval.ApprovalType == "DESIGN")
        {
            if (approval.Design == null)
            {
                return BadRequest(new
                {
                    message =
                        "The design associated with this approval was not found."
                });
            }

            if (approval.Design.Status != "CUSTOMER_REVIEW")
            {
                return BadRequest(new
                {
                    message =
                        $"Revision cannot be requested because the design is currently '{approval.Design.Status}'.",

                    requiredDesignStatus =
                        "CUSTOMER_REVIEW"
                });
            }

            approval.Status =
                "REVISION_REQUESTED";

            approval.Comments =
                request.Comments.Trim();

            // Update design
            approval.Design.Status =
                "REVISION_REQUIRED";

            approval.Design.UpdatedAt =
                DateTime.Now;
        }
        else
        {
            approval.Status =
                "REVISION_REQUESTED";

            approval.Comments =
                request.Comments.Trim();
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Customer revision request recorded successfully.",

            customerApprovalId =
                approval.CustomerApprovalId,

            approvalType =
                approval.ApprovalType,

            status =
                approval.Status,

            designId =
                approval.DesignId,

            comments =
                approval.Comments
        });
    }

    // =========================================================
    // PUT: api/CustomerApprovals/1/reject
    //
    // PENDING -> REJECTED
    // =========================================================
    [HttpPut("{id:long}/reject")]
    public async Task<IActionResult> Reject(
        long id,
        [FromBody] RejectCustomerApprovalRequest request)
    {
        var approval = await _context.CustomerApprovals
            .Include(a => a.Design)
            .FirstOrDefaultAsync(a =>
                a.CustomerApprovalId == id);

        if (approval == null)
        {
            return NotFound(new
            {
                message = "Customer approval not found."
            });
        }

        if (approval.Status != "PENDING")
        {
            return BadRequest(new
            {
                message =
                    $"Approval cannot be rejected from status '{approval.Status}'.",

                requiredStatus = "PENDING"
            });
        }

        if (string.IsNullOrWhiteSpace(request.Comments))
        {
            return BadRequest(new
            {
                message =
                    "Comments are required when rejecting an approval."
            });
        }

        approval.Status = "REJECTED";
        approval.Comments =
            request.Comments.Trim();

        // -----------------------------------------------------
        // For DESIGN rejection, mark the design REJECTED.
        // -----------------------------------------------------

        if (approval.ApprovalType == "DESIGN" &&
            approval.Design != null)
        {
            if (approval.Design.Status !=
                "CUSTOMER_REVIEW")
            {
                return BadRequest(new
                {
                    message =
                        $"The design cannot be rejected because its current status is '{approval.Design.Status}'.",

                    requiredDesignStatus =
                        "CUSTOMER_REVIEW"
                });
            }

            approval.Design.Status = "REJECTED";
            approval.Design.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Customer approval rejected successfully.",

            customerApprovalId =
                approval.CustomerApprovalId,

            approvalType =
                approval.ApprovalType,

            status =
                approval.Status,

            designId =
                approval.DesignId
        });
    }
}

// =============================================================
// DTOs
// =============================================================

public class CreateCustomerApprovalRequest
{
    public long ProjectId { get; set; }

    public long? QuotationId { get; set; }

    public long? DesignId { get; set; }

    public string ApprovalType { get; set; } = null!;

    public string? Comments { get; set; }
}

public class ApproveCustomerApprovalRequest
{
    public long? ApprovedByUserId { get; set; }

    public string? Comments { get; set; }
}

public class RevisionCustomerApprovalRequest
{
    public string Comments { get; set; } = null!;
}

public class RejectCustomerApprovalRequest
{
    public string Comments { get; set; } = null!;
}