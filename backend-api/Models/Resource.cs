using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class Resource
{
    public long ResourceId { get; set; }

    public long ResourceTypeId { get; set; }

    public string ResourceName { get; set; } = null!;

    public string? ContactPerson { get; set; }

    public string? Mobile { get; set; }

    public string? AlternateMobile { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? District { get; set; }

    public string? State { get; set; }

    public string? Gstnumber { get; set; }

    public decimal? InternalRating { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ProjectResource> ProjectResources { get; set; } = new List<ProjectResource>();

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();

    public virtual ICollection<ResourceService> ResourceServices { get; set; } = new List<ResourceService>();

    public virtual ResourceType ResourceType { get; set; } = null!;
}
