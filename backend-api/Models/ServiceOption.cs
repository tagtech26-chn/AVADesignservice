using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class ServiceOption
{
    public long ServiceOptionId { get; set; }

    public long ServiceId { get; set; }

    public string OptionCode { get; set; } = null!;

    public string OptionName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsAvailableToCustomer { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long? ServiceTypeId { get; set; }

    public virtual ICollection<LeadService> LeadServices { get; set; } = new List<LeadService>();

    public virtual ICollection<ProjectService> ProjectServices { get; set; } = new List<ProjectService>();

    public virtual Service Service { get; set; } = null!;

    public virtual ServiceType? ServiceType { get; set; }
}
