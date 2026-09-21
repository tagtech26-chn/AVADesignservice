using Microsoft.AspNetCore.Mvc;
using backend_api.Data;
using backend_api.Models;

namespace backend_api.Controllers;

[Route("api/[controller]")]
public class ServiceOptionsController : CrudController<ServiceOption>
{
    public ServiceOptionsController(AVADesignServicesDbContext context)
        : base(context)
    {
    }
}

