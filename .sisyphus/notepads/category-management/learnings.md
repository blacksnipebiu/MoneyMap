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

---

## StatisticsView Implementation (2026-05-04)

### Files Created
1. `src/Bookkeeping.App/ViewModels/StatisticsViewModel.cs`
2. `src/Bookkeeping.App/Views/StatisticsView.axaml`
3. `src/Bookkeeping.App/Views/StatisticsView.axaml.cs`

### ViewModel Properties
- `ObservableCollection<CategorySummaryDto> CategorySummaries`
- `ObservableCollection<TopCategoryDto> TopCategories`
- `ObservableCollection<MonthAmountDto> TrendData`
- `TransactionType SelectedType` (default: Expense)
- `DateTime SelectedMonth` (default: first day of current month)
- `long? SelectedCategoryId`
- `string? SelectedCategoryName`
- `CategorySummaryDto? SelectedCategorySummary` (for ComboBox binding)
- `bool IsLoading`, `string? ErrorMessage`, `int SelectedTabIndex`
- Chart series arrays: `PieChartSeries`, `TrendChartSeries`, `TopNChartSeries`
- Axis arrays: `TrendXAxes`, `TrendYAxes`, `TopNXAxes`, `TopNYAxes`

### Commands
- `LoadSummaryCommand` → `GetCategorySummaryAsync(month, type)`
- `LoadTopCategoriesCommand` → `GetTopCategoriesAsync(type, 5, month)`
- `LoadTrendCommand` → `GetCategoryTrendAsync(categoryId, 6)`
- `SwitchTypeCommand(type)` → toggles Income/Expense
- `ChangeMonthCommand(offset)` → ±1 month navigation
- `LoadAllDataAsync()` → parallel Task.WhenAll for summary + topN

### LiveCharts2 Integration
- Package already present: `LiveChartsCore.SkiaSharpView.Avalonia` version 2.0.0-rc5.4
- Namespace: `xmlns:lvc="using:LiveChartsCore.SkiaSharpView.Avalonia"`
- Chart control: `<lvc:CartesianChart>` for all chart types (pie, line, bar)
- PieChart uses `PieSeries<decimal>` with colors array
- LineChart uses `LineSeries<decimal>` with GeometrySize and stroke
- BarChart uses `ColumnSeries<decimal>` with MaxBarWidth
- Axis configuration with LabelsPaint and SeparatorsPaint using SKColor/SKColors

### XAML Patterns
- `x:Static enums:TransactionType.Expense` requires `xmlns:enums="using:Bookkeeping.Core.Enums"`
- Empty state uses `IsVisible="{Binding !CollectionName.Count}"` pattern
- Chart visibility: `IsVisible="{Binding !!CollectionName.Count}"`

### Pre-existing Build Errors (NOT from this task)
- CategoriesView.axaml has errors: `EnumConverters` type not found, `TransactionType` resolution issues
- These errors existed before StatisticsView was created (CategoriesView was never modified)

---

## CategoriesViewModel Rebuild (2026-05-04)

### Files Modified
1. `src/Bookkeeping.App/ViewModels/CategoriesViewModel.cs` - Complete rebuild with full CRUD
2. `src/Bookkeeping.App/Views/CategoriesView.axaml` - Updated bindings to match new ViewModel
3. `src/Bookkeeping.Data/ServiceCollectionExtensions.cs` - Added ICategoryService registration
4. `src/Bookkeeping.App/Converters/CommonConverters.cs` - Created EnumToBoolConverter, NotNullToBoolConverter, NullToBoolConverter
5. `src/Bookkeeping.App/App.axaml` - Registered new converters

### ViewModel Structure

#### Properties
- `ObservableCollection<CategoryItemViewModel> IncomeCategories` - Grouped income categories
- `ObservableCollection<CategoryItemViewModel> ExpenseCategories` - Grouped expense categories
- `CategoryItemViewModel? SelectedCategory` - Current selection
- `string NewCategoryName` - Add form
- `TransactionType NewCategoryType` - Add form (default Expense)
- `string? SelectedIcon` - Add form emoji
- `bool IsLoading`, `string? ErrorMessage`, `bool IsEditing` - State
- `string EditingName`, `string? EditingIcon` - Edit form
- `bool ShowDeleteReassignDialog`, `CategoryItemViewModel? CategoryToDelete`, `ReassignTargetCategory` - Delete dialog state

#### Nested CategoryItemViewModel
- Properties: Id, Name, Icon, Type, SortOrder, TransactionCount, IsSelected
- Computed: DisplayName (icon + name)

#### Commands
- `LoadCategoriesAsync` - Loads all categories, splits by type into collections
- `AddCategoryAsync` - Validates, calls CreateAsync, adds to correct collection
- `EditCategory(CategoryItemViewModel)` - Sets SelectedCategory, enters edit mode
- `UpdateCategoryAsync` - Saves changes via UpdateAsync
- `DeleteCategoryAsync(CategoryItemViewModel)` - Checks transactions, shows dialog if needed
- `ConfirmDeleteWithReassignAsync` - Calls CategoryService.ReassignAndDeleteAsync (cast from interface)
- `CloseDeleteReassignDialog` - Resets dialog state
- `CancelEdit` - Resets edit state
- `MoveUpAsync` / `MoveDownAsync` - Swaps SortOrder between adjacent items

### Key Implementation Notes

#### DI Registration
- Added `services.AddScoped<ICategoryService, CategoryService>()` to Bookkeeping.Data ServiceCollectionExtensions
- This was missing before - CategoryService existed but wasn't registered

#### Service Access Pattern
- All commands access ICategoryService via `App.Services.GetRequiredService<ICategoryService>()`
- For ReassignAndDeleteAsync (not on interface), cast to concrete CategoryService:
  ```csharp
  var categoryService = (CategoryService)App.Services.GetRequiredService<ICategoryService>();
  ```

#### TransactionCount
- Category.Transactions collection may not be loaded (no Include in GetAllAsync)
- Set to 0 or use Transactions?.Count - relies on EF lazy loading or manual refresh
- For accurate count after reassignment, manually update the target CategoryItemViewModel.TransactionCount

#### RadioButton Binding
- Used `EnumToBoolConverter` to bind RadioButtons to TransactionType enum
- Converter checks `value.ToString() == parameter.ToString()`

### XAML Patterns Used
- ItemsControl with DataTemplate for category lists
- Command binding via `$parent[ItemsControl].((vm:CategoriesViewModel)DataContext)` pattern
- Dialog overlay using Grid.ColumnSpan="2" with Background="#80000000"
- PlaceholderText instead of deprecated Watermark

### Build Issues Fixed
1. Multiple root elements in UserControl → wrapped all content in single Grid
2. Watermark deprecation → changed to PlaceholderText
3. Missing converter registrations → added to App.axaml Resources
4. Missing ICategoryService registration → added to ServiceCollectionExtensions