using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class LeadService
{
    public long LeadServiceId { get; set; }

    public long LeadId { get; set; }

    public long ServiceId { get; set; }

    public long? ServiceOptionId { get; set; }

    public string? Requirement { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Lead Lead { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;

    public virtual ServiceOption? ServiceOption { get; set; }
}
