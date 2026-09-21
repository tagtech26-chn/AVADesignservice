using Microsoft.AspNetCore.Mvc;
using backend_api.Data;
using backend_api.Models;

namespace backend_api.Controllers;

[Route("api/[controller]")]
public class NotificationsController : CrudController<Notification>
{
    public NotificationsController(AVADesignServicesDbContext context)
        : base(context)
    {
    }
}

