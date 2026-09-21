
<#
.SYNOPSIS
    AVADesignServices - Master Setup / Bootstrap Script

.DESCRIPTION
    One PowerShell script for recreating the current AVADesignServices
    development backend and database setup.

    Stack:
      .NET 9
      ASP.NET Core Web API
      SQL Server Express: localhost\SQLEXPRESS
      Database: AVADesignServices
      EF Core: 9.0.15
#>

$ErrorActionPreference = "Stop"

$Root = Join-Path $PSScriptRoot "AVADesignServices"
$Backend = Join-Path $Root "backend-api"
$DatabaseDir = Join-Path $Root "database"

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan
}

function Ensure-Directory([string]$Path) {
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Write-TextFile([string]$Path, [string]$Content) {
    Ensure-Directory (Split-Path $Path -Parent)
    Set-Content -Path $Path -Value $Content -Encoding UTF8
}

function Invoke-DotNet([string]$Arguments) {
    Write-Host "dotnet $Arguments" -ForegroundColor DarkGray
    & dotnet $Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed with exit code $LASTEXITCODE"
    }
}

function Invoke-SqlScript([string]$SqlScriptPath) {
    $sqlcmd = Get-Command sqlcmd.exe -ErrorAction SilentlyContinue
    if (-not $sqlcmd) {
        $sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
    }

    if (-not $sqlcmd) {
        throw "sqlcmd was not found. Install the SQL Server command-line utilities and rerun this script."
    }

    & $sqlcmd.Source -S "localhost\SQLEXPRESS" -E -i $SqlScriptPath
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed with exit code $LASTEXITCODE"
    }
}

Write-Step "AVADesignServices - Master Setup"

Ensure-Directory $Root
Ensure-Directory $DatabaseDir
Ensure-Directory $Backend

Write-Step "1. Writing the complete database script"

$DatabaseSql = @'
/*
    AVADesignServices
    Complete SQL Server database setup
    Version: 1.0

    Business model:
      OUR_SERVICE   = directly provided by AVA
      SUPPLY        = material/product supplied
      RESOURCE      = external skilled/vendor resource arranged
      COORDINATION  = project coordination

    Safe to run on a new SQL Server instance.
    The database is created only if it does not already exist.
*/

USE master;
GO

IF DB_ID(N'AVADesignServices') IS NULL
BEGIN
    CREATE DATABASE AVADesignServices;
END
GO

USE AVADesignServices;
GO

/* ============================================================
   1. SECURITY
   ============================================================ */

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        RoleId          BIGINT IDENTITY(1,1) NOT NULL,
        RoleName        NVARCHAR(100) NOT NULL,
        Description     NVARCHAR(500) NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT (1),
        CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Roles_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt       DATETIME2(0) NULL,
        CONSTRAINT PK_Roles PRIMARY KEY (RoleId),
        CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
    );
END
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId          BIGINT IDENTITY(1,1) NOT NULL,
        UserName        NVARCHAR(100) NOT NULL,
        Email           NVARCHAR(255) NULL,
        Mobile          NVARCHAR(30) NULL,
        PasswordHash    NVARCHAR(500) NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt       DATETIME2(0) NULL,
        LastLoginAt     DATETIME2(0) NULL,
        CONSTRAINT PK_Users PRIMARY KEY (UserId)
    );
END
GO

IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRoles
    (
        UserId          BIGINT NOT NULL,
        RoleId          BIGINT NOT NULL,
        AssignedAt      DATETIME2(0) NOT NULL CONSTRAINT DF_UserRoles_AssignedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_UserRoles_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_UserRoles_Role FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId)
    );
END
GO

/* ============================================================
   2. SERVICE CATALOGUE
   ============================================================ */

