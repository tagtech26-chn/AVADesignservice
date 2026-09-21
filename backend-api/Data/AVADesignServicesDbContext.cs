using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using backend_api.Models;

namespace backend_api.Data;

public partial class AVADesignServicesDbContext : DbContext
{
    public AVADesignServicesDbContext(DbContextOptions<AVADesignServicesDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<CustomerAddress> CustomerAddresses { get; set; }

    public virtual DbSet<CustomerApproval> CustomerApprovals { get; set; }

    public virtual DbSet<Design> Designs { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<DocumentType> DocumentTypes { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceItem> InvoiceItems { get; set; }

    public virtual DbSet<Lead> Leads { get; set; }

    public virtual DbSet<LeadService> LeadServices { get; set; }

    public virtual DbSet<LeadStatus> LeadStatuses { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectProgress> ProjectProgresses { get; set; }

    public virtual DbSet<ProjectResource> ProjectResources { get; set; }

    public virtual DbSet<ProjectService> ProjectServices { get; set; }

    public virtual DbSet<ProjectStage> ProjectStages { get; set; }

    public virtual DbSet<ProjectTask> ProjectTasks { get; set; }

    public virtual DbSet<ProjectType> ProjectTypes { get; set; }

    public virtual DbSet<Quotation> Quotations { get; set; }

    public virtual DbSet<QuotationItem> QuotationItems { get; set; }

    public virtual DbSet<Resource> Resources { get; set; }

    public virtual DbSet<ResourceService> ResourceServices { get; set; }

    public virtual DbSet<ResourceType> ResourceTypes { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ServiceCategory> ServiceCategories { get; set; }

    public virtual DbSet<ServiceOption> ServiceOptions { get; set; }

    public virtual DbSet<ServiceType> ServiceTypes { get; set; }

    public virtual DbSet<SiteVisit> SiteVisits { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasIndex(e => new { e.ProjectId, e.CreatedAt }, "IX_ActivityLogs_Project");

            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(64);

            entity.HasOne(d => d.Customer).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_ActivityLogs_Customer");

            entity.HasOne(d => d.Project).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_ActivityLogs_Project");

            entity.HasOne(d => d.User).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_ActivityLogs_User");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => e.Email, "IX_Customers_Email");

            entity.HasIndex(e => e.Mobile, "IX_Customers_Mobile");

            entity.Property(e => e.AlternateMobile).HasMaxLength(30);
            entity.Property(e => e.CompanyName).HasMaxLength(250);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.CustomerType).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Gstnumber)
                .HasMaxLength(50)
                .HasColumnName("GSTNumber");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Mobile).HasMaxLength(30);
            entity.Property(e => e.Pannumber)
                .HasMaxLength(50)
                .HasColumnName("PANNumber");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.PortalUser).WithMany(p => p.Customers)
                .HasForeignKey(d => d.PortalUserId)
                .HasConstraintName("FK_Customers_PortalUser");
        });

