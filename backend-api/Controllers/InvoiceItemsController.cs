using Microsoft.AspNetCore.Mvc;
using backend_api.Data;
using backend_api.Models;

namespace backend_api.Controllers;

[Route("api/[controller]")]
public class InvoiceItemsController : CrudController<InvoiceItem>
{
    public InvoiceItemsController(AVADesignServicesDbContext context)
        : base(context)
    {
    }
}

