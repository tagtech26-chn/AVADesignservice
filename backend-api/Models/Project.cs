using System;
using System.Collections.Generic;

namespace backend_api.Models;

public partial class Project
{
    public long ProjectId { get; set; }

    public string ProjectCode { get; set; } = null!;

    public long CustomerId { get; set; }

    public long ProjectTypeId { get; set; }

    public string ProjectName { get; set; } = null!;

    public string? Description { get; set; }

    public string? SiteAddress { get; set; }

    public string? City { get; set; }

    public string? District { get; set; }

    public string? State { get; set; }

    public string? Pincode { get; set; }

    public decimal? ApproximateArea { get; set; }

    public string? AreaUnit { get; set; }

    public DateOnly? ExpectedStartDate { get; set; }

    public DateOnly? ExpectedEndDate { get; set; }

    public string Status { get; set; } = null!;

    public long? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long? LeadId { get; set; }

    public long? ApprovedQuotationId { get; set; }

    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public virtual Quotation? ApprovedQuotation { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<CustomerApproval> CustomerApprovals { get; set; } = new List<CustomerApproval>();

    public virtual ICollection<Design> Designs { get; set; } = new List<Design>();

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual Lead? Lead { get; set; }

    public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<ProjectProgress> ProjectProgresses { get; set; } = new List<ProjectProgress>();

    public virtual ICollection<ProjectResource> ProjectResources { get; set; } = new List<ProjectResource>();

    public virtual ICollection<ProjectService> ProjectServices { get; set; } = new List<ProjectService>();

    public virtual ICollection<ProjectStage> ProjectStages { get; set; } = new List<ProjectStage>();

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();

    public virtual ProjectType ProjectType { get; set; } = null!;

    public virtual ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();

    public virtual ICollection<SiteVisit> SiteVisits { get; set; } = new List<SiteVisit>();
}
