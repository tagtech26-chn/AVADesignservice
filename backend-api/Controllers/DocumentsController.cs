using Microsoft.AspNetCore.Mvc;
using backend_api.Data;
using backend_api.Models;

namespace backend_api.Controllers;

[Route("api/[controller]")]
public class DocumentsController : CrudController<Document>
{
    public DocumentsController(AVADesignServicesDbContext context)
        : base(context)
    {
    }
}

