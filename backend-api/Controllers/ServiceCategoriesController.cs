using backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceCategoriesController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public ServiceCategoriesController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetServiceCategories()
    {
        var categories = await _context.ServiceCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new
            {
                serviceCategoryId = c.ServiceCategoryId,
                categoryName = c.CategoryName,
                description = c.Description,
                displayOrder = c.DisplayOrder,
                services = c.Services
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => new
                    {
                        serviceId = s.ServiceId,
                        serviceName = s.ServiceName,
                        shortDescription = s.ShortDescription,
                        displayOrder = s.DisplayOrder,
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
                    .ToList()
            })
            .ToListAsync();

        return Ok(categories);
    }
}
