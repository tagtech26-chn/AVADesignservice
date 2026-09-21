using backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeadStatusesController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public LeadStatusesController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetLeadStatuses()
    {
        var statuses = await _context.LeadStatuses
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new
            {
                leadStatusId = s.LeadStatusId,
                statusCode = s.StatusCode,
                statusName = s.StatusName,
                description = s.Description,
                displayOrder = s.DisplayOrder
            })
            .ToListAsync();

        return Ok(statuses);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetLeadStatus(long id)
    {
        var status = await _context.LeadStatuses
            .AsNoTracking()
            .Where(s => s.LeadStatusId == id && s.IsActive)
            .Select(s => new
            {
                leadStatusId = s.LeadStatusId,
                statusCode = s.StatusCode,
                statusName = s.StatusName,
                description = s.Description,
                displayOrder = s.DisplayOrder
            })
            .FirstOrDefaultAsync();

        if (status == null)
            return NotFound();

        return Ok(status);
    }
}