        modelBuilder.Entity<CustomerAddress>(entity =>
        {
            entity.Property(e => e.AddressLine1).HasMaxLength(250);
            entity.Property(e => e.AddressLine2).HasMaxLength(250);
            entity.Property(e => e.AddressType).HasMaxLength(50);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasDefaultValue("India");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Landmark).HasMaxLength(250);
            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Pincode).HasMaxLength(20);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Customer).WithMany(p => p.CustomerAddresses)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CustomerAddresses_Customer");
        });

        modelBuilder.Entity<CustomerApproval>(entity =>
        {
            entity.Property(e => e.ApprovalType).HasMaxLength(50);
            entity.Property(e => e.ApprovedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("PENDING");

            entity.HasOne(d => d.ApprovedByUser).WithMany(p => p.CustomerApprovals)
                .HasForeignKey(d => d.ApprovedByUserId)
                .HasConstraintName("FK_CustomerApprovals_User");

            entity.HasOne(d => d.Design).WithMany(p => p.CustomerApprovals)
                .HasForeignKey(d => d.DesignId)
                .HasConstraintName("FK_CustomerApprovals_Design");

            entity.HasOne(d => d.Project).WithMany(p => p.CustomerApprovals)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CustomerApprovals_Project");

            entity.HasOne(d => d.Quotation).WithMany(p => p.CustomerApprovals)
                .HasForeignKey(d => d.QuotationId)
                .HasConstraintName("FK_CustomerApprovals_Quotation");
        });

        modelBuilder.Entity<Design>(entity =>
        {
            entity.Property(e => e.ApprovedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DesignTitle).HasMaxLength(250);
            entity.Property(e => e.DesignType).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("DRAFT");
            entity.Property(e => e.SubmittedAt).HasPrecision(0);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.VersionNumber).HasDefaultValue(1);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Designs)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_Designs_CreatedBy");

            entity.HasOne(d => d.Project).WithMany(p => p.Designs)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Designs_Project");

            entity.HasOne(d => d.ProjectService).WithMany(p => p.Designs)
                .HasForeignKey(d => d.ProjectServiceId)
                .HasConstraintName("FK_Designs_ProjectService");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasIndex(e => new { e.ProjectId, e.CreatedAt }, "IX_Documents_Project");

            entity.Property(e => e.ContentType).HasMaxLength(150);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(1000);
            entity.Property(e => e.OriginalFileName).HasMaxLength(255);
            entity.Property(e => e.VersionNumber).HasDefaultValue(1);

            entity.HasOne(d => d.Design).WithMany(p => p.Documents)
                .HasForeignKey(d => d.DesignId)
                .HasConstraintName("FK_Documents_Design");

            entity.HasOne(d => d.DocumentType).WithMany(p => p.Documents)
                .HasForeignKey(d => d.DocumentTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Documents_Type");

            entity.HasOne(d => d.Project).WithMany(p => p.Documents)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_Documents_Project");

            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.Documents)
                .HasForeignKey(d => d.UploadedByUserId)
                .HasConstraintName("FK_Documents_UploadedBy");
        });

        modelBuilder.Entity<DocumentType>(entity =>
        {
            entity.HasIndex(e => e.DocumentTypeName, "UQ_DocumentTypes_Name").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DocumentTypeName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(e => new { e.CustomerId, e.Status }, "IX_Invoices_Customer");

            entity.HasIndex(e => e.InvoiceNumber, "UQ_Invoices_Number").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GrandTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.InvoiceDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.InvoiceNumber).HasMaxLength(50);
            entity.Property(e => e.PaidAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("DRAFT");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_Invoices_CreatedBy");

            entity.HasOne(d => d.Customer).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Customer");

            entity.HasOne(d => d.Project).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Project");
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.LineTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxPercent).HasColumnType("decimal(8, 3)");
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceItems_Invoice");
        });

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.HasIndex(e => e.Mobile, "IX_Leads_Mobile");

            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "IX_Leads_Status");

            entity.HasIndex(e => e.LeadCode, "UQ_Leads_LeadCode").IsUnique();

            entity.Property(e => e.ApproximateArea).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AreaUnit).HasMaxLength(20);
            entity.Property(e => e.CompanyName).HasMaxLength(250);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.LeadCode).HasMaxLength(50);
            entity.Property(e => e.LeadSource).HasMaxLength(100);
            entity.Property(e => e.Location).HasMaxLength(500);
            entity.Property(e => e.Mobile).HasMaxLength(30);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("NEW");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.AssignedToUser).WithMany(p => p.Leads)
                .HasForeignKey(d => d.AssignedToUserId)
                .HasConstraintName("FK_Leads_AssignedTo");

            entity.HasOne(d => d.Customer).WithMany(p => p.Leads)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Leads_Customer");

            entity.HasOne(d => d.Project).WithMany(p => p.Leads)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_Leads_Project");

            entity.HasOne(d => d.ProjectType).WithMany(p => p.Leads)
                .HasForeignKey(d => d.ProjectTypeId)
                .HasConstraintName("FK_Leads_ProjectType");
        });

        modelBuilder.Entity<LeadService>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Lead).WithMany(p => p.LeadServices)
                .HasForeignKey(d => d.LeadId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeadServices_Lead");

            entity.HasOne(d => d.Service).WithMany(p => p.LeadServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeadServices_Service");

            entity.HasOne(d => d.ServiceOption).WithMany(p => p.LeadServices)
                .HasForeignKey(d => d.ServiceOptionId)
                .HasConstraintName("FK_LeadServices_Option");
        });

        modelBuilder.Entity<LeadStatus>(entity =>
        {
            entity.HasIndex(e => e.StatusCode, "UQ_LeadStatuses_StatusCode").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.StatusCode).HasMaxLength(50);
            entity.Property(e => e.StatusName).HasMaxLength(100);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.SenderType).HasMaxLength(20);

            entity.HasOne(d => d.Customer).WithMany(p => p.Messages)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Messages_Customer");

            entity.HasOne(d => d.Project).WithMany(p => p.Messages)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_Messages_Project");

            entity.HasOne(d => d.User).WithMany(p => p.Messages)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Messages_User");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.NotificationType).HasMaxLength(50);
            entity.Property(e => e.ReadAt).HasPrecision(0);
            entity.Property(e => e.Title).HasMaxLength(250);

            entity.HasOne(d => d.Customer).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Notifications_Customer");

            entity.HasOne(d => d.Project).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_Notifications_Project");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Notifications_User");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => new { e.ProjectId, e.PaymentDate }, "IX_Payments_Project");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PaymentDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentReference).HasMaxLength(100);
            entity.Property(e => e.TransactionReference).HasMaxLength(150);

            entity.HasOne(d => d.Customer).WithMany(p => p.Payments)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Customer");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("FK_Payments_Invoice");

            entity.HasOne(d => d.Project).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Project");

            entity.HasOne(d => d.ReceivedByUser).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ReceivedByUserId)
                .HasConstraintName("FK_Payments_ReceivedBy");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasIndex(e => e.ApprovedQuotationId, "IX_Projects_ApprovedQuotationId");

            entity.HasIndex(e => new { e.CustomerId, e.Status }, "IX_Projects_Customer");

            entity.HasIndex(e => e.LeadId, "IX_Projects_LeadId");

            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "IX_Projects_Status");

            entity.HasIndex(e => e.ProjectCode, "UQ_Projects_ProjectCode").IsUnique();

            entity.Property(e => e.ApproximateArea).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AreaUnit).HasMaxLength(20);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.Pincode).HasMaxLength(20);
            entity.Property(e => e.ProjectCode).HasMaxLength(50);
            entity.Property(e => e.ProjectName).HasMaxLength(250);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("ENQUIRY");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.ApprovedQuotation).WithMany(p => p.Projects)
                .HasForeignKey(d => d.ApprovedQuotationId)
                .HasConstraintName("FK_Projects_ApprovedQuotation");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Projects)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_Projects_CreatedBy");

            entity.HasOne(d => d.Customer).WithMany(p => p.Projects)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Projects_Customer");

            entity.HasOne(d => d.Lead).WithMany(p => p.Projects)
                .HasForeignKey(d => d.LeadId)
                .HasConstraintName("FK_Projects_Lead");

            entity.HasOne(d => d.ProjectType).WithMany(p => p.Projects)
                .HasForeignKey(d => d.ProjectTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Projects_ProjectType");
        });

        modelBuilder.Entity<ProjectProgress>(entity =>
        {
            entity.ToTable("ProjectProgress");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ProgressPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.UpdateTitle).HasMaxLength(250);

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectProgresses)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectProgress_Project");

            entity.HasOne(d => d.ProjectStage).WithMany(p => p.ProjectProgresses)
                .HasForeignKey(d => d.ProjectStageId)
                .HasConstraintName("FK_ProjectProgress_Stage");

            entity.HasOne(d => d.ProjectTask).WithMany(p => p.ProjectProgresses)
                .HasForeignKey(d => d.ProjectTaskId)
                .HasConstraintName("FK_ProjectProgress_Task");

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.ProjectProgresses)
                .HasForeignKey(d => d.UpdatedByUserId)
                .HasConstraintName("FK_ProjectProgress_User");
        });

        modelBuilder.Entity<ProjectResource>(entity =>
        {
            entity.HasIndex(e => new { e.ProjectId, e.AssignmentStatus }, "IX_ProjectResources_Project");

            entity.Property(e => e.AgreedAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AssignmentStatus)
                .HasMaxLength(50)
                .HasDefaultValue("ASSIGNED");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.AssignedByUser).WithMany(p => p.ProjectResources)
                .HasForeignKey(d => d.AssignedByUserId)
                .HasConstraintName("FK_ProjectResources_AssignedBy");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectResources)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectResources_Project");

            entity.HasOne(d => d.ProjectService).WithMany(p => p.ProjectResources)
                .HasForeignKey(d => d.ProjectServiceId)
                .HasConstraintName("FK_ProjectResources_ProjectService");

            entity.HasOne(d => d.Resource).WithMany(p => p.ProjectResources)
                .HasForeignKey(d => d.ResourceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectResources_Resource");
        });

        modelBuilder.Entity<ProjectService>(entity =>
        {
            entity.HasIndex(e => new { e.ProjectId, e.Status }, "IX_ProjectServices_Project");

            entity.Property(e => e.ApprovedAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.EstimatedAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("REQUESTED");
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectServices)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectServices_Project");

            entity.HasOne(d => d.Service).WithMany(p => p.ProjectServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectServices_Service");

            entity.HasOne(d => d.ServiceOption).WithMany(p => p.ProjectServices)
                .HasForeignKey(d => d.ServiceOptionId)
                .HasConstraintName("FK_ProjectServices_Option");
        });

        modelBuilder.Entity<ProjectStage>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ProgressPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.StageCode).HasMaxLength(50);
            entity.Property(e => e.StageName).HasMaxLength(150);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectStages)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectStages_Project");
        });

        modelBuilder.Entity<ProjectTask>(entity =>
        {
            entity.HasIndex(e => e.DependsOnProjectTaskId, "IX_ProjectTasks_DependsOnProjectTaskId");

            entity.HasIndex(e => new { e.ProjectId, e.Status }, "IX_ProjectTasks_Project");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Priority)
                .HasMaxLength(20)
                .HasDefaultValue("MEDIUM");
            entity.Property(e => e.ProgressPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.TaskName).HasMaxLength(250);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.AssignedResource).WithMany(p => p.ProjectTasks)
                .HasForeignKey(d => d.AssignedResourceId)
                .HasConstraintName("FK_ProjectTasks_Resource");

            entity.HasOne(d => d.AssignedToUser).WithMany(p => p.ProjectTasks)
                .HasForeignKey(d => d.AssignedToUserId)
                .HasConstraintName("FK_ProjectTasks_User");

            entity.HasOne(d => d.DependsOnProjectTask).WithMany(p => p.InverseDependsOnProjectTask)
                .HasForeignKey(d => d.DependsOnProjectTaskId)
                .HasConstraintName("FK_ProjectTasks_DependsOn");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectTasks)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectTasks_Project");

            entity.HasOne(d => d.ProjectService).WithMany(p => p.ProjectTasks)
                .HasForeignKey(d => d.ProjectServiceId)
                .HasConstraintName("FK_ProjectTasks_ProjectService");

            entity.HasOne(d => d.ProjectStage).WithMany(p => p.ProjectTasks)
                .HasForeignKey(d => d.ProjectStageId)
                .HasConstraintName("FK_ProjectTasks_Stage");
        });

        modelBuilder.Entity<ProjectType>(entity =>
        {
            entity.HasIndex(e => e.ProjectTypeName, "UQ_ProjectTypes_Name").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ProjectTypeName).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<Quotation>(entity =>
        {
            entity.HasIndex(e => e.LeadId, "IX_Quotations_LeadId");

            entity.HasIndex(e => new { e.ProjectId, e.Status }, "IX_Quotations_Project");

            entity.HasIndex(e => e.QuotationNumber, "UQ_Quotations_Number").IsUnique();

            entity.Property(e => e.ApprovedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GrandTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.QuotationDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.QuotationNumber).HasMaxLength(50);
            entity.Property(e => e.RevisionNumber).HasDefaultValue(1);
            entity.Property(e => e.SentAt).HasPrecision(0);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("DRAFT");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Quotations)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_Quotations_CreatedBy");

            entity.HasOne(d => d.Lead).WithMany(p => p.Quotations)
                .HasForeignKey(d => d.LeadId)
                .HasConstraintName("FK_Quotations_Lead");

            entity.HasOne(d => d.Project).WithMany(p => p.Quotations)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_Quotations_Project");
        });

        modelBuilder.Entity<QuotationItem>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ItemType).HasMaxLength(30);
            entity.Property(e => e.LineTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxPercent).HasColumnType("decimal(8, 3)");
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.ProjectService).WithMany(p => p.QuotationItems)
                .HasForeignKey(d => d.ProjectServiceId)
                .HasConstraintName("FK_QuotationItems_ProjectService");

            entity.HasOne(d => d.Quotation).WithMany(p => p.QuotationItems)
                .HasForeignKey(d => d.QuotationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuotationItems_Quotation");
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.HasIndex(e => new { e.ResourceTypeId, e.IsActive }, "IX_Resources_Type");

            entity.Property(e => e.AlternateMobile).HasMaxLength(30);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.ContactPerson).HasMaxLength(200);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Gstnumber)
                .HasMaxLength(50)
                .HasColumnName("GSTNumber");
            entity.Property(e => e.InternalRating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Mobile).HasMaxLength(30);
            entity.Property(e => e.ResourceName).HasMaxLength(250);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.ResourceType).WithMany(p => p.Resources)
                .HasForeignKey(d => d.ResourceTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Resources_Type");
        });

        modelBuilder.Entity<ResourceService>(entity =>
        {
            entity.HasIndex(e => new { e.ResourceId, e.ServiceId }, "UQ_ResourceServices").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(d => d.Resource).WithMany(p => p.ResourceServices)
                .HasForeignKey(d => d.ResourceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ResourceServices_Resource");

            entity.HasOne(d => d.Service).WithMany(p => p.ResourceServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ResourceServices_Service");
        });

        modelBuilder.Entity<ResourceType>(entity =>
        {
            entity.HasIndex(e => e.ResourceTypeName, "UQ_ResourceTypes_Name").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ResourceTypeName).HasMaxLength(150);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.RoleName, "UQ_Roles_RoleName").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RoleName).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasIndex(e => new { e.ServiceCategoryId, e.IsActive, e.DisplayOrder }, "IX_Services_Category");

            entity.HasIndex(e => new { e.ServiceCategoryId, e.ServiceName }, "UQ_Services_Category_Name").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsCustomerSelectable).HasDefaultValue(true);
            entity.Property(e => e.RequiresQuotation).HasDefaultValue(true);
            entity.Property(e => e.ServiceName).HasMaxLength(200);
            entity.Property(e => e.ShortDescription).HasMaxLength(500);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.ServiceCategory).WithMany(p => p.Services)
                .HasForeignKey(d => d.ServiceCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Services_Category");
        });

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.HasIndex(e => e.CategoryName, "UQ_ServiceCategories_CategoryName").IsUnique();

            entity.Property(e => e.CategoryName).HasMaxLength(150);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<ServiceOption>(entity =>
        {
            entity.HasIndex(e => new { e.ServiceId, e.IsActive, e.DisplayOrder }, "IX_ServiceOptions_Service");

            entity.HasIndex(e => e.ServiceTypeId, "IX_ServiceOptions_ServiceTypeId");

            entity.HasIndex(e => new { e.ServiceId, e.OptionCode }, "UQ_ServiceOptions_Service_Code").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsAvailableToCustomer).HasDefaultValue(true);
            entity.Property(e => e.OptionCode).HasMaxLength(50);
            entity.Property(e => e.OptionName).HasMaxLength(150);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceOptions)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceOptions_Service");

            entity.HasOne(d => d.ServiceType).WithMany(p => p.ServiceOptions)
                .HasForeignKey(d => d.ServiceTypeId)
                .HasConstraintName("FK_ServiceOptions_ServiceTypes");
        });

        modelBuilder.Entity<ServiceType>(entity =>
        {
            entity.HasIndex(e => e.TypeCode, "UQ_ServiceTypes_TypeCode").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.TypeCode).HasMaxLength(50);
            entity.Property(e => e.TypeName).HasMaxLength(150);
        });

        modelBuilder.Entity<SiteVisit>(entity =>
        {
            entity.HasIndex(e => e.LeadId, "IX_SiteVisits_LeadId");

            entity.HasIndex(e => new { e.ProjectId, e.ScheduledAt }, "IX_SiteVisits_Project");

            entity.Property(e => e.CompletedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ScheduledAt).HasPrecision(0);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("SCHEDULED");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.AssignedToUser).WithMany(p => p.SiteVisits)
                .HasForeignKey(d => d.AssignedToUserId)
                .HasConstraintName("FK_SiteVisits_AssignedTo");

            entity.HasOne(d => d.Lead).WithMany(p => p.SiteVisits)
                .HasForeignKey(d => d.LeadId)
                .HasConstraintName("FK_SiteVisits_Lead");

            entity.HasOne(d => d.Project).WithMany(p => p.SiteVisits)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_SiteVisits_Project");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "IX_Users_Email");

            entity.HasIndex(e => e.Mobile, "IX_Users_Mobile");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastLoginAt).HasPrecision(0);
            entity.Property(e => e.Mobile).HasMaxLength(30);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.UserName).HasMaxLength(100);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.Property(e => e.AssignedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_Role");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
