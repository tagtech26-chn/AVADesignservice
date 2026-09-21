$ErrorActionPreference = "Stop"

$controllersPath = Join-Path $PSScriptRoot "Controllers"
$baseControllerPath = Join-Path $controllersPath "CrudController.cs"

# Make sure Controllers folder exists
if (-not (Test-Path $controllersPath)) {
    New-Item -ItemType Directory -Path $controllersPath | Out-Null
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host " AVADesignServices - Controller Generator" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# ---------------------------------------------------------
# Generic CRUD Base Controller
# ---------------------------------------------------------

$baseController = @'
using backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace backend_api.Controllers;

[ApiController]
public abstract class CrudController<TEntity> : ControllerBase
    where TEntity : class
{
    protected readonly AVADesignServicesDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    protected CrudController(AVADesignServicesDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    // GET: api/[controller]
    [HttpGet]
    public virtual async Task<IActionResult> GetAll()
    {
        IQueryable<TEntity> query = _dbSet.AsNoTracking();

        // Automatically filter IsActive = true when the entity has IsActive
        var isActiveProperty = typeof(TEntity).GetProperty("IsActive");

        if (isActiveProperty != null &&
            isActiveProperty.PropertyType == typeof(bool))
        {
            query = query.Where(e => EF.Property<bool>(e, "IsActive"));
        }

        var result = await query.ToListAsync();

        return Ok(result);
    }

    // GET: api/[controller]/{id}
    [HttpGet("{id:long}")]
    public virtual async Task<IActionResult> GetById(long id)
    {
        var entityType = _context.Model.FindEntityType(typeof(TEntity));

        if (entityType == null)
            return NotFound();

        var primaryKey = entityType.FindPrimaryKey();

        if (primaryKey == null || primaryKey.Properties.Count != 1)
            return BadRequest("Entity does not have a single primary key.");

        var keyProperty = primaryKey.Properties[0];

        var keyType = Nullable.GetUnderlyingType(keyProperty.ClrType)
                      ?? keyProperty.ClrType;

        object convertedId;

        try
        {
            convertedId = Convert.ChangeType(id, keyType);
        }
        catch
        {
            return BadRequest("Invalid ID.");
        }

        var entity = await _dbSet.FindAsync(new[] { convertedId });

        if (entity == null)
            return NotFound();

        return Ok(entity);
    }

    // POST: api/[controller]
    [HttpPost]
    public virtual async Task<IActionResult> Create([FromBody] TEntity entity)
    {
        if (entity == null)
            return BadRequest();

        _dbSet.Add(entity);

        await _context.SaveChangesAsync();

        return Ok(entity);
    }

    // PUT: api/[controller]/{id}
    [HttpPut("{id:long}")]
    public virtual async Task<IActionResult> Update(
        long id,
        [FromBody] TEntity entity)
    {
        if (entity == null)
            return BadRequest();

        var entityType = _context.Model.FindEntityType(typeof(TEntity));

        if (entityType == null)
            return NotFound();

        var primaryKey = entityType.FindPrimaryKey();

        if (primaryKey == null || primaryKey.Properties.Count != 1)
            return BadRequest("Entity does not have a single primary key.");

        var keyProperty = primaryKey.Properties[0];

        var keyType = Nullable.GetUnderlyingType(keyProperty.ClrType)
                      ?? keyProperty.ClrType;

        object convertedId;

        try
        {
            convertedId = Convert.ChangeType(id, keyType);
        }
        catch
        {
            return BadRequest("Invalid ID.");
        }

        var existing = await _dbSet.FindAsync(new[] { convertedId });

        if (existing == null)
            return NotFound();

        // Preserve primary key
        var keyClrProperty = typeof(TEntity).GetProperty(keyProperty.Name);

        if (keyClrProperty != null)
        {
            keyClrProperty.SetValue(entity, keyClrProperty.GetValue(existing));
        }

        _context.Entry(existing).CurrentValues.SetValues(entity);

        await _context.SaveChangesAsync();

        return Ok(existing);
    }

    // DELETE: api/[controller]/{id}
    [HttpDelete("{id:long}")]
    public virtual async Task<IActionResult> Delete(long id)
    {
        var entityType = _context.Model.FindEntityType(typeof(TEntity));

        if (entityType == null)
            return NotFound();

        var primaryKey = entityType.FindPrimaryKey();

        if (primaryKey == null || primaryKey.Properties.Count != 1)
            return BadRequest("Entity does not have a single primary key.");

        var keyProperty = primaryKey.Properties[0];

        var keyType = Nullable.GetUnderlyingType(keyProperty.ClrType)
                      ?? keyProperty.ClrType;

        object convertedId;

        try
        {
            convertedId = Convert.ChangeType(id, keyType);
        }
        catch
        {
            return BadRequest("Invalid ID.");
        }

        var entity = await _dbSet.FindAsync(new[] { convertedId });

        if (entity == null)
            return NotFound();

        // Prefer soft delete when IsActive exists
        var isActiveProperty = typeof(TEntity).GetProperty("IsActive");

        if (isActiveProperty != null &&
            isActiveProperty.PropertyType == typeof(bool))
        {
            isActiveProperty.SetValue(entity, false);
        }
        else
        {
            _dbSet.Remove(entity);
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
'@

Set-Content `
    -Path $baseControllerPath `
    -Value $baseController `
    -Encoding UTF8

Write-Host "Created CrudController.cs" -ForegroundColor Green

# ---------------------------------------------------------
# Entity -> Controller mapping
# ---------------------------------------------------------

$entities = @(
    "ActivityLog",
    "CustomerAddress",
    "CustomerApproval",
    "Customer",
    "Design",
    "Document",
    "DocumentType",
    "Invoice",
    "InvoiceItem",
    "Lead",
    "LeadService",
    "Message",
    "Notification",
    "Payment",
    "ProjectProgress",
    "ProjectResource",
    "ProjectService",
    "ProjectStage",
    "ProjectTask",
    "Project",
    "QuotationItem",
    "Quotation",
    "Resource",
    "ResourceService",
    "ResourceType",
    "Role",
    "ServiceCategory",
    "ServiceOption",
    "SiteVisit",
    "UserRole",
    "User"
)

# ---------------------------------------------------------
# Create concrete controllers
# ---------------------------------------------------------

foreach ($entity in $entities) {

    $controllerName = "${entity}sController"

    # Fix plural names where necessary
    switch ($entity) {
        "ActivityLog"      { $controllerName = "ActivityLogsController" }
        "CustomerAddress"  { $controllerName = "CustomerAddressesController" }
        "CustomerApproval" { $controllerName = "CustomerApprovalsController" }
        "Document"         { $controllerName = "DocumentsController" }
        "DocumentType"     { $controllerName = "DocumentTypesController" }
        "InvoiceItem"      { $controllerName = "InvoiceItemsController" }
        "LeadService"      { $controllerName = "LeadServicesController" }
        "ProjectProgress"  { $controllerName = "ProjectProgressController" }
        "ProjectResource"  { $controllerName = "ProjectResourcesController" }
        "ProjectService"   { $controllerName = "ProjectServicesController" }
        "ProjectStage"     { $controllerName = "ProjectStagesController" }
        "ProjectTask"      { $controllerName = "ProjectTasksController" }
        "QuotationItem"    { $controllerName = "QuotationItemsController" }
        "ResourceService"  { $controllerName = "ResourceServicesController" }
        "ResourceType"     { $controllerName = "ResourceTypesController" }
        "ServiceCategory"  { $controllerName = "ServiceCategoriesController" }
        "ServiceOption"    { $controllerName = "ServiceOptionsController" }
        "SiteVisit"        { $controllerName = "SiteVisitsController" }
        "UserRole"         { $controllerName = "UserRolesController" }
        "Customer"         { $controllerName = "CustomersController" }
        "Design"           { $controllerName = "DesignsController" }
        "Invoice"          { $controllerName = "InvoicesController" }
        "Lead"             { $controllerName = "LeadsController" }
        "Message"          { $controllerName = "MessagesController" }
        "Notification"     { $controllerName = "NotificationsController" }
        "Payment"          { $controllerName = "PaymentsController" }
        "Project"          { $controllerName = "ProjectsController" }
        "Quotation"        { $controllerName = "QuotationsController" }
        "Resource"         { $controllerName = "ResourcesController" }
        "Role"             { $controllerName = "RolesController" }
        "User"             { $controllerName = "UsersController" }
    }

    $filePath = Join-Path $controllersPath "$controllerName.cs"

    # Do not overwrite controllers already created manually
    if (Test-Path $filePath) {
        Write-Host "Skipping existing: $controllerName.cs" -ForegroundColor Yellow
        continue
    }

    $controllerContent = @"
using backend_api.Data;
using backend_api.Models;

namespace backend_api.Controllers;

[Route("api/[controller]")]
public class $controllerName : CrudController<$entity>
{
    public $controllerName(AVADesignServicesDbContext context)
        : base(context)
    {
    }
}
"@

    Set-Content `
        -Path $filePath `
        -Value $controllerContent `
        -Encoding UTF8

    Write-Host "Created $controllerName.cs" -ForegroundColor Green
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host " Controller generation completed." -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Controllers folder:" -ForegroundColor Cyan
Write-Host $controllersPath

Write-Host ""
Write-Host "Next step: run dotnet build" -ForegroundColor Yellow
Write-Host ""