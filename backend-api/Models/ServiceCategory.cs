using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class ServiceCategory
{
    public long ServiceCategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}
