# Category Management Learnings

## Updates (2026-05-04)

### Seed Data Update
- Added `SourceCategoryMapping` and `AutoMatchPattern` to all 14 default categories
- Fixed "投资理财" Type from `TransactionType.Transfer` → `TransactionType.Expense`
- Preserved existing SortOrder values for all categories

### SourceCategoryMapping Pattern
- Comma-separated source category names from external data sources (Alipay/WeChat)
- Examples: "餐饮美食,美食,餐饮,外卖,快餐,零食"

### AutoMatchPattern Pattern
- Regex pattern with pipe-separated alternatives
- Examples: "餐饮|美食|外卖|快餐"
- null for catch-all categories (其他支出, 其他收入)

### Category Type Convention
- Expense categories: TransactionType.Expense
- Income categories: TransactionType.Income
- Transfer type seems inappropriate for a category (should only be Expense/Income)

## StatisticsService Implementation (2026-05-04)

### Pattern: Direct DbContext Injection
- Statistics queries use direct `BookkeepingDbContext` injection instead of repositories
- Reason: Complex aggregations requiring GroupBy with multiple aggregates are cleaner with direct queries

### Month Filtering Pattern
- Start date: `new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc)`
- End date: `startDate.AddMonths(1).AddTicks(-1)` — last tick of the month
- Alternative: `startDate.AddMonths(1).AddSeconds(-1)` works similarly

### Percentage Calculation
- Calculate total per group, then divide each item by total
- Use `Math.Round(value, 2)` for 2 decimal precision
- Guard against division by zero: `totalAmount > 0 ? ... : 0`

### Empty Data Handling
- Return `Enumerable.Empty<T>()` for no results
- Trend data fills all months with 0, never omits months

### Null CategoryId Handling
- Transactions without a category have `CategoryId = null`
- Display as "未分类" (uncategorized) in summaries

## Emoji Data Set Creation (2026-05-04)

### Files Created
1. `src/Bookkeeping.App/Models/EmojiItem.cs` - Immutable model with Emoji, DisplayName, Category properties
2. `src/Bookkeeping.App/Models/EmojiData.cs` - Static class with 153 emojis across 10 categories
3. `src/Bookkeeping.App/Controls/EmojiPickerButton.axaml` - Basic UserControl with Border/Card styling
4. `src/Bookkeeping.App/Controls/EmojiPickerButton.axaml.cs` - StyledProperty-based Emoji binding

### EmojiData Structure
- 10 categories: 餐饮(16), 交通(18), 购物(16), 金融(15), 生活(17), 娱乐(16), 健康(14), 教育(13), 常用(16), 运动(14), 旅游(15)
- Total: 153 emojis
- Static constructor pattern for initialization
- `_categoryMap` Dictionary for O(1) category lookup
- `GetByCategory(string)` returns filtered list or empty

### EmojiPickerButton Design
- Uses Avalonia StyledProperty for Emoji string binding
- XAML binds TextBlock.Text directly to Emoji property via `{Binding}`
- Card classes applied via Border for consistent styling
- MinWidth=60, MinHeight=40 for reasonable click target

### UI Pattern Observed
- Existing controls use `x:DataType` with specific types (e.g., `services:ToastItem`)
- Code-behind pattern: `InitializeComponent()` in constructor + event handlers
- ToastControl shows pattern: DataContextChanged handler for binding-based property access

### Pre-existing Build Errors
- Bookkeeping.Data has missing interfaces (IStatisticsService, ICategoryService) - not related to emoji work
- Emoji files pass LSP diagnostics

## Build Fixes Applied (2026-05-04)

### EmojiData.cs Fixes
- Added `using System.Linq;` for GroupBy and ToList
- Added explicit cast `(IReadOnlyList<EmojiItem>)` in Dictionary initialization to resolve covariance issue

### EmojiPickerButton.axaml.cs Fixes
- Changed `using Avalonia.Media;` to `using Avalonia;` — StyledProperty lives in Avalonia namespace, not Avalonia.Media
- AvaloniaProperty.Register<TOwner, TValue> works with just `using Avalonia;`

### Build Result
- 4 projects, 0 errors, 1 warning (pre-existing Watermark → PlaceholderText deprecation in unrelated code)

---

## CategoryService Implementation (2026-05-04)

### Location
- `src/Bookkeeping.Data/Services/CategoryService.cs`

### Architecture
- Inject `ICategoryRepository` and `BookkeepingDbContext` via constructor
- Use `_context.Transactions` directly for reassignment operations (not via repository)
- Repository pattern for basic CRUD; service adds business logic

### Key Implementation Details

#### Validation Rules
- Name: cannot be empty, max 20 chars
- Icon: max 10 chars (optional)
- Duplicate check: same Type + same Name (case-insensitive)

#### CreateAsync
- Validates category before creation
- Checks for duplicates within same type
- Auto-increments SortOrder: finds max SortOrder for type + 1

#### UpdateAsync
- Validates category
- Checks duplicate name excluding self (by Id)
- Uses repository's UpdateAsync

#### DeleteAsync
- First checks if category exists
- Checks if this is the LAST category of its type (Count <= 1) → throws exception
- Cannot delete last category of any type

#### ReassignAndDeleteAsync
- Validates both categories exist
- Checks same Type before reassignment (throws if different types)
- Uses `_context.Transactions.Where(t => t.CategoryId == deleteId)` for bulk update
- Sets transaction CategoryId and Category navigation property
- Calls `_context.SaveChangesAsync()` then `_categoryRepository.DeleteAsync(deleteId)`
- Returns count of reassigned transactions

#### AutoCategorizeAsync
- Priority order: exact name match → SourceCategoryMapping (comma-split) → AutoMatchPattern (regex)
- Case-insensitive matching throughout
- Regex errors are caught and skipped (invalid patterns don't break categorization)
- Returns null if no match found

### Build Issues Encountered
- Initial build failed due to cached obj files; resolved by `dotnet clean` then rebuild
- Both CategoryService and StatisticsService had same issue (ICategoryService/IStatisticsService not found)
- Clean rebuild resolved the issue - was likely stale assembly reference cache