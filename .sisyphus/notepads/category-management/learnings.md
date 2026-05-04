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

---

## EmojiPickerDialog Creation (2026-05-04)

### Files Created
1. `src/Bookkeeping.App/Views/EmojiPickerDialog.axaml` - Modal window with category tabs and emoji grid
2. `src/Bookkeeping.App/Views/EmojiPickerDialog.axaml.cs` - Code-behind with ShowDialog helper

### Dialog Design
- Title: "选择图标"
- Size: 420x480, non-resizable, modal
- WindowStartupLocation: CenterOwner
- Layout: Category TabControl at top, WrapPanel emoji grid in ScrollViewer, Cancel button at bottom

### Key Implementation Details

#### TabControl Categories
- Populated from `EmojiData.Categories` in Loaded event
- SelectionChanged triggers LoadEmojisForCategory

#### Emoji Grid
- ItemsControl with WrapPanel (Orientation="Horizontal")
- Button per emoji with Tag bound to Emoji string
- ToolTip.Tip bound to DisplayName
- Click="EmojiClick" handler in code-behind

#### Dialog Return Pattern
- `ShowDialog(Window parent, string? currentEmoji)` static method
- Sets _selectedEmoji and calls Close(_selectedEmoji)
- Returns null if Cancel clicked or dialog closed without selection

#### EmojiClick Handler
- Gets Tag from Button, sets _selectedEmoji, Close(_selectedEmoji)
- CancelClick calls Close(null)

### Style Notes
- EmojiButton style: transparent bg, border, cornerRadius=6, min 50x50
- Hover: background changes, border color changes to PrimaryLight
- Selected state (not actively used): PrimaryLight bg with Primary border

### Build Status
- EmojiPickerDialog files pass diagnostics (0 errors)
- Pre-existing build errors in CategoryEditViewModel.cs (CategoryItemViewModel not found) are unrelated

---

## DeleteReassignDialog Implementation (2026-05-04)

### Files Created
1. `src/Bookkeeping.App/ViewModels/DeleteReassignViewModel.cs`
2. `src/Bookkeeping.App/Views/DeleteReassignDialog.axaml`
3. `src/Bookkeeping.App/Views/DeleteReassignDialog.axaml.cs`

### DeleteReassignViewModel Properties
- `string CategoryName` - name of category being deleted
- `int TransactionCount` - number of transactions to reassign
- `ObservableCollection<CategoriesViewModel.CategoryItemViewModel> AvailableTargets` - same-type categories excluding deleted
- `CategoriesViewModel.CategoryItemViewModel? SelectedTarget` - chosen target
- `string? ErrorMessage` - error display
- `bool IsLoading` - loading state
- `bool CanConfirm` - computed: true only when target selected and not the deleted category

### DeleteReassignViewModel Commands
- `ConfirmAsync(Window)` - calls CategoryService.ReassignAndDeleteAsync, closes with success
- `Cancel(Window)` - closes dialog without changes

### Key Implementation Notes

#### CategoryItemViewModel Reference
- DeleteReassignViewModel is in same namespace as CategoriesViewModel
- Must use fully-qualified `CategoriesViewModel.CategoryItemViewModel` since it's a nested type
- The same pattern applies for x:DataType in XAML: `vm:CategoriesViewModel+CategoryItemViewModel`

#### Dialog Pattern
- Uses `ShowDialog<bool>(parent)` returning bool for success/failure
- Commands receive Window as CommandParameter via `{Binding $parent[Window]}`
- Window passed to CloseDialog() which calls `ownerWindow.Close(success)`

#### Service Access
- Access `CategoryService` via cast: `(CategoryService)App.Services.GetRequiredService<ICategoryService>()`
- This is needed because `ReassignAndDeleteAsync` is not on `ICategoryService` interface

#### Pre-existing Build Errors (NOT from this task)
- EmojiPickerDialog.axaml.cs: IReadOnlyList<> missing using, WindowBase vs Window issues
- CategoryEditDialog.axaml.cs: WindowBase vs Window conversion issue at line 75

### XAML Patterns
- Window with SizeToContent="Height" for auto-height dialogs
- Card Border with Padding="24" for content container
- Run elements for mixed binding text with string interpolation
- ComboBox ItemTemplate with DataTemplate for dropdown items

---

## CategoryEditDialog Implementation (2026-05-05)

### Files Created
1. `src/Bookkeeping.App/ViewModels/CategoryEditViewModel.cs`
2. `src/Bookkeeping.App/Views/CategoryEditDialog.axaml`
3. `src/Bookkeeping.App/Views/CategoryEditDialog.axaml.cs`

### CategoryEditViewModel Properties
- `string Name` - category name (max 20 chars)
- `TransactionType Type` - category type (locked for edit mode)
- `string? SelectedIcon` - selected emoji
- `bool IsEditMode` - true=edit, false=add
- `long? EditingCategoryId` - for edit mode
- `int SortOrder` - read-only in edit mode
- `string? ErrorMessage` - validation errors
- `bool DialogResult` - set to true when confirmed

### CategoryEditViewModel Methods
- `InitializeForAdd(TransactionType defaultType)` - sets up for new category
- `InitializeForEdit(CategoryItemViewModel category)` - populates fields for editing

### CategoryEditViewModel Commands
- `ConfirmCommand` - validates (non-empty, no duplicate name within type), sets DialogResult=true
- `CancelCommand` - sets DialogResult=false

### CategoryEditDialog Features
- Window (not UserControl) - opens as modal dialog
- Title: "添加分类" or "编辑分类" based on IsEditMode
- Name TextBox with MaxLength=20 and PlaceholderText
- Type RadioButtons (支出/收入) - IsEnabled only in add mode
- Icon display with "选择图标" button to open EmojiPickerDialog
- SortOrder display (read-only, visible only in edit mode)
- Error message TextBlock (red, visible when ErrorMessage not null)
- Confirm/Cancel buttons at bottom right

### Key Implementation Notes

#### Dialog Show Pattern
```csharp
public static CategoryEditViewModel? ShowDialog(Window parent, CategoryEditViewModel vm)
{
    // Creates dialog, sets DataContext, shows modal, returns vm if confirmed
}

public static CategoryEditViewModel? ShowDialog(Window parent, CategoriesViewModel.CategoryItemViewModel category)
{
    // Creates new vm, initializes for edit, shows modal, returns vm if confirmed
}
```

#### Owner Casting for EmojiPickerDialog
- EmojiPickerDialog.ShowDialog requires `Window` not `WindowBase`
- Must cast: `Owner is not Window ownerWindow` check before calling
- Fixed error: `cannot convert from 'Avalonia.Controls.WindowBase' to 'Avalonia.Controls.Window'`

#### Title Update Pattern
- Window Title property set in XAML to "添加分类" (default)
- Code-behind `UpdateTitle()` method updates both `Title` and `TitleText.Text`
- Called in OnDataContextChanged and after InitializeForAdd/Edit

### Integration with CategoriesViewModel
- CategoriesViewModel already has IsEditing, SelectedCategory, EditingName, EditingIcon
- Task requirement: dialog should be shown via Window from CategoriesView
- Use `App.Services.GetRequiredService<ICategoryService>()` for validation

### Build Status
- CategoryEditDialog files: 0 errors in LSP diagnostics
- Pre-existing build error in EmojiPickerDialog.axaml.cs (IReadOnlyList<> not found)
- This is a pre-existing issue, not caused by CategoryEditDialog