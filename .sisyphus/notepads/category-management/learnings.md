# Category Management - QA Learnings

## QA Evidence Summary

### 1. Build Verification
- **Result**: PASS
- **Command**: `dotnet build src/Bookkeeping.App/Bookkeeping.App.csproj -c Release`
- **Output**: `ok dotnet build: 4 projects, 0 errors, 0 warnings (00:00:02.37)`

### 2. Unit Tests
- **Result**: PASS (14/14 tests)
- **Command**: `dotnet test tests/Bookkeeping.Data.Tests/ -c Debug --no-build`
- **Tests covered**:
  - CategoryService: Create, Update, Delete, ReassignAndDelete, AutoCategorize, GetByType
  - StatisticsService: GetCategorySummary, GetCategoryTrend, GetTopCategories

### 3. Seed Data Verification
- **Result**: PASS
- **SourceCategoryMapping populated** for all 14 seed categories:
  - Expense (9): 餐饮美食, 交通出行, 购物消费, 生活服务, 休闲娱乐, 医疗健康, 教育培训, 投资理财, 其他支出
  - Income (5): 工资收入, 奖金收入, 投资收益, 红包收入, 退款收入, 其他收入
- **投资理财 Type corrected**: Changed from Transfer(3) to Expense(2)
- **AutoMatchPattern**: All non-fallback categories have regex patterns

### 4. Service Implementation
- **CategoryService**: Full CRUD with validation (duplicate name check, last-category protection)
- **StatisticsService**: Summary, Trend, TopN queries with DTO projections

### 5. UI Components Implemented
- **CategoriesView**: Full CRUD with move up/down, edit, delete
- **StatisticsView**: 3 tabs (Summary piechart, Trend linechart, TopN barchart) with LiveCharts2
- **Dialogs**: CategoryEditDialog, EmojiPickerDialog, DeleteReassignDialog

### 6. DI Registration
- Services registered in App.axaml.cs: ICategoryService, IStatisticsService
- Repositories registered in ServiceCollectionExtensions: ICategoryRepository

### QA Limitations
- Could not run interactive UI test (dev-browser skill not accessible)
- Build warnings: CS8622 nullability mismatches in EmojiPickerDialog
- Build warnings: file lock issues when app was already running

### Edge Cases Tested (via unit tests)
- Empty database: GetCategorySummary returns empty list (no exception)
- Duplicate name: Throws InvalidOperationException
- Delete last category of type: Throws InvalidOperationException
- Invalid regex pattern: Skipped gracefully in AutoCategorize

## Date
2026-05-05