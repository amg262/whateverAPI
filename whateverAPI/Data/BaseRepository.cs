using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace whateverAPI.Data;

public interface IEntity<TId> where TId : struct
{
    TId Id { get; set; }
}

public interface IBaseRepository<TEntity, in TId> where TEntity : class where TId : struct
{
    // Basic CRUD
    Task<TEntity> CreateAsync(TEntity entity);
    Task<TEntity?> GetByIdAsync(TId id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<TEntity> UpdateAsync(TEntity entity);
    Task DeleteAsync(TId id);

    // Common Query Operations
    Task<bool> ExistsByIdAsync(TId id);
    Task<int> CountAsync();
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
    Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate);

    // Querying and Filtering
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);

    // Batch Operations
    Task CreateRangeAsync(IEnumerable<TEntity> entities);
    Task DeleteRangeAsync(IEnumerable<TId> ids);

    // Pagination and Sorting
    IQueryable<TEntity> GetQueryable();

    IOrderedQueryable<TEntity> ApplySorting<TKey>(
        IQueryable<TEntity> query,
        Expression<Func<TEntity, TKey>> keySelector,
        bool descending = false);

    IQueryable<TEntity> ApplyPaging(
        IQueryable<TEntity> query,
        int pageNumber,
        int pageSize);
}

public abstract class BaseRepository<TEntity, TId> : IBaseRepository<TEntity, TId>
    where TEntity : class, IEntity<TId> // Adding IEntity constraint to make the Id property available
    where TId : struct
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<TEntity> DbSet;
    protected readonly ILogger<BaseRepository<TEntity, TId>> Logger;

    protected BaseRepository(
        AppDbContext context,
        ILogger<BaseRepository<TEntity, TId>> logger)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
        Logger = logger;
    }

    // Basic CRUD Operations
    public virtual async Task<TEntity> CreateAsync(TEntity entity)
    {
        await DbSet.AddAsync(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<TEntity?> GetByIdAsync(TId id) => await DbSet.FindAsync(id);
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync() => await DbSet.ToListAsync();

    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task DeleteAsync(TId id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
        {
            throw new KeyNotFoundException($"Entity with ID {id} not found");
        }

        DbSet.Remove(entity);
        await Context.SaveChangesAsync();
    }

    // Common Query Operations - New implementations
    public virtual async Task<bool> ExistsByIdAsync(TId id) => await DbSet.FindAsync(id) != null;

    public virtual async Task<int> CountAsync() => await DbSet.CountAsync();


    public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate) => await DbSet.AnyAsync(predicate);

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate) => await DbSet.CountAsync(predicate);


    // Querying and Filtering - New implementations
    public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate) =>
        await DbSet.Where(predicate).ToListAsync();


    public virtual async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate) =>
        await DbSet.FirstOrDefaultAsync(predicate);


    // Batch Operations - New implementations
    public virtual async Task CreateRangeAsync(IEnumerable<TEntity> entities)
    {
        await DbSet.AddRangeAsync(entities);
        await Context.SaveChangesAsync();
    }

    public virtual async Task DeleteRangeAsync(IEnumerable<TId> ids)
    {
        var entities = await DbSet
            .Where(e => ids.Contains(e.Id))
            .ToListAsync();

        if (entities.Count == 0)
        {
            return;
        }

        DbSet.RemoveRange(entities);
        await Context.SaveChangesAsync();
    }

    // Pagination and Sorting - Fixed to match interface exactly
    public virtual IQueryable<TEntity> GetQueryable() => DbSet.AsQueryable();


    public virtual IOrderedQueryable<TEntity> ApplySorting<TKey>(
        IQueryable<TEntity> query,
        Expression<Func<TEntity, TKey>> keySelector,
        bool descending = false)
    {
        return descending
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }

    public virtual IQueryable<TEntity> ApplyPaging(
        IQueryable<TEntity> query,
        int pageNumber,
        int pageSize)
    {
        return query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }
}