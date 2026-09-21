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
