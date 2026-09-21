using backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceCatalogController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public ServiceCatalogController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetCatalog()
    {
        var rows = await
            (from st in _context.ServiceTypes
             join so in _context.ServiceOptions
                 on st.ServiceTypeId equals so.ServiceTypeId
             join s in _context.Services
                 on so.ServiceId equals s.ServiceId
             where st.IsActive
                   && so.ServiceTypeId != null
                   && s.IsActive
             orderby st.DisplayOrder, s.ServiceId, so.ServiceOptionId
             select new
             {
                 ServiceTypeId = st.ServiceTypeId,
                 ServiceTypeCode = st.TypeCode,
                 ServiceTypeName = st.TypeName,

                 ServiceId = s.ServiceId,
                 ServiceName = s.ServiceName,

                 ServiceOptionId = so.ServiceOptionId,
                 ServiceOptionCode = so.OptionCode,
                 ServiceOptionName = so.OptionName
             })
            .AsNoTracking()
            .ToListAsync();

        var result = rows
            .GroupBy(x => new
            {
                x.ServiceTypeId,
                x.ServiceTypeCode,
                x.ServiceTypeName
            })
            .Select(type => new
            {
                serviceTypeId = type.Key.ServiceTypeId,
                typeCode = type.Key.ServiceTypeCode,
                typeName = type.Key.ServiceTypeName,

                services = type
                    .GroupBy(x => new
                    {
                        x.ServiceId,
                        x.ServiceName
                    })
                    .Select(service => new
                    {
                        serviceId = service.Key.ServiceId,
                        serviceName = service.Key.ServiceName,

                        subdivisions = service
                            .Select(x => new
                            {
                                serviceOptionId = x.ServiceOptionId,
                                optionCode = x.ServiceOptionCode,
                                optionName = x.ServiceOptionName
                            })
                            .Distinct()
                            .ToList()
                    })
                    .ToList()
            })
            .ToList();

        return Ok(result);
    }
}