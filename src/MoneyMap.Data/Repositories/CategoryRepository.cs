using MoneyMap.Core.Enums;
using MoneyMap.Core.Models;
using MoneyMap.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MoneyMap.Data.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly MoneyMapDbContext _context;

    public CategoryRepository(MoneyMapDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _context.Categories
            .Include(c => c.Parent)
            .Include(c => c.Children)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(long id)
    {
        return await _context.Categories
            .Include(c => c.Parent)
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Category?> GetByNameAsync(string name)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task AddAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Category category)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Category>> GetByTypeAsync(TransactionType type)
    {
        return await _context.Categories
            .Where(c => c.Type == type)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Category>> GetRootCategoriesAsync()
    {
        return await _context.Categories
            .Where(c => c.ParentId == null)
            .Include(c => c.Children)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }
}