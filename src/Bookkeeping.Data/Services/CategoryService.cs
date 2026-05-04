using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bookkeeping.Core.Enums;
using Bookkeeping.Core.Models;
using Bookkeeping.Core.Services;
using Bookkeeping.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Bookkeeping.Data.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly BookkeepingDbContext _context;

    public CategoryService(ICategoryRepository categoryRepository, BookkeepingDbContext context)
    {
        _categoryRepository = categoryRepository;
        _context = context;
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _categoryRepository.GetAllAsync();
    }

    public async Task<Category?> GetByIdAsync(long id)
    {
        return await _categoryRepository.GetByIdAsync(id);
    }

    public async Task<Category> CreateAsync(Category category)
    {
        ValidateCategory(category);

        // Check for duplicate name within same type (case-insensitive)
        var existingCategories = await _categoryRepository.GetByTypeAsync(category.Type);
        if (existingCategories.Any(c => c.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Category with name '{category.Name}' already exists for type {category.Type}");
        }

        // Auto-increment SortOrder to be max + 1 for this type
        var maxSortOrder = existingCategories.Any() ? existingCategories.Max(c => c.SortOrder) : 0;
        category.SortOrder = maxSortOrder + 1;

        await _categoryRepository.AddAsync(category);
        return category;
    }

    public async Task<Category> UpdateAsync(Category category)
    {
        ValidateCategory(category);

        // Check for duplicate name excluding self (case-insensitive)
        var existingCategories = await _categoryRepository.GetByTypeAsync(category.Type);
        if (existingCategories.Any(c => c.Id != category.Id && c.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Category with name '{category.Name}' already exists for type {category.Type}");
        }

        await _categoryRepository.UpdateAsync(category);
        return category;
    }

    public async Task DeleteAsync(long id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            throw new InvalidOperationException($"Category with id {id} not found");
        }

        // Check if this is the last category of its type
        var categoriesOfType = await _categoryRepository.GetByTypeAsync(category.Type);
        if (categoriesOfType.Count() <= 1)
        {
            throw new InvalidOperationException($"Cannot delete the last category of type {category.Type}");
        }

        await _categoryRepository.DeleteAsync(id);
    }

    public async Task<Category?> AutoCategorizeAsync(string description, string? counterparty, DataSource source)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var allCategories = await _categoryRepository.GetAllAsync();
        var categoriesList = allCategories.ToList();

        // Try exact name match first (case-insensitive)
        var exactMatch = categoriesList.FirstOrDefault(c =>
            description.Contains(c.Name, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
            return exactMatch;

        // Try SourceCategoryMapping (comma-split) match
        foreach (var category in categoriesList)
        {
            if (string.IsNullOrWhiteSpace(category.SourceCategoryMapping))
                continue;

            var mappings = category.SourceCategoryMapping.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var mapping in mappings)
            {
                var trimmed = mapping.Trim();
                if (!string.IsNullOrEmpty(trimmed) &&
                    description.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return category;
                }
            }
        }

        // Try AutoMatchPattern (regex) match
        foreach (var category in categoriesList)
        {
            if (string.IsNullOrWhiteSpace(category.AutoMatchPattern))
                continue;

            try
            {
                var regex = new Regex(category.AutoMatchPattern, RegexOptions.IgnoreCase);
                if (regex.IsMatch(description) || (counterparty != null && regex.IsMatch(counterparty)))
                {
                    return category;
                }
            }
            catch (RegexParseException)
            {
                // Invalid regex pattern, skip this category
                continue;
            }
        }

        return null;
    }

    public async Task<IEnumerable<Category>> GetByTypeAsync(TransactionType type)
    {
        return await _categoryRepository.GetByTypeAsync(type);
    }

    /// <summary>
    /// Reassigns all transactions from deleteId to targetId, then deletes the category.
    /// Returns the count of reassigned transactions.
    /// </summary>
    public async Task<int> ReassignAndDeleteAsync(long deleteId, long targetId)
    {
        var deleteCategory = await _categoryRepository.GetByIdAsync(deleteId);
        var targetCategory = await _categoryRepository.GetByIdAsync(targetId);

        if (deleteCategory == null)
            throw new InvalidOperationException($"Category with id {deleteId} not found");
        if (targetCategory == null)
            throw new InvalidOperationException($"Category with id {targetId} not found");

        if (deleteCategory.Type != targetCategory.Type)
            throw new InvalidOperationException("Cannot reassign transactions between categories of different types");

        // Reassign all transactions from deleteId to targetId
        var transactionsToReassign = await _context.Transactions
            .Where(t => t.CategoryId == deleteId)
            .ToListAsync();

        foreach (var transaction in transactionsToReassign)
        {
            transaction.CategoryId = targetId;
            transaction.Category = targetCategory;
            transaction.CategoryName = targetCategory.Name;
        }

        await _context.SaveChangesAsync();

        // Delete the category
        await _categoryRepository.DeleteAsync(deleteId);

        return transactionsToReassign.Count;
    }

    private static void ValidateCategory(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
            throw new InvalidOperationException("Category name cannot be empty");

        if (category.Name.Length > 20)
            throw new InvalidOperationException("Category name cannot exceed 20 characters");

        if (category.Icon != null && category.Icon.Length > 10)
            throw new InvalidOperationException("Category icon cannot exceed 10 characters");
    }
}