using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class Notification
{
    public long NotificationId { get; set; }

    public long? UserId { get; set; }

    public long? CustomerId { get; set; }

    public long? ProjectId { get; set; }

    public string NotificationType { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Project? Project { get; set; }

    public virtual User? User { get; set; }
}
