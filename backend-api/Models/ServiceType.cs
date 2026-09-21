using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class ServiceType
{
    public long ServiceTypeId { get; set; }

    public string TypeCode { get; set; } = null!;

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ServiceOption> ServiceOptions { get; set; } = new List<ServiceOption>();
}
