using MoneyMap.Core.Models;
using MoneyMap.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MoneyMap.Data.Repositories;

public class ImportRecordRepository : IImportRecordRepository
{
    private readonly MoneyMapDbContext _context;

    public ImportRecordRepository(MoneyMapDbContext context)
    {
        _context = context;
    }

    public async Task<ImportRecord?> GetByIdAsync(long id)
    {
        return await _context.ImportRecords
            .Include(i => i.Transactions)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<ImportRecord?> GetByFilePathAsync(string filePath)
    {
        return await _context.ImportRecords
            .FirstOrDefaultAsync(i => i.FilePath == filePath);
    }

    public async Task<IEnumerable<ImportRecord>> GetRecentAsync(int limit = 20)
    {
        return await _context.ImportRecords
            .OrderByDescending(i => i.ImportTime)
            .Take(limit)
            .ToListAsync();
    }

    public async Task AddAsync(ImportRecord record)
    {
        await _context.ImportRecords.AddAsync(record);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ImportRecord record)
    {
        _context.ImportRecords.Update(record);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(ImportRecord record)
    {
        _context.ImportRecords.Remove(record);
        await _context.SaveChangesAsync();
    }
}