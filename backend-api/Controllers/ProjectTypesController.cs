using backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectTypesController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public ProjectTypesController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetProjectTypes()
    {
        var projectTypes = await _context.ProjectTypes
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new
            {
                projectTypeId = p.ProjectTypeId,
                projectTypeName = p.ProjectTypeName,
                description = p.Description
            })
            .ToListAsync();

        return Ok(projectTypes);
    }
}
