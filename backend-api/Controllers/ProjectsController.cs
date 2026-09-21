using Microsoft.AspNetCore.Mvc;
using backend_api.Data;
using backend_api.Models;

namespace backend_api.Controllers;

[Route("api/[controller]")]
public class ProjectsController : CrudController<Project>
{
    public ProjectsController(AVADesignServicesDbContext context)
        : base(context)
    {
    }
}