IF OBJECT_ID(N'dbo.ServiceCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ServiceCategories
    (
        ServiceCategoryId BIGINT IDENTITY(1,1) NOT NULL,
        CategoryName      NVARCHAR(150) NOT NULL,
        Description       NVARCHAR(1000) NULL,
        DisplayOrder      INT NOT NULL CONSTRAINT DF_ServiceCategories_DisplayOrder DEFAULT (0),
        IsActive          BIT NOT NULL CONSTRAINT DF_ServiceCategories_IsActive DEFAULT (1),
        CreatedAt         DATETIME2(0) NOT NULL CONSTRAINT DF_ServiceCategories_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt         DATETIME2(0) NULL,
        CONSTRAINT PK_ServiceCategories PRIMARY KEY (ServiceCategoryId),
        CONSTRAINT UQ_ServiceCategories_CategoryName UNIQUE (CategoryName)
    );
END
GO

IF OBJECT_ID(N'dbo.Services', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Services
    (
        ServiceId             BIGINT IDENTITY(1,1) NOT NULL,
        ServiceCategoryId     BIGINT NOT NULL,
        ServiceName           NVARCHAR(200) NOT NULL,
        ShortDescription      NVARCHAR(500) NULL,
        Description           NVARCHAR(MAX) NULL,
        DisplayOrder          INT NOT NULL CONSTRAINT DF_Services_DisplayOrder DEFAULT (0),
        IsCustomerSelectable  BIT NOT NULL CONSTRAINT DF_Services_IsCustomerSelectable DEFAULT (1),
        RequiresSiteVisit     BIT NOT NULL CONSTRAINT DF_Services_RequiresSiteVisit DEFAULT (0),
        RequiresQuotation     BIT NOT NULL CONSTRAINT DF_Services_RequiresQuotation DEFAULT (1),
        IsActive              BIT NOT NULL CONSTRAINT DF_Services_IsActive DEFAULT (1),
        CreatedAt             DATETIME2(0) NOT NULL CONSTRAINT DF_Services_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt             DATETIME2(0) NULL,
        CONSTRAINT PK_Services PRIMARY KEY (ServiceId),
        CONSTRAINT FK_Services_Category FOREIGN KEY (ServiceCategoryId)
            REFERENCES dbo.ServiceCategories(ServiceCategoryId),
        CONSTRAINT UQ_Services_Category_Name UNIQUE (ServiceCategoryId, ServiceName)
    );
END
GO

IF OBJECT_ID(N'dbo.ServiceOptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ServiceOptions
    (
        ServiceOptionId       BIGINT IDENTITY(1,1) NOT NULL,
        ServiceId             BIGINT NOT NULL,
        OptionCode            NVARCHAR(50) NOT NULL,
        OptionName            NVARCHAR(150) NOT NULL,
        Description           NVARCHAR(500) NULL,
        IsAvailableToCustomer BIT NOT NULL CONSTRAINT DF_ServiceOptions_Available DEFAULT (1),
        IsActive              BIT NOT NULL CONSTRAINT DF_ServiceOptions_IsActive DEFAULT (1),
        DisplayOrder          INT NOT NULL CONSTRAINT DF_ServiceOptions_DisplayOrder DEFAULT (0),
        CreatedAt             DATETIME2(0) NOT NULL CONSTRAINT DF_ServiceOptions_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt             DATETIME2(0) NULL,
        CONSTRAINT PK_ServiceOptions PRIMARY KEY (ServiceOptionId),
        CONSTRAINT FK_ServiceOptions_Service FOREIGN KEY (ServiceId)
            REFERENCES dbo.Services(ServiceId),
        CONSTRAINT UQ_ServiceOptions_Service_Code UNIQUE (ServiceId, OptionCode)
    );
END
GO

/* ============================================================
   3. PROJECT TYPES
   ============================================================ */

IF OBJECT_ID(N'dbo.ProjectTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectTypes
    (
        ProjectTypeId   BIGINT IDENTITY(1,1) NOT NULL,
        ProjectTypeName NVARCHAR(100) NOT NULL,
        Description     NVARCHAR(500) NULL,
        DisplayOrder    INT NOT NULL CONSTRAINT DF_ProjectTypes_DisplayOrder DEFAULT (0),
        IsActive        BIT NOT NULL CONSTRAINT DF_ProjectTypes_IsActive DEFAULT (1),
        CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_ProjectTypes_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt       DATETIME2(0) NULL,
        CONSTRAINT PK_ProjectTypes PRIMARY KEY (ProjectTypeId),
        CONSTRAINT UQ_ProjectTypes_Name UNIQUE (ProjectTypeName)
    );
END
GO

/* ============================================================
   4. CUSTOMERS
   ============================================================ */

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        CustomerId      BIGINT IDENTITY(1,1) NOT NULL,
        CustomerType    NVARCHAR(20) NOT NULL,
        CustomerName    NVARCHAR(200) NOT NULL,
        CompanyName     NVARCHAR(250) NULL,
        Email           NVARCHAR(255) NULL,
        Mobile          NVARCHAR(30) NULL,
        AlternateMobile NVARCHAR(30) NULL,
        GSTNumber       NVARCHAR(50) NULL,
        PANNumber       NVARCHAR(50) NULL,
        Notes           NVARCHAR(MAX) NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT (1),
        CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt       DATETIME2(0) NULL,
        CONSTRAINT PK_Customers PRIMARY KEY (CustomerId),
        CONSTRAINT CK_Customers_CustomerType CHECK (CustomerType IN (N'INDIVIDUAL', N'COMPANY'))
    );
END
GO

IF OBJECT_ID(N'dbo.CustomerAddresses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerAddresses
    (
        CustomerAddressId BIGINT IDENTITY(1,1) NOT NULL,
        CustomerId        BIGINT NOT NULL,
        AddressType       NVARCHAR(50) NOT NULL,
        AddressLine1      NVARCHAR(250) NULL,
        AddressLine2      NVARCHAR(250) NULL,
        Landmark          NVARCHAR(250) NULL,
        City              NVARCHAR(100) NULL,
        District          NVARCHAR(100) NULL,
        State             NVARCHAR(100) NULL,
        Country           NVARCHAR(100) NOT NULL CONSTRAINT DF_CustomerAddresses_Country DEFAULT (N'India'),
        Pincode           NVARCHAR(20) NULL,
        Latitude          DECIMAL(10,7) NULL,
        Longitude         DECIMAL(10,7) NULL,
        IsPrimary         BIT NOT NULL CONSTRAINT DF_CustomerAddresses_IsPrimary DEFAULT (0),
        IsActive          BIT NOT NULL CONSTRAINT DF_CustomerAddresses_IsActive DEFAULT (1),
        CreatedAt         DATETIME2(0) NOT NULL CONSTRAINT DF_CustomerAddresses_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt         DATETIME2(0) NULL,
        CONSTRAINT PK_CustomerAddresses PRIMARY KEY (CustomerAddressId),
        CONSTRAINT FK_CustomerAddresses_Customer FOREIGN KEY (CustomerId)
            REFERENCES dbo.Customers(CustomerId)
    );
END
GO

/* ============================================================
   5. PROJECTS
   ============================================================ */

IF OBJECT_ID(N'dbo.Projects', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Projects
    (
        ProjectId          BIGINT IDENTITY(1,1) NOT NULL,
        ProjectCode        NVARCHAR(50) NOT NULL,
        CustomerId         BIGINT NOT NULL,
        ProjectTypeId      BIGINT NOT NULL,
        ProjectName        NVARCHAR(250) NOT NULL,
        Description        NVARCHAR(MAX) NULL,
        SiteAddress        NVARCHAR(MAX) NULL,
        City               NVARCHAR(100) NULL,
        District           NVARCHAR(100) NULL,
        State              NVARCHAR(100) NULL,
        Pincode            NVARCHAR(20) NULL,
        ApproximateArea    DECIMAL(18,2) NULL,
        AreaUnit           NVARCHAR(20) NULL,
        ExpectedStartDate  DATE NULL,
        ExpectedEndDate    DATE NULL,
        Status              NVARCHAR(50) NOT NULL CONSTRAINT DF_Projects_Status DEFAULT (N'ENQUIRY'),
        CreatedByUserId     BIGINT NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_Projects_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt           DATETIME2(0) NULL,
        CONSTRAINT PK_Projects PRIMARY KEY (ProjectId),
        CONSTRAINT UQ_Projects_ProjectCode UNIQUE (ProjectCode),
        CONSTRAINT FK_Projects_Customer FOREIGN KEY (CustomerId)
            REFERENCES dbo.Customers(CustomerId),
        CONSTRAINT FK_Projects_ProjectType FOREIGN KEY (ProjectTypeId)
            REFERENCES dbo.ProjectTypes(ProjectTypeId),
        CONSTRAINT FK_Projects_CreatedBy FOREIGN KEY (CreatedByUserId)
            REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_Projects_Status CHECK
        (
            Status IN
            (
                N'ENQUIRY',
                N'REVIEW',
                N'SITE_VISIT',
                N'DESIGN',
                N'QUOTATION',
                N'CUSTOMER_APPROVAL',
                N'APPROVED',
                N'PLANNING',
                N'EXECUTION',
                N'INSPECTION',
                N'COMPLETED',
                N'CANCELLED',
                N'ON_HOLD'
            )
        )
    );
END
GO

IF OBJECT_ID(N'dbo.ProjectServices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectServices
    (
        ProjectServiceId    BIGINT IDENTITY(1,1) NOT NULL,
        ProjectId           BIGINT NOT NULL,
        ServiceId           BIGINT NOT NULL,
        ServiceOptionId     BIGINT NULL,
        CustomerRequirement NVARCHAR(MAX) NULL,
        Quantity            DECIMAL(18,3) NULL,
        Unit                NVARCHAR(50) NULL,
        Status              NVARCHAR(50) NOT NULL CONSTRAINT DF_ProjectServices_Status DEFAULT (N'REQUESTED'),
        EstimatedAmount     DECIMAL(18,2) NULL,
        ApprovedAmount      DECIMAL(18,2) NULL,
        CustomerNotes      NVARCHAR(MAX) NULL,
        InternalNotes       NVARCHAR(MAX) NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_ProjectServices_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt           DATETIME2(0) NULL,
        CONSTRAINT PK_ProjectServices PRIMARY KEY (ProjectServiceId),
        CONSTRAINT FK_ProjectServices_Project FOREIGN KEY (ProjectId)
            REFERENCES dbo.Projects(ProjectId),
        CONSTRAINT FK_ProjectServices_Service FOREIGN KEY (ServiceId)
            REFERENCES dbo.Services(ServiceId),
        CONSTRAINT FK_ProjectServices_Option FOREIGN KEY (ServiceOptionId)
            REFERENCES dbo.ServiceOptions(ServiceOptionId),
        CONSTRAINT CK_ProjectServices_Status CHECK
        (
            Status IN
            (
                N'REQUESTED',
                N'REVIEWING',
                N'QUOTED',
                N'APPROVED',
                N'ASSIGNED',
                N'IN_PROGRESS',
                N'COMPLETED',
                N'CANCELLED',
                N'ON_HOLD'
            )
        )
    );
END
GO

/* ============================================================
   6. LEADS / ENQUIRIES
   ============================================================ */

IF OBJECT_ID(N'dbo.Leads', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Leads
    (
        LeadId             BIGINT IDENTITY(1,1) NOT NULL,
        LeadCode           NVARCHAR(50) NOT NULL,
        CustomerId         BIGINT NULL,
        ProjectId          BIGINT NULL,
        LeadSource         NVARCHAR(100) NULL,
        CustomerName       NVARCHAR(200) NOT NULL,
        CompanyName        NVARCHAR(250) NULL,
        Email              NVARCHAR(255) NULL,
        Mobile             NVARCHAR(30) NULL,
        ProjectTypeId      BIGINT NULL,
        Location           NVARCHAR(500) NULL,
        ApproximateArea    DECIMAL(18,2) NULL,
        AreaUnit           NVARCHAR(20) NULL,
        Requirement        NVARCHAR(MAX) NULL,
        Status             NVARCHAR(50) NOT NULL CONSTRAINT DF_Leads_Status DEFAULT (N'NEW'),
        AssignedToUserId   BIGINT NULL,
        CreatedAt          DATETIME2(0) NOT NULL CONSTRAINT DF_Leads_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt          DATETIME2(0) NULL,
        CONSTRAINT PK_Leads PRIMARY KEY (LeadId),
        CONSTRAINT UQ_Leads_LeadCode UNIQUE (LeadCode),
        CONSTRAINT FK_Leads_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
        CONSTRAINT FK_Leads_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(ProjectId),
        CONSTRAINT FK_Leads_ProjectType FOREIGN KEY (ProjectTypeId) REFERENCES dbo.ProjectTypes(ProjectTypeId),
        CONSTRAINT FK_Leads_AssignedTo FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_Leads_Status CHECK
        (
            Status IN
            (
                N'NEW',
                N'CONTACTED',
                N'QUALIFIED',
                N'SITE_VISIT',
                N'QUOTATION',
                N'WON',
                N'LOST',
                N'ON_HOLD'
            )
        )
    );
END
GO

IF OBJECT_ID(N'dbo.LeadServices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeadServices
    (
        LeadServiceId   BIGINT IDENTITY(1,1) NOT NULL,
        LeadId          BIGINT NOT NULL,
        ServiceId       BIGINT NOT NULL,
        ServiceOptionId BIGINT NULL,
        Requirement     NVARCHAR(MAX) NULL,
        CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_LeadServices_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT PK_LeadServices PRIMARY KEY (LeadServiceId),
        CONSTRAINT FK_LeadServices_Lead FOREIGN KEY (LeadId) REFERENCES dbo.Leads(LeadId),
        CONSTRAINT FK_LeadServices_Service FOREIGN KEY (ServiceId) REFERENCES dbo.Services(ServiceId),
        CONSTRAINT FK_LeadServices_Option FOREIGN KEY (ServiceOptionId) REFERENCES dbo.ServiceOptions(ServiceOptionId)
    );
END
GO

/* ============================================================
   7. RESOURCES / VENDORS
   ============================================================ */

IF OBJECT_ID(N'dbo.ResourceTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ResourceTypes
    (
        ResourceTypeId BIGINT IDENTITY(1,1) NOT NULL,
        ResourceTypeName NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ResourceTypes_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ResourceTypes_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT PK_ResourceTypes PRIMARY KEY (ResourceTypeId),
        CONSTRAINT UQ_ResourceTypes_Name UNIQUE (ResourceTypeName)
    );
END
GO

IF OBJECT_ID(N'dbo.Resources', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Resources
    (
        ResourceId       BIGINT IDENTITY(1,1) NOT NULL,
        ResourceTypeId   BIGINT NOT NULL,
        ResourceName     NVARCHAR(250) NOT NULL,
        ContactPerson    NVARCHAR(200) NULL,
        Mobile           NVARCHAR(30) NULL,
        AlternateMobile  NVARCHAR(30) NULL,
        Email            NVARCHAR(255) NULL,
        Address          NVARCHAR(MAX) NULL,
        City             NVARCHAR(100) NULL,
        District         NVARCHAR(100) NULL,
        State            NVARCHAR(100) NULL,
        GSTNumber        NVARCHAR(50) NULL,
        InternalRating   DECIMAL(3,2) NULL,
        Notes             NVARCHAR(MAX) NULL,
        IsActive         BIT NOT NULL CONSTRAINT DF_Resources_IsActive DEFAULT (1),
        CreatedAt        DATETIME2(0) NOT NULL CONSTRAINT DF_Resources_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt        DATETIME2(0) NULL,
        CONSTRAINT PK_Resources PRIMARY KEY (ResourceId),
        CONSTRAINT FK_Resources_Type FOREIGN KEY (ResourceTypeId) REFERENCES dbo.ResourceTypes(ResourceTypeId),
        CONSTRAINT CK_Resources_Rating CHECK (InternalRating IS NULL OR (InternalRating >= 0 AND InternalRating <= 5))
    );
END
GO

IF OBJECT_ID(N'dbo.ResourceServices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ResourceServices
    (
        ResourceServiceId BIGINT IDENTITY(1,1) NOT NULL,
        ResourceId        BIGINT NOT NULL,
        ServiceId        BIGINT NOT NULL,
        Notes             NVARCHAR(500) NULL,
        IsPreferred       BIT NOT NULL CONSTRAINT DF_ResourceServices_IsPreferred DEFAULT (0),
        IsActive          BIT NOT NULL CONSTRAINT DF_ResourceServices_IsActive DEFAULT (1),
        CreatedAt         DATETIME2(0) NOT NULL CONSTRAINT DF_ResourceServices_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT PK_ResourceServices PRIMARY KEY (ResourceServiceId),
        CONSTRAINT FK_ResourceServices_Resource FOREIGN KEY (ResourceId) REFERENCES dbo.Resources(ResourceId),
        CONSTRAINT FK_ResourceServices_Service FOREIGN KEY (ServiceId) REFERENCES dbo.Services(ServiceId),
        CONSTRAINT UQ_ResourceServices UNIQUE (ResourceId, ServiceId)
    );
END
GO

IF OBJECT_ID(N'dbo.ProjectResources', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectResources
    (
        ProjectResourceId BIGINT IDENTITY(1,1) NOT NULL,
        ProjectId        BIGINT NOT NULL,
        ProjectServiceId BIGINT NULL,
        ResourceId       BIGINT NOT NULL,
        AssignedByUserId BIGINT NULL,
        AssignmentStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_ProjectResources_Status DEFAULT (N'ASSIGNED'),
        AgreedAmount     DECIMAL(18,2) NULL,
        StartDate        DATE NULL,
        EndDate          DATE NULL,
        ScopeOfWork      NVARCHAR(MAX) NULL,
        Notes            NVARCHAR(MAX) NULL,
        CreatedAt        DATETIME2(0) NOT NULL CONSTRAINT DF_ProjectResources_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt        DATETIME2(0) NULL,
        CONSTRAINT PK_ProjectResources PRIMARY KEY (ProjectResourceId),
        CONSTRAINT FK_ProjectResources_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(ProjectId),
        CONSTRAINT FK_ProjectResources_ProjectService FOREIGN KEY (ProjectServiceId) REFERENCES dbo.ProjectServices(ProjectServiceId),
        CONSTRAINT FK_ProjectResources_Resource FOREIGN KEY (ResourceId) REFERENCES dbo.Resources(ResourceId),
        CONSTRAINT FK_ProjectResources_AssignedBy FOREIGN KEY (AssignedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_ProjectResources_Status CHECK
        (
            AssignmentStatus IN
            (
                N'ASSIGNED',
                N'CONFIRMED',
                N'IN_PROGRESS',
                N'COMPLETED',
                N'CANCELLED'
            )
        )
    );
END
GO

/* ============================================================
   8. SITE VISITS
   ============================================================ */

IF OBJECT_ID(N'dbo.SiteVisits', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SiteVisits
    (
        SiteVisitId       BIGINT IDENTITY(1,1) NOT NULL,
        ProjectId         BIGINT NOT NULL,
        AssignedToUserId  BIGINT NULL,
        ScheduledAt       DATETIME2(0) NULL,
        CompletedAt       DATETIME2(0) NULL,
        Status             NVARCHAR(50) NOT NULL CONSTRAINT DF_SiteVisits_Status DEFAULT (N'SCHEDULED'),
        SiteCondition     NVARCHAR(MAX) NULL,
        Measurements      NVARCHAR(MAX) NULL,
        CustomerNotes     NVARCHAR(MAX) NULL,
        InternalNotes     NVARCHAR(MAX) NULL,
        CreatedAt         DATETIME2(0) NOT NULL CONSTRAINT DF_SiteVisits_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt         DATETIME2(0) NULL,
        CONSTRAINT PK_SiteVisits PRIMARY KEY (SiteVisitId),
        CONSTRAINT FK_SiteVisits_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(ProjectId),
        CONSTRAINT FK_SiteVisits_AssignedTo FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_SiteVisits_Status CHECK
        (
            Status IN (N'SCHEDULED', N'CONFIRMED', N'COMPLETED', N'CANCELLED', N'RESCHEDULED')
        )
    );
END
GO

/* ============================================================
   9. PROJECT STAGES / TASKS / PROGRESS
   ============================================================ */

IF OBJECT_ID(N'dbo.ProjectStages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectStages
    (
        ProjectStageId BIGINT IDENTITY(1,1) NOT NULL,
        ProjectId      BIGINT NOT NULL,
        StageCode      NVARCHAR(50) NOT NULL,
        StageName      NVARCHAR(150) NOT NULL,
        DisplayOrder   INT NOT NULL CONSTRAINT DF_ProjectStages_DisplayOrder DEFAULT (0),
        Status         NVARCHAR(50) NOT NULL CONSTRAINT DF_ProjectStages_Status DEFAULT (N'PENDING'),
        ProgressPercent DECIMAL(5,2) NOT NULL CONSTRAINT DF_ProjectStages_Progress DEFAULT (0),
        StartDate      DATE NULL,
        EndDate        DATE NULL,
        Notes          NVARCHAR(MAX) NULL,
        CreatedAt      DATETIME2(0) NOT NULL CONSTRAINT DF_ProjectStages_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt      DATETIME2(0) NULL,
        CONSTRAINT PK_ProjectStages PRIMARY KEY (ProjectStageId),
        CONSTRAINT FK_ProjectStages_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(ProjectId),
        CONSTRAINT CK_ProjectStages_Progress CHECK (ProgressPercent >= 0 AND ProgressPercent <= 100),
        CONSTRAINT CK_ProjectStages_Status CHECK
        (
            Status IN (N'PENDING', N'IN_PROGRESS', N'COMPLETED', N'ON_HOLD', N'CANCELLED')
        )
    );
END
GO

IF OBJECT_ID(N'dbo.ProjectTasks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectTasks
    (
        ProjectTaskId     BIGINT IDENTITY(1,1) NOT NULL,
        ProjectId         BIGINT NOT NULL,
        ProjectStageId    BIGINT NULL,
        ProjectServiceId  BIGINT NULL,
        TaskName          NVARCHAR(250) NOT NULL,
        Description       NVARCHAR(MAX) NULL,
        AssignedToUserId  BIGINT NULL,
        AssignedResourceId BIGINT NULL,
        Status             NVARCHAR(50) NOT NULL CONSTRAINT DF_ProjectTasks_Status DEFAULT (N'PENDING'),
        Priority           NVARCHAR(20) NOT NULL CONSTRAINT DF_ProjectTasks_Priority DEFAULT (N'MEDIUM'),
        StartDate          DATE NULL,
        DueDate            DATE NULL,
        CompletedDate      DATE NULL,
        ProgressPercent    DECIMAL(5,2) NOT NULL CONSTRAINT DF_ProjectTasks_Progress DEFAULT (0),
        Notes              NVARCHAR(MAX) NULL,
        CreatedAt          DATETIME2(0) NOT NULL CONSTRAINT DF_ProjectTasks_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt          DATETIME2(0) NULL,
        CONSTRAINT PK_ProjectTasks PRIMARY KEY (ProjectTaskId),
        CONSTRAINT FK_ProjectTasks_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(ProjectId),
        CONSTRAINT FK_ProjectTasks_Stage FOREIGN KEY (ProjectStageId) REFERENCES dbo.ProjectStages(ProjectStageId),
        CONSTRAINT FK_ProjectTasks_ProjectService FOREIGN KEY (ProjectServiceId) REFERENCES dbo.ProjectServices(ProjectServiceId),
        CONSTRAINT FK_ProjectTasks_User FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_ProjectTasks_Resource FOREIGN KEY (AssignedResourceId) REFERENCES dbo.Resources(ResourceId),
        CONSTRAINT CK_ProjectTasks_Progress CHECK (ProgressPercent >= 0 AND ProgressPercent <= 100),
        CONSTRAINT CK_ProjectTasks_Status CHECK
        (
            Status IN (N'PENDING', N'IN_PROGRESS', N'COMPLETED', N'ON_HOLD', N'CANCELLED')
        ),
        CONSTRAINT CK_ProjectTasks_Priority CHECK
        (
            Priority IN (N'LOW', N'MEDIUM', N'HIGH', N'URGENT')
        )
    );
END
GO

IF OBJECT_ID(N'dbo.ProjectProgress', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectProgress
    (
        ProjectProgressId BIGINT IDENTITY(1,1) NOT NULL,
        ProjectId        BIGINT NOT NULL,
        ProjectStageId   BIGINT NULL,
        ProjectTaskId    BIGINT NULL,
        ProgressPercent  DECIMAL(5,2) NOT NULL,
        UpdateTitle      NVARCHAR(250) NULL,
        UpdateDescription NVARCHAR(MAX) NULL,
        UpdatedByUserId  BIGINT NULL,
        CreatedAt        DATETIME2(0) NOT NULL CONSTRAINT DF_ProjectProgress_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT PK_ProjectProgress PRIMARY KEY (ProjectProgressId),
        CONSTRAINT FK_ProjectProgress_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(ProjectId),
        CONSTRAINT FK_ProjectProgress_Stage FOREIGN KEY (ProjectStageId) REFERENCES dbo.ProjectStages(ProjectStageId),
        CONSTRAINT FK_ProjectProgress_Task FOREIGN KEY (ProjectTaskId) REFERENCES dbo.ProjectTasks(ProjectTaskId),
        CONSTRAINT FK_ProjectProgress_User FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_ProjectProgress_Percent CHECK (ProgressPercent >= 0 AND ProgressPercent <= 100)
    );
END
GO

/* ============================================================
   10. DESIGNS / DOCUMENTS
   ============================================================ */

IF OBJECT_ID(N'dbo.Designs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Designs
    (
        DesignId          BIGINT IDENTITY(1,1) NOT NULL,
        ProjectId         BIGINT NOT NULL,
        ProjectServiceId  BIGINT NULL,
        DesignType        NVARCHAR(100) NOT NULL,
        VersionNumber     INT NOT NULL CONSTRAINT DF_Designs_Version DEFAULT (1),
        DesignTitle       NVARCHAR(250) NULL,
        Description       NVARCHAR(MAX) NULL,
        Status             NVARCHAR(50) NOT NULL CONSTRAINT DF_Designs_Status DEFAULT (N'DRAFT'),
        CreatedByUserId   BIGINT NULL,
        SubmittedAt       DATETIME2(0) NULL,
        ApprovedAt        DATETIME2(0) NULL,
        CreatedAt         DATETIME2(0) NOT NULL CONSTRAINT DF_Designs_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt         DATETIME2(0) NULL,
        CONSTRAINT PK_Designs PRIMARY KEY (DesignId),
        CONSTRAINT FK_Designs_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(ProjectId),
        CONSTRAINT FK_Designs_ProjectService FOREIGN KEY (ProjectServiceId) REFERENCES dbo.ProjectServices(ProjectServiceId),
        CONSTRAINT FK_Designs_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_Designs_Status CHECK
        (
            Status IN (N'DRAFT', N'SUBMITTED', N'CUSTOMER_REVIEW', N'REVISION_REQUIRED', N'APPROVED', N'REJECTED', N'ARCHIVED')
        )
    );
END
GO

IF OBJECT_ID(N'dbo.DocumentTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentTypes
    (
        DocumentTypeId   BIGINT IDENTITY(1,1) NOT NULL,
        DocumentTypeName NVARCHAR(150) NOT NULL,
        Description      NVARCHAR(500) NULL,
        IsActive         BIT NOT NULL CONSTRAINT DF_DocumentTypes_IsActive DEFAULT (1),
        CreatedAt        DATETIME2(0) NOT NULL CONSTRAINT DF_DocumentTypes_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT PK_DocumentTypes PRIMARY KEY (DocumentTypeId),
        CONSTRAINT UQ_DocumentTypes_Name UNIQUE (DocumentTypeName)
    );
END
GO

IF OBJECT_ID(N'dbo.Documents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Documents
    (
        DocumentId       BIGINT IDENTITY(1,1) NOT NULL,
        ProjectId        BIGINT NULL,
        DesignId         BIGINT NULL,
        DocumentTypeId   BIGINT NOT NULL,
        FileName         NVARCHAR(255) NOT NULL,
        OriginalFileName NVARCHAR(255) NULL,
        FilePath         NVARCHAR(1000) NOT NULL,
        ContentType      NVARCHAR(150) NULL,
        FileSizeBytes    BIGINT NULL,
        VersionNumber    INT NOT NULL CONSTRAINT DF_Documents_Version DEFAULT (1),
        IsCustomerVisible BIT NOT NULL CONSTRAINT DF_Documents_CustomerVisible DEFAULT (0),
        UploadedByUserId BIGINT NULL,
        CreatedAt        DATETIME2(0) NOT NULL CONSTRAINT DF_Documents_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT PK_Documents PRIMARY KEY (DocumentId),
        CONSTRAINT FK_Documents_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(ProjectId),
        CONSTRAINT FK_Documents_Design FOREIGN KEY (DesignId) REFERENCES dbo.Designs(DesignId),
        CONSTRAINT FK_Documents_Type FOREIGN KEY (DocumentTypeId) REFERENCES dbo.DocumentTypes(DocumentTypeId),
        CONSTRAINT FK_Documents_UploadedBy FOREIGN KEY (UploadedByUserId) REFERENCES dbo.Users(UserId)
    );
END
GO

/* ============================================================
   11. QUOTATIONS
   ============================================================ */

IF OBJECT_ID(N'dbo.Quotations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Quotations
    (
        QuotationId      BIGINT IDENTITY(1,1) NOT NULL,
        QuotationNumber  NVARCHAR(50) NOT NULL,
        ProjectId        BIGINT NOT NULL,
        RevisionNumber   INT NOT NULL CONSTRAINT DF_Quotations_Revision DEFAULT (1),
        QuotationDate    DATE NOT NULL CONSTRAINT DF_Quotations_Date DEFAULT (CONVERT(DATE, GETDATE())),
        ValidUntil       DATE NULL,
        SubTotal         DECIMAL(18,2) NOT NULL CONSTRAINT DF_Quotations_SubTotal DEFAULT (0),
        DiscountAmount   DECIMAL(18,2) NOT NULL CONSTRAINT DF_Quotations_Discount DEFAULT (0),
        TaxAmount        DECIMAL(18,2) NOT NULL CONSTRAINT DF_Quotations_Tax DEFAULT (0),
        GrandTotal       DECIMAL(18,2) NOT NULL CONSTRAINT DF_Quotations_GrandTotal DEFAULT (0),
        Status            NVARCHAR(50) NOT NULL CONSTRAINT DF_Quotations_Status DEFAULT (N'DRAFT'),
        Notes             NVARCHAR(MAX) NULL,
        CreatedByUserId  BIGINT NULL,
        SentAt            DATETIME2(0) NULL,
        ApprovedAt        DATETIME2(0) NULL,
        CreatedAt         DATETIME2(0) NOT NULL CONSTRAINT DF_Quotations_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt         DATETIME2(0) NULL,
        CONSTRAINT PK_Quotations PRIMARY KEY (QuotationId),
        CONSTRAINT UQ_Quotations_Number UNIQUE (QuotationNumber),
        CONSTRAINT FK_Quotations_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(ProjectId),
        CONSTRAINT FK_Quotations_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_Quotations_Status CHECK
        (
            Status IN (N'DRAFT', N'SENT', N'CUSTOMER_REVIEW', N'REVISION_REQUESTED', N'APPROVED', N'REJECTED', N'EXPIRED', N'CANCELLED')
        )
    );
END
GO

IF OBJECT_ID(N'dbo.QuotationItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuotationItems
    (
        QuotationItemId BIGINT IDENTITY(1,1) NOT NULL,
        QuotationId     BIGINT NOT NULL,
        ProjectServiceId BIGINT NULL,
        ItemType        NVARCHAR(30) NOT NULL,
        Description     NVARCHAR(500) NOT NULL,
        Quantity        DECIMAL(18,3) NULL,
        Unit            NVARCHAR(50) NULL,
        UnitPrice       DECIMAL(18,2) NOT NULL CONSTRAINT DF_QuotationItems_UnitPrice DEFAULT (0),
        DiscountAmount  DECIMAL(18,2) NOT NULL CONSTRAINT DF_QuotationItems_Discount DEFAULT (0),
        TaxPercent      DECIMAL(8,3) NOT NULL CONSTRAINT DF_QuotationItems_TaxPercent DEFAULT (0),
        TaxAmount       DECIMAL(18,2) NOT NULL CONSTRAINT DF_QuotationItems_TaxAmount DEFAULT (0),
        LineTotal       DECIMAL(18,2) NOT NULL CONSTRAINT DF_QuotationItems_LineTotal DEFAULT (0),
        DisplayOrder    INT NOT NULL CONSTRAINT DF_QuotationItems_DisplayOrder DEFAULT (0),
        Notes           NVARCHAR(MAX) NULL,
        CONSTRAINT PK_QuotationItems PRIMARY KEY (QuotationItemId),
        CONSTRAINT FK_QuotationItems_Quotation FOREIGN KEY (QuotationId) REFERENCES dbo.Quotations(QuotationId),
        CONSTRAINT FK_QuotationItems_ProjectService FOREIGN KEY (ProjectServiceId) REFERENCES dbo.ProjectServices(ProjectServiceId),
        CONSTRAINT CK_QuotationItems_ItemType CHECK
        (
            ItemType IN (N'SERVICE', N'SUPPLY', N'RESOURCE', N'COORDINATION', N'OTHER')
        )
    );
END
GO

IF OBJECT_ID(N'dbo.CustomerApprovals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerApprovals
    (
        CustomerApprovalId BIGINT IDENTITY(1,1) NOT NULL,
        ProjectId          BIGINT NOT NULL,
        QuotationId       BIGINT NULL,
        DesignId          BIGINT NULL,
        ApprovalType      NVARCHAR(50) NOT NULL,
        Status             NVARCHAR(50) NOT NULL CONSTRAINT DF_CustomerApprovals_Status DEFAULT (N'PENDING'),
        Comments           NVARCHAR(MAX) NULL,
        ApprovedByUserId  BIGINT NULL,
        ApprovedAt        DATETIME2(0) NULL,
        CreatedAt         DATETIME2(0) NOT NULL CONSTRAINT DF_CustomerApprovals_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT PK_CustomerApprovals PRIMARY KEY (CustomerApprovalId),
        CONSTRAINT FK_CustomerApprovals_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(ProjectId),
        CONSTRAINT FK_CustomerApprovals_Quotation FOREIGN KEY (QuotationId) REFERENCES dbo.Quotations(QuotationId),
        CONSTRAINT FK_CustomerApprovals_Design FOREIGN KEY (DesignId) REFERENCES dbo.Designs(DesignId),
        CONSTRAINT FK_CustomerApprovals_User FOREIGN KEY (ApprovedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_CustomerApprovals_Type CHECK
        (
            ApprovalType IN (N'DESIGN', N'QUOTATION', N'MATERIAL', N'PROJECT', N'OTHER')
        ),
        CONSTRAINT CK_CustomerApprovals_Status CHECK
        (
            Status IN (N'PENDING', N'APPROVED', N'REJECTED', N'REVISION_REQUESTED')
        )
    );
END
GO

/* ============================================================
   12. BILLING / PAYMENTS
   ============================================================ */

IF OBJECT_ID(N'dbo.Invoices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Invoices
    (
        InvoiceId       BIGINT IDENTITY(1,1) NOT NULL,
        InvoiceNumber   NVARCHAR(50) NOT NULL,
        ProjectId       BIGINT NOT NULL,
        CustomerId      BIGINT NOT NULL,
        InvoiceDate     DATE NOT NULL CONSTRAINT DF_Invoices_Date DEFAULT (CONVERT(DATE, GETDATE())),
        DueDate         DATE NULL,
        SubTotal        DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_SubTotal DEFAULT (0),
        DiscountAmount  DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_Discount DEFAULT (0),
        TaxAmount       DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_Tax DEFAULT (0),
        GrandTotal      DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_GrandTotal DEFAULT (0),
        PaidAmount      DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_Paid DEFAULT (0),
        Status          NVARCHAR(50) NOT NULL CONSTRAINT DF_Invoices_Status DEFAULT (N'DRAFT'),
        Notes           NVARCHAR(MAX) NULL,
        CreatedByUserId BIGINT NULL,
        CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Invoices_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt       DATETIME2(0) NULL,
        CONSTRAINT PK_Invoices PRIMARY KEY (InvoiceId),
        CONSTRAINT UQ_Invoices_Number UNIQUE (InvoiceNumber),
        CONSTRAINT FK_Invoices_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(ProjectId),
        CONSTRAINT FK_Invoices_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
        CONSTRAINT FK_Invoices_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_Invoices_Status CHECK
        (
            Status IN (N'DRAFT', N'ISSUED', N'PARTIALLY_PAID', N'PAID', N'OVERDUE', N'CANCELLED')
        )
    );
END
GO

IF OBJECT_ID(N'dbo.InvoiceItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvoiceItems
    (
        InvoiceItemId   BIGINT IDENTITY(1,1) NOT NULL,
        InvoiceId       BIGINT NOT NULL,
        Description     NVARCHAR(500) NOT NULL,
        Quantity        DECIMAL(18,3) NULL,
        Unit            NVARCHAR(50) NULL,
        UnitPrice       DECIMAL(18,2) NOT NULL CONSTRAINT DF_InvoiceItems_UnitPrice DEFAULT (0),
        TaxPercent      DECIMAL(8,3) NOT NULL CONSTRAINT DF_InvoiceItems_TaxPercent DEFAULT (0),
        TaxAmount       DECIMAL(18,2) NOT NULL CONSTRAINT DF_InvoiceItems_TaxAmount DEFAULT (0),
        LineTotal       DECIMAL(18,2) NOT NULL CONSTRAINT DF_InvoiceItems_LineTotal DEFAULT (0),
        DisplayOrder    INT NOT NULL CONSTRAINT DF_InvoiceItems_DisplayOrder DEFAULT (0),
        CONSTRAINT PK_InvoiceItems PRIMARY KEY (InvoiceItemId),
        CONSTRAINT FK_InvoiceItems_Invoice FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(InvoiceId)
    );
END
GO

IF OBJECT_ID(N'dbo.Payments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments
    (
        PaymentId       BIGINT IDENTITY(1,1) NOT NULL,
        InvoiceId       BIGINT NULL,
        ProjectId       BIGINT NOT NULL,
        CustomerId      BIGINT NOT NULL,
        PaymentReference NVARCHAR(100) NULL,
        PaymentDate     DATE NOT NULL CONSTRAINT DF_Payments_Date DEFAULT (CONVERT(DATE, GETDATE())),
        Amount          DECIMAL(18,2) NOT NULL,
        PaymentMethod   NVARCHAR(50) NOT NULL,
        TransactionReference NVARCHAR(150) NULL,
        Notes           NVARCHAR(MAX) NULL,
        ReceivedByUserId BIGINT NULL,
        CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Payments_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT PK_Payments PRIMARY KEY (PaymentId),
        CONSTRAINT FK_Payments_Invoice FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(InvoiceId),
        CONSTRAINT FK_Payments_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(ProjectId),
        CONSTRAINT FK_Payments_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
        CONSTRAINT FK_Payments_ReceivedBy FOREIGN KEY (ReceivedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_Payments_Amount CHECK (Amount > 0)
    );
END
GO

/* ============================================================
   13. COMMUNICATION
   ============================================================ */

IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications
    (
        NotificationId BIGINT IDENTITY(1,1) NOT NULL,
        UserId         BIGINT NULL,
        CustomerId     BIGINT NULL,
        ProjectId      BIGINT NULL,
        NotificationType NVARCHAR(50) NOT NULL,
        Title          NVARCHAR(250) NOT NULL,
        Message        NVARCHAR(MAX) NOT NULL,
        IsRead         BIT NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT (0),
        ReadAt         DATETIME2(0) NULL,
        CreatedAt      DATETIME2(0) NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT PK_Notifications PRIMARY KEY (NotificationId),
        CONSTRAINT FK_Notifications_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_Notifications_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
        CONSTRAINT FK_Notifications_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(ProjectId)
    );
END
GO

IF OBJECT_ID(N'dbo.Messages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Messages
    (
        MessageId       BIGINT IDENTITY(1,1) NOT NULL,
        ProjectId       BIGINT NULL,
        UserId           BIGINT NULL,
        CustomerId       BIGINT NULL,
        SenderType      NVARCHAR(20) NOT NULL,
        MessageText     NVARCHAR(MAX) NOT NULL,
        IsRead          BIT NOT NULL CONSTRAINT DF_Messages_IsRead DEFAULT (0),
        CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Messages_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT PK_Messages PRIMARY KEY (MessageId),
        CONSTRAINT FK_Messages_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(ProjectId),
        CONSTRAINT FK_Messages_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_Messages_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
        CONSTRAINT CK_Messages_SenderType CHECK (SenderType IN (N'ADMIN', N'CUSTOMER'))
    );
END
GO

/* ============================================================
   14. ACTIVITY / AUDIT
   ============================================================ */

IF OBJECT_ID(N'dbo.ActivityLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ActivityLogs
    (
        ActivityLogId BIGINT IDENTITY(1,1) NOT NULL,
        UserId        BIGINT NULL,
        CustomerId    BIGINT NULL,
        ProjectId     BIGINT NULL,
        EntityType    NVARCHAR(100) NULL,
        EntityId      BIGINT NULL,
        Action        NVARCHAR(100) NOT NULL,
        Description   NVARCHAR(MAX) NULL,
        IpAddress     NVARCHAR(64) NULL,
        CreatedAt     DATETIME2(0) NOT NULL CONSTRAINT DF_ActivityLogs_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT PK_ActivityLogs PRIMARY KEY (ActivityLogId),
        CONSTRAINT FK_ActivityLogs_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_ActivityLogs_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
        CONSTRAINT FK_ActivityLogs_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(ProjectId)
    );
END
GO

/* ============================================================
   15. INDEXES
   ============================================================ */

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_Email' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE INDEX IX_Users_Email ON dbo.Users(Email);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_Mobile' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE INDEX IX_Users_Mobile ON dbo.Users(Mobile);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Services_Category' AND object_id = OBJECT_ID(N'dbo.Services'))
    CREATE INDEX IX_Services_Category ON dbo.Services(ServiceCategoryId, IsActive, DisplayOrder);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ServiceOptions_Service' AND object_id = OBJECT_ID(N'dbo.ServiceOptions'))
    CREATE INDEX IX_ServiceOptions_Service ON dbo.ServiceOptions(ServiceId, IsActive, DisplayOrder);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Customers_Mobile' AND object_id = OBJECT_ID(N'dbo.Customers'))
    CREATE INDEX IX_Customers_Mobile ON dbo.Customers(Mobile);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Customers_Email' AND object_id = OBJECT_ID(N'dbo.Customers'))
    CREATE INDEX IX_Customers_Email ON dbo.Customers(Email);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Projects_Customer' AND object_id = OBJECT_ID(N'dbo.Projects'))
    CREATE INDEX IX_Projects_Customer ON dbo.Projects(CustomerId, Status);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Projects_Status' AND object_id = OBJECT_ID(N'dbo.Projects'))
    CREATE INDEX IX_Projects_Status ON dbo.Projects(Status, CreatedAt);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectServices_Project' AND object_id = OBJECT_ID(N'dbo.ProjectServices'))
    CREATE INDEX IX_ProjectServices_Project ON dbo.ProjectServices(ProjectId, Status);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Leads_Status' AND object_id = OBJECT_ID(N'dbo.Leads'))
    CREATE INDEX IX_Leads_Status ON dbo.Leads(Status, CreatedAt);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Leads_Mobile' AND object_id = OBJECT_ID(N'dbo.Leads'))
    CREATE INDEX IX_Leads_Mobile ON dbo.Leads(Mobile);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Resources_Type' AND object_id = OBJECT_ID(N'dbo.Resources'))
    CREATE INDEX IX_Resources_Type ON dbo.Resources(ResourceTypeId, IsActive);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectResources_Project' AND object_id = OBJECT_ID(N'dbo.ProjectResources'))
    CREATE INDEX IX_ProjectResources_Project ON dbo.ProjectResources(ProjectId, AssignmentStatus);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SiteVisits_Project' AND object_id = OBJECT_ID(N'dbo.SiteVisits'))
    CREATE INDEX IX_SiteVisits_Project ON dbo.SiteVisits(ProjectId, ScheduledAt);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectTasks_Project' AND object_id = OBJECT_ID(N'dbo.ProjectTasks'))
    CREATE INDEX IX_ProjectTasks_Project ON dbo.ProjectTasks(ProjectId, Status);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_Project' AND object_id = OBJECT_ID(N'dbo.Documents'))
    CREATE INDEX IX_Documents_Project ON dbo.Documents(ProjectId, CreatedAt);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Quotations_Project' AND object_id = OBJECT_ID(N'dbo.Quotations'))
    CREATE INDEX IX_Quotations_Project ON dbo.Quotations(ProjectId, Status);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Invoices_Customer' AND object_id = OBJECT_ID(N'dbo.Invoices'))
    CREATE INDEX IX_Invoices_Customer ON dbo.Invoices(CustomerId, Status);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Payments_Project' AND object_id = OBJECT_ID(N'dbo.Payments'))
    CREATE INDEX IX_Payments_Project ON dbo.Payments(ProjectId, PaymentDate);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ActivityLogs_Project' AND object_id = OBJECT_ID(N'dbo.ActivityLogs'))
    CREATE INDEX IX_ActivityLogs_Project ON dbo.ActivityLogs(ProjectId, CreatedAt);
GO

/* ============================================================
   16. MASTER DATA
   ============================================================ */

INSERT INTO dbo.Roles (RoleName, Description)
SELECT v.RoleName, v.Description
FROM
(
    VALUES
    (N'Super Admin', N'Full system access'),
    (N'Admin', N'General administration'),
    (N'Sales', N'Lead and customer management'),
    (N'Designer', N'Architecture and design management'),
    (N'Project Coordinator', N'Project and execution coordination'),
    (N'Accounts', N'Quotation, invoice and payment management'),
    (N'Resource Manager', N'Resource and vendor management'),
    (N'Customer', N'Customer portal access')
) v(RoleName, Description)
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.Roles r WHERE r.RoleName = v.RoleName
);
GO

INSERT INTO dbo.ProjectTypes (ProjectTypeName, Description, DisplayOrder)
SELECT v.ProjectTypeName, v.Description, v.DisplayOrder
FROM
(
    VALUES
    (N'Home', N'Residential home project', 1),
    (N'Office', N'Office setup or renovation', 2),
    (N'Shop', N'Retail shop project', 3),
    (N'Showroom', N'Showroom project', 4),
    (N'Clinic', N'Clinic or healthcare space', 5),
    (N'Institution', N'Institutional project', 6),
    (N'Commercial', N'Other commercial project', 7),
    (N'Renovation', N'Renovation or modification project', 8),
    (N'Other', N'Other project type', 9)
) v(ProjectTypeName, Description, DisplayOrder)
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.ProjectTypes p WHERE p.ProjectTypeName = v.ProjectTypeName
);
GO

INSERT INTO dbo.ResourceTypes (ResourceTypeName, Description)
SELECT v.ResourceTypeName, v.Description
FROM
(
    VALUES
    (N'Civil Contractor', N'Civil and site work resource'),
    (N'Tile Installer', N'Tile laying and setting resource'),
    (N'Electrician', N'Electrical work resource'),
    (N'Plumber', N'Plumbing work resource'),
    (N'False Ceiling', N'False ceiling installation resource'),
    (N'Painter', N'Painting and wall finishing resource'),
    (N'Carpenter', N'Carpentry and furniture resource'),
    (N'Furniture Vendor', N'Furniture supply and fabrication'),
    (N'Glass & Aluminium', N'Glass, aluminium and partition resource'),
    (N'AC Technician', N'Air conditioning resource'),
    (N'Networking', N'Networking and IT infrastructure resource'),
    (N'CCTV & Security', N'CCTV and access control resource'),
    (N'Signage', N'Signage and branding resource'),
    (N'Other', N'Other specialist resource')
) v(ResourceTypeName, Description)
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.ResourceTypes r WHERE r.ResourceTypeName = v.ResourceTypeName
);
GO

INSERT INTO dbo.DocumentTypes (DocumentTypeName, Description)
SELECT v.DocumentTypeName, v.Description
FROM
(
    VALUES
    (N'Site Photo', N'Site photographs'),
    (N'Customer Drawing', N'Customer-provided drawings'),
    (N'Floor Plan', N'Floor plans'),
    (N'Architectural Drawing', N'Architectural drawings'),
    (N'Interior Design', N'Interior design files'),
    (N'3D Visualization', N'3D views and renders'),
    (N'Tile Layout', N'Tile and flooring layouts'),
    (N'Quotation', N'Quotation documents'),
    (N'Invoice', N'Invoice documents'),
    (N'Agreement', N'Agreement or contract'),
    (N'Completion Document', N'Project completion documents'),
    (N'Other', N'Other project documents')
) v(DocumentTypeName, Description)
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.DocumentTypes d WHERE d.DocumentTypeName = v.DocumentTypeName
);
GO

/* Service categories */
INSERT INTO dbo.ServiceCategories (CategoryName, Description, DisplayOrder)
SELECT v.CategoryName, v.Description, v.DisplayOrder
FROM
(
    VALUES
    (N'Architecture & Design', N'Architecture, planning, interior design and visualization', 1),
    (N'Tiles & Flooring', N'Tiles, flooring, tile supply and installation', 2),
    (N'Civil & Site Services', N'Civil modifications and site-related resources', 3),
    (N'Electrical & Plumbing', N'Electrical and plumbing planning and resources', 4),
    (N'Ceiling & Wall Finishing', N'Ceiling, wall preparation, painting and finishing', 5),
    (N'Interior & Furniture', N'Furniture, modular and interior fit-out services', 6),
    (N'Office Technology', N'Networking, CCTV, access control and office technology', 7),
    (N'Project Coordination', N'Project coordination and execution support', 8)
) v(CategoryName, Description, DisplayOrder)
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.ServiceCategories c WHERE c.CategoryName = v.CategoryName
);
GO

/* Services */
INSERT INTO dbo.Services
(
    ServiceCategoryId, ServiceName, ShortDescription, DisplayOrder,
    IsCustomerSelectable, RequiresSiteVisit, RequiresQuotation
)
SELECT
    c.ServiceCategoryId,
    v.ServiceName,
    v.ShortDescription,
    v.DisplayOrder,
    v.IsCustomerSelectable,
    v.RequiresSiteVisit,
    v.RequiresQuotation
FROM
(
    VALUES
    (N'Architecture & Design', N'Site Assessment & Measurement', N'Site study, measurement and requirement assessment', 1, 1, 1, 1),
    (N'Architecture & Design', N'Architectural Planning', N'Architectural planning and space planning', 2, 1, 1, 1),
    (N'Architecture & Design', N'Floor Plan Design', N'2D floor plans and layouts', 3, 1, 1, 1),
    (N'Architecture & Design', N'Interior Design', N'Interior space planning and design', 4, 1, 1, 1),
    (N'Architecture & Design', N'3D Visualization', N'3D interior and exterior visualization', 5, 1, 0, 1),
    (N'Architecture & Design', N'Elevation Design', N'Exterior elevation design and visualization', 6, 1, 1, 1),
    (N'Architecture & Design', N'Electrical Layout', N'Electrical and lighting planning', 7, 1, 1, 1),
    (N'Architecture & Design', N'Plumbing Layout', N'Plumbing planning and layout', 8, 1, 1, 1),
    (N'Architecture & Design', N'Tile Layout Design', N'Tile pattern, layout and quantity planning', 9, 1, 1, 1),
    (N'Architecture & Design', N'Furniture Layout', N'Furniture and workstation planning', 10, 1, 1, 1),

    (N'Tiles & Flooring', N'Tile Selection', N'Tile selection assistance and design coordination', 1, 1, 0, 1),
    (N'Tiles & Flooring', N'Tile Supply', N'Tile and surface material supply', 2, 1, 1, 1),
    (N'Tiles & Flooring', N'Tile Quantity Calculation', N'Quantity calculation based on drawings or site', 3, 1, 1, 1),
    (N'Tiles & Flooring', N'Tile Laying & Setting', N'Tile installation through resource partners', 4, 1, 1, 1),
    (N'Tiles & Flooring', N'Grouting & Finishing', N'Grouting and finishing through resources', 5, 1, 1, 1),
    (N'Tiles & Flooring', N'Flooring Installation', N'Flooring installation and finishing', 6, 1, 1, 1),

    (N'Civil & Site Services', N'Civil Work Planning', N'Planning and coordination of civil requirements', 1, 1, 1, 1),
    (N'Civil & Site Services', N'Partition Work', N'Partition and space division through resources', 2, 1, 1, 1),
    (N'Civil & Site Services', N'False Ceiling', N'False ceiling through resource partners', 3, 1, 1, 1),
    (N'Civil & Site Services', N'Wall Finishing', N'Wall preparation and finishing through resources', 4, 1, 1, 1),
    (N'Civil & Site Services', N'Painting', N'Painting through resource partners', 5, 1, 1, 1),
    (N'Civil & Site Services', N'Waterproofing', N'Waterproofing through specialist resources', 6, 1, 1, 1),
    (N'Civil & Site Services', N'Demolition & Dismantling', N'Demolition or dismantling through resources', 7, 1, 1, 1),
    (N'Civil & Site Services', N'Minor Civil Modifications', N'Minor civil modifications through resources', 8, 1, 1, 1),

    (N'Electrical & Plumbing', N'Electrical Planning', N'Electrical planning and coordination', 1, 1, 1, 1),
    (N'Electrical & Plumbing', N'Electrical Works', N'Electrical installation through resources', 2, 1, 1, 1),
    (N'Electrical & Plumbing', N'Lighting Installation', N'Lighting installation through resources', 3, 1, 1, 1),
    (N'Electrical & Plumbing', N'Plumbing Planning', N'Plumbing planning and coordination', 4, 1, 1, 1),
    (N'Electrical & Plumbing', N'Plumbing Works', N'Plumbing installation through resources', 5, 1, 1, 1),

    (N'Ceiling & Wall Finishing', N'Gypsum Ceiling', N'Gypsum false ceiling through resources', 1, 1, 1, 1),
    (N'Ceiling & Wall Finishing', N'Grid Ceiling', N'Grid ceiling through resources', 2, 1, 1, 1),
    (N'Ceiling & Wall Finishing', N'Decorative Ceiling', N'Decorative ceiling through resources', 3, 1, 1, 1),
    (N'Ceiling & Wall Finishing', N'Wall Preparation', N'Putty, plaster and surface preparation', 4, 1, 1, 1),
    (N'Ceiling & Wall Finishing', N'Wall Painting', N'Painting and wall finishing', 5, 1, 1, 1),
    (N'Ceiling & Wall Finishing', N'Decorative Wall Finish', N'Texture, panels and decorative finishes', 6, 1, 1, 1),

    (N'Interior & Furniture', N'Office Workstations', N'Office workstation design and resource arrangement', 1, 1, 1, 1),
    (N'Interior & Furniture', N'Office Furniture', N'Office furniture through resource partners', 2, 1, 1, 1),
    (N'Interior & Furniture', N'Reception Counter', N'Reception and front-desk furniture', 3, 1, 1, 1),
    (N'Interior & Furniture', N'Modular Kitchen', N'Modular kitchen design and resource arrangement', 4, 1, 1, 1),
    (N'Interior & Furniture', N'Wardrobes', N'Wardrobe design and fabrication through resources', 5, 1, 1, 1),
    (N'Interior & Furniture', N'Custom Furniture', N'Custom furniture through resource partners', 6, 1, 1, 1),
    (N'Interior & Furniture', N'Glass & Aluminium Work', N'Glass partitions and aluminium work', 7, 1, 1, 1),
    (N'Interior & Furniture', N'Blinds & Curtains', N'Blinds and curtain solutions', 8, 1, 0, 1),
    (N'Interior & Furniture', N'Signage', N'Indoor and outdoor signage through resources', 9, 1, 1, 1),

    (N'Office Technology', N'Networking', N'LAN, Wi-Fi and network infrastructure', 1, 1, 1, 1),
    (N'Office Technology', N'CCTV & Security', N'CCTV, access control and security systems', 2, 1, 1, 1),
    (N'Office Technology', N'Access Control', N'Access control and attendance systems', 3, 1, 1, 1),
    (N'Office Technology', N'Conference Room Setup', N'Display, conferencing and meeting room technology', 4, 1, 1, 1),

    (N'Project Coordination', N'Project Coordination', N'Coordination of selected services and resources', 1, 1, 1, 1),
    (N'Project Coordination', N'Material Coordination', N'Material planning and delivery coordination', 2, 1, 1, 1),
    (N'Project Coordination', N'Resource Coordination', N'Resource identification, assignment and coordination', 3, 1, 1, 1),
    (N'Project Coordination', N'Quality Coordination', N'Inspection and quality coordination', 4, 1, 1, 1)
) v(CategoryName, ServiceName, ShortDescription, DisplayOrder, IsCustomerSelectable, RequiresSiteVisit, RequiresQuotation)
JOIN dbo.ServiceCategories c ON c.CategoryName = v.CategoryName
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Services s
    WHERE s.ServiceCategoryId = c.ServiceCategoryId
      AND s.ServiceName = v.ServiceName
);
GO

/* Service options */
INSERT INTO dbo.ServiceOptions
(
    ServiceId, OptionCode, OptionName, Description, DisplayOrder
)
SELECT
    s.ServiceId,
    v.OptionCode,
    v.OptionName,
    v.Description,
    v.DisplayOrder
FROM
(
    VALUES
    (N'Architecture & Design', N'Site Assessment & Measurement', N'OUR_SERVICE', N'Our team provides the service', 1),
    (N'Architecture & Design', N'Architectural Planning', N'OUR_SERVICE', N'Our team provides the service', 1),
    (N'Architecture & Design', N'Floor Plan Design', N'OUR_SERVICE', N'Our team provides the service', 1),
    (N'Architecture & Design', N'Interior Design', N'OUR_SERVICE', N'Our team provides the service', 1),
    (N'Architecture & Design', N'3D Visualization', N'OUR_SERVICE', N'Our team provides the service', 1),
    (N'Architecture & Design', N'Elevation Design', N'OUR_SERVICE', N'Our team provides the service', 1),
    (N'Architecture & Design', N'Electrical Layout', N'OUR_SERVICE', N'Our team provides the service', 1),
    (N'Architecture & Design', N'Plumbing Layout', N'OUR_SERVICE', N'Our team provides the service', 1),
    (N'Architecture & Design', N'Tile Layout Design', N'OUR_SERVICE', N'Our team provides the service', 1),
    (N'Architecture & Design', N'Furniture Layout', N'OUR_SERVICE', N'Our team provides the service', 1),

    (N'Tiles & Flooring', N'Tile Selection', N'OUR_SERVICE', N'Our team provides selection assistance', 1),
    (N'Tiles & Flooring', N'Tile Supply', N'SUPPLY', N'Material supplied through AVA', 1),
    (N'Tiles & Flooring', N'Tile Quantity Calculation', N'OUR_SERVICE', N'Our team calculates quantities', 1),
    (N'Tiles & Flooring', N'Tile Laying & Setting', N'RESOURCE', N'Installation resource arranged', 1),
    (N'Tiles & Flooring', N'Grouting & Finishing', N'RESOURCE', N'Finishing resource arranged', 1),
    (N'Tiles & Flooring', N'Flooring Installation', N'RESOURCE', N'Installation resource arranged', 1),

    (N'Civil & Site Services', N'Civil Work Planning', N'OUR_SERVICE', N'Planning and coordination by our team', 1),
    (N'Civil & Site Services', N'Partition Work', N'RESOURCE', N'Resource partner arranged', 1),
    (N'Civil & Site Services', N'False Ceiling', N'RESOURCE', N'Resource partner arranged', 1),
    (N'Civil & Site Services', N'Wall Finishing', N'RESOURCE', N'Resource partner arranged', 1),
    (N'Civil & Site Services', N'Painting', N'RESOURCE', N'Resource partner arranged', 1),
    (N'Civil & Site Services', N'Waterproofing', N'RESOURCE', N'Specialist resource arranged', 1),
    (N'Civil & Site Services', N'Demolition & Dismantling', N'RESOURCE', N'Resource partner arranged', 1),
    (N'Civil & Site Services', N'Minor Civil Modifications', N'RESOURCE', N'Resource partner arranged', 1),

    (N'Electrical & Plumbing', N'Electrical Planning', N'OUR_SERVICE', N'Planning and coordination by our team', 1),
    (N'Electrical & Plumbing', N'Electrical Works', N'RESOURCE', N'Electrical resource arranged', 1),
    (N'Electrical & Plumbing', N'Lighting Installation', N'RESOURCE', N'Installation resource arranged', 1),
    (N'Electrical & Plumbing', N'Plumbing Planning', N'OUR_SERVICE', N'Planning and coordination by our team', 1),
    (N'Electrical & Plumbing', N'Plumbing Works', N'RESOURCE', N'Plumbing resource arranged', 1),

    (N'Ceiling & Wall Finishing', N'Gypsum Ceiling', N'RESOURCE', N'Resource partner arranged', 1),
    (N'Ceiling & Wall Finishing', N'Grid Ceiling', N'RESOURCE', N'Resource partner arranged', 1),
    (N'Ceiling & Wall Finishing', N'Decorative Ceiling', N'RESOURCE', N'Resource partner arranged', 1),
    (N'Ceiling & Wall Finishing', N'Wall Preparation', N'RESOURCE', N'Resource partner arranged', 1),
    (N'Ceiling & Wall Finishing', N'Wall Painting', N'RESOURCE', N'Resource partner arranged', 1),
    (N'Ceiling & Wall Finishing', N'Decorative Wall Finish', N'RESOURCE', N'Resource partner arranged', 1),

    (N'Interior & Furniture', N'Office Workstations', N'RESOURCE', N'Resource/fabrication partner arranged', 1),
    (N'Interior & Furniture', N'Office Furniture', N'RESOURCE', N'Resource/fabrication partner arranged', 1),
    (N'Interior & Furniture', N'Reception Counter', N'RESOURCE', N'Resource/fabrication partner arranged', 1),
    (N'Interior & Furniture', N'Modular Kitchen', N'RESOURCE', N'Resource/fabrication partner arranged', 1),
    (N'Interior & Furniture', N'Wardrobes', N'RESOURCE', N'Resource/fabrication partner arranged', 1),
    (N'Interior & Furniture', N'Custom Furniture', N'RESOURCE', N'Resource/fabrication partner arranged', 1),
    (N'Interior & Furniture', N'Glass & Aluminium Work', N'RESOURCE', N'Resource partner arranged', 1),
    (N'Interior & Furniture', N'Blinds & Curtains', N'RESOURCE', N'Resource partner arranged', 1),
    (N'Interior & Furniture', N'Signage', N'RESOURCE', N'Resource partner arranged', 1),

    (N'Office Technology', N'Networking', N'RESOURCE', N'Networking resource arranged', 1),
    (N'Office Technology', N'CCTV & Security', N'RESOURCE', N'Security resource arranged', 1),
    (N'Office Technology', N'Access Control', N'RESOURCE', N'Security resource arranged', 1),
    (N'Office Technology', N'Conference Room Setup', N'RESOURCE', N'Technology resource arranged', 1),

    (N'Project Coordination', N'Project Coordination', N'COORDINATION', N'Project coordination by AVA', 1),
    (N'Project Coordination', N'Material Coordination', N'COORDINATION', N'Material coordination by AVA', 1),
    (N'Project Coordination', N'Resource Coordination', N'COORDINATION', N'Resource coordination by AVA', 1),
    (N'Project Coordination', N'Quality Coordination', N'COORDINATION', N'Quality coordination by AVA', 1)
) v(CategoryName, ServiceName, OptionCode, Description, DisplayOrder)
JOIN dbo.ServiceCategories c ON c.CategoryName = v.CategoryName
JOIN dbo.Services s ON s.ServiceCategoryId = c.ServiceCategoryId
                 AND s.ServiceName = v.ServiceName
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.ServiceOptions so
    WHERE so.ServiceId = s.ServiceId
      AND so.OptionCode = v.OptionCode
);
GO

/* Add COORDINATION as an available option to non-coordination services */
INSERT INTO dbo.ServiceOptions
(
    ServiceId, OptionCode, OptionName, Description, DisplayOrder
)
SELECT
    s.ServiceId,
    N'COORDINATION',
    N'Project Coordination',
    N'AVA coordinates the selected service within the project',
    10
FROM dbo.Services s
WHERE s.IsActive = 1
  AND s.ServiceName <> N'Project Coordination'
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.ServiceOptions so
      WHERE so.ServiceId = s.ServiceId
        AND so.OptionCode = N'COORDINATION'
  );
GO

/* ============================================================
   17. OPTIONAL CUSTOMER PORTAL LINK
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Customers')
      AND name = N'PortalUserId'
)
BEGIN
    ALTER TABLE dbo.Customers ADD PortalUserId BIGINT NULL;
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Customers_PortalUser'
)
BEGIN
    ALTER TABLE dbo.Customers
    ADD CONSTRAINT FK_Customers_PortalUser
        FOREIGN KEY (PortalUserId) REFERENCES dbo.Users(UserId);
END
GO

/* ============================================================
   18. VERIFICATION
   ============================================================ */

SELECT
    DB_NAME() AS DatabaseName,
    (SELECT COUNT(*) FROM dbo.Roles) AS Roles,
    (SELECT COUNT(*) FROM dbo.ServiceCategories) AS ServiceCategories,
    (SELECT COUNT(*) FROM dbo.Services) AS Services,
    (SELECT COUNT(*) FROM dbo.ServiceOptions) AS ServiceOptions,
    (SELECT COUNT(*) FROM dbo.ProjectTypes) AS ProjectTypes,
    (SELECT COUNT(*) FROM dbo.ResourceTypes) AS ResourceTypes,
    (SELECT COUNT(*) FROM dbo.DocumentTypes) AS DocumentTypes;
GO

SELECT
    c.CategoryName,
    s.ServiceName,
    so.OptionCode,
    so.OptionName
FROM dbo.ServiceCategories c
JOIN dbo.Services s ON s.ServiceCategoryId = c.ServiceCategoryId
LEFT JOIN dbo.ServiceOptions so ON so.ServiceId = s.ServiceId
ORDER BY c.DisplayOrder, s.DisplayOrder, so.DisplayOrder;
GO

PRINT N'AVADesignServices database setup completed successfully.';
GO

'@

$DatabaseSqlPath = Join-Path $DatabaseDir "AVADesignServices_Database.sql"
Write-TextFile $DatabaseSqlPath $DatabaseSql

Write-Step "2. Creating / updating SQL Server database"
Invoke-SqlScript $DatabaseSqlPath

Write-Step "3. Creating ASP.NET Core Web API project"

if (-not (Test-Path (Join-Path $Backend "backend-api.csproj"))) {
    Push-Location $Root
    try {
        Invoke-DotNet "new webapi -n backend-api --framework net9.0 --use-controllers"
    }
    finally {
        Pop-Location
    }
}

Write-Step "4. Installing required packages"

Push-Location $Backend
try {
    Invoke-DotNet "add package Microsoft.EntityFrameworkCore.Design --version 9.0.15"
    Invoke-DotNet "add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.15"
    Invoke-DotNet "add package Microsoft.EntityFrameworkCore.Tools --version 9.0.15"
    Invoke-DotNet "add package Swashbuckle.AspNetCore --version 9.0.6"

    if (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue)) {
        & dotnet tool install --global dotnet-ef --version 9.0.15
        if ($LASTEXITCODE -ne 0) {
            throw "Unable to install dotnet-ef 9.0.15."
        }
    }

    Write-Step "5. Scaffolding EF Core models"

    $connection = "Server=localhost\SQLEXPRESS;Database=AVADesignServices;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"

    & dotnet ef dbcontext scaffold $connection Microsoft.EntityFrameworkCore.SqlServer `
        --output-dir Models `
        --context-dir Data `
        --context AVADesignServicesDbContext `
        --no-onconfiguring `
        --force

    if ($LASTEXITCODE -ne 0) {
        throw "EF Core scaffolding failed."
    }
}
finally {
    Pop-Location
}

Write-Step "6. Writing application configuration"

$appsettings = @'
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=AVADesignServices;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
'@

Write-TextFile (Join-Path $Backend "appsettings.json") $appsettings

$program = @'
using backend_api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AVADesignServicesDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
'@

Write-TextFile (Join-Path $Backend "Program.cs") $program

Write-Step "7. Writing DTOs"

$createLead = @'
namespace backend_api.DTOs;

public class CreateLeadRequest
{
    public string? LeadSource { get; set; }
    public string CustomerName { get; set; } = null!;
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public long? ProjectTypeId { get; set; }
    public string? Location { get; set; }
    public decimal? ApproximateArea { get; set; }
    public string? AreaUnit { get; set; }
    public string? Requirement { get; set; }
    public List<CreateLeadServiceRequest> Services { get; set; } = new();
}

public class CreateLeadServiceRequest
{
    public long ServiceId { get; set; }
    public long? ServiceOptionId { get; set; }
    public string? Requirement { get; set; }
}
'@

$updateLeadStatus = @'
namespace backend_api.DTOs;

public class UpdateLeadStatusRequest
{
    public string StatusCode { get; set; } = null!;
}
'@

$assignLead = @'
namespace backend_api.DTOs;

public class AssignLeadRequest
{
    public long AssignedToUserId { get; set; }
}
'@

Write-TextFile (Join-Path $Backend "DTOs\CreateLeadRequest.cs") $createLead
Write-TextFile (Join-Path $Backend "DTOs\UpdateLeadStatusRequest.cs") $updateLeadStatus
Write-TextFile (Join-Path $Backend "DTOs\AssignLeadRequest.cs") $assignLead

Write-Step "8. Writing service catalogue controllers"

$servicesController = @'
using backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public ServicesController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetServices()
    {
        var services = await _context.Services
            .AsNoTracking()
            .Include(s => s.ServiceCategory)
            .Include(s => s.ServiceOptions)
            .Where(s => s.IsActive)
            .OrderBy(s => s.ServiceCategory.DisplayOrder)
            .ThenBy(s => s.DisplayOrder)
            .Select(s => new
            {
                serviceId = s.ServiceId,
                serviceName = s.ServiceName,
                shortDescription = s.ShortDescription,
                category = new
                {
                    categoryId = s.ServiceCategory.ServiceCategoryId,
                    categoryName = s.ServiceCategory.CategoryName
                },
                options = s.ServiceOptions
                    .Where(o => o.IsActive && o.IsAvailableToCustomer)
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => new
                    {
                        optionId = o.ServiceOptionId,
                        code = o.OptionCode,
                        name = o.OptionName,
                        description = o.Description
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(services);
    }
}
'@

$projectTypesController = @'
using backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectTypesController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public ProjectTypesController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetProjectTypes()
    {
        var projectTypes = await _context.ProjectTypes
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new
            {
                projectTypeId = p.ProjectTypeId,
                projectTypeName = p.ProjectTypeName,
                description = p.Description
            })
            .ToListAsync();

        return Ok(projectTypes);
    }
}
'@

$serviceCategoriesController = @'
using backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceCategoriesController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public ServiceCategoriesController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetServiceCategories()
    {
        var categories = await _context.ServiceCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new
            {
                serviceCategoryId = c.ServiceCategoryId,
                categoryName = c.CategoryName,
                description = c.Description,
                displayOrder = c.DisplayOrder,
                services = c.Services
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => new
                    {
                        serviceId = s.ServiceId,
                        serviceName = s.ServiceName,
                        shortDescription = s.ShortDescription,
                        displayOrder = s.DisplayOrder,
                        options = s.ServiceOptions
                            .Where(o => o.IsActive && o.IsAvailableToCustomer)
                            .OrderBy(o => o.DisplayOrder)
                            .Select(o => new
                            {
                                optionId = o.ServiceOptionId,
                                code = o.OptionCode,
                                name = o.OptionName,
                                description = o.Description
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(categories);
    }
}
'@

Write-TextFile (Join-Path $Backend "Controllers\ServicesController.cs") $servicesController
Write-TextFile (Join-Path $Backend "Controllers\ProjectTypesController.cs") $projectTypesController
Write-TextFile (Join-Path $Backend "Controllers\ServiceCategoriesController.cs") $serviceCategoriesController

Write-Step "9. Writing Lead Status controller"

$leadStatusesController = @'
using backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeadStatusesController : ControllerBase
{
    private readonly AVADesignServicesDbContext _context;

    public LeadStatusesController(AVADesignServicesDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetLeadStatuses()
    {
        var statuses = await _context.LeadStatuses
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new
            {
                leadStatusId = s.LeadStatusId,
                statusCode = s.StatusCode,
                statusName = s.StatusName,
                description = s.Description,
                displayOrder = s.DisplayOrder
            })
            .ToListAsync();

        return Ok(statuses);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetLeadStatus(long id)
    {
        var status = await _context.LeadStatuses
            .AsNoTracking()
            .Where(s => s.LeadStatusId == id && s.IsActive)
            .Select(s => new
            {
                leadStatusId = s.LeadStatusId,
                statusCode = s.StatusCode,
                statusName = s.StatusName,
                description = s.Description,
                displayOrder = s.DisplayOrder
            })
            .FirstOrDefaultAsync();

        if (status == null)
            return NotFound();

        return Ok(status);
    }
}
'@

Write-TextFile (Join-Path $Backend "Controllers\LeadStatusesController.cs") $leadStatusesController

Write-Step "10. Building backend"

Push-Location $Backend
try {
    Invoke-DotNet "build"
}
finally {
    Pop-Location
}

Write-Step "SETUP COMPLETE"

Write-Host ""
Write-Host "Root:    $Root" -ForegroundColor Green
Write-Host "Backend: $Backend" -ForegroundColor Green
Write-Host "Database: AVADesignServices on localhost\SQLEXPRESS" -ForegroundColor Green
Write-Host ""
Write-Host "Start the API with:" -ForegroundColor Yellow
Write-Host "  cd `"$Backend`"" -ForegroundColor White
Write-Host "  dotnet run" -ForegroundColor White
Write-Host ""
