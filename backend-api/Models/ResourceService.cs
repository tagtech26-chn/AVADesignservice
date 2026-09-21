using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class ResourceService
{
    public long ResourceServiceId { get; set; }

    public long ResourceId { get; set; }

    public long ServiceId { get; set; }

    public string? Notes { get; set; }

    public bool IsPreferred { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Resource Resource { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
