using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class UserRole
{
    public long UserId { get; set; }

    public long RoleId { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
