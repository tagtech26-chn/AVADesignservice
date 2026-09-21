using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class ResourceType
{
    public long ResourceTypeId { get; set; }

    public string ResourceTypeName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();
}
