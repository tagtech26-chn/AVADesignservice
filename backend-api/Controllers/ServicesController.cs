using backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public ServicesController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetServices()
    {
        var services = await _context.Services
            .AsNoTracking()
            .Include(s => s.ServiceCategory)
            .Include(s => s.ServiceOptions)
            .Where(s => s.IsActive)
            .OrderBy(s => s.ServiceCategory.DisplayOrder)
            .ThenBy(s => s.DisplayOrder)
            .Select(s => new
            {
                serviceId = s.ServiceId,
                serviceName = s.ServiceName,
                shortDescription = s.ShortDescription,
                category = new
                {
                    categoryId = s.ServiceCategory.ServiceCategoryId,
                    categoryName = s.ServiceCategory.CategoryName
                },
                options = s.ServiceOptions
                    .Where(o => o.IsActive && o.IsAvailableToCustomer)
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => new
                    {
                        optionId = o.ServiceOptionId,
                        code = o.OptionCode,
                        name = o.OptionName,
                        description = o.Description
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(services);
    }
}
