using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class ProjectType
{
    public long ProjectTypeId { get; set; }

    public string ProjectTypeName { get; set; } = null!;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}
