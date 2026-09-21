using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class Customer
{
    public long CustomerId { get; set; }

    public string CustomerType { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string? CompanyName { get; set; }

    public string? Email { get; set; }

    public string? Mobile { get; set; }

    public string? AlternateMobile { get; set; }

    public string? Gstnumber { get; set; }

    public string? Pannumber { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long? PortalUserId { get; set; }

    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public virtual ICollection<CustomerAddress> CustomerAddresses { get; set; } = new List<CustomerAddress>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User? PortalUser { get; set; }

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}
