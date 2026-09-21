using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class Service
{
    public long ServiceId { get; set; }

    public long ServiceCategoryId { get; set; }

    public string ServiceName { get; set; } = null!;

    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsCustomerSelectable { get; set; }

    public bool RequiresSiteVisit { get; set; }

    public bool RequiresQuotation { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<LeadService> LeadServices { get; set; } = new List<LeadService>();

    public virtual ICollection<ProjectService> ProjectServices { get; set; } = new List<ProjectService>();

    public virtual ICollection<ResourceService> ResourceServices { get; set; } = new List<ResourceService>();

    public virtual ServiceCategory ServiceCategory { get; set; } = null!;

    public virtual ICollection<ServiceOption> ServiceOptions { get; set; } = new List<ServiceOption>();
}
