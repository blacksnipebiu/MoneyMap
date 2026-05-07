# MoneyMap - 跨平台桌面记账应用

**Generated:** 2026-05-06 | **Branch:** master | **Stack:** Avalonia 12 + EF Core SQLite + CommunityToolkit.Mvvm

## 项目结构

```
src/
├── MoneyMap.Core/       # 领域模型、枚举、仓储接口（无外部依赖）
├── MoneyMap.Data/       # EF Core + SQLite 数据层
├── MoneyMap.Import/     # 账单解析（支付宝CSV、微信Excel）
├── MoneyMap.Utils/      # 工具类
└── MoneyMap.App/        # Avalonia UI 桌面应用（入口）
tests/Bookkeeping.Data.Tests/  # xUnit 测试
```

**依赖方向**: `Utils` ← `Core` ← `Data` ← `App` | `Import` ← `App`

## 构建命令

```bash
dotnet build src/MoneyMap.App/MoneyMap.App.csproj -c Release
dotnet run --project src/MoneyMap.App/MoneyMap.App.csproj
dotnet test   # xUnit + EF Core InMemory
```

## WHERE TO LOOK

| 任务 | 位置 | 说明 |
|------|------|------|
| **应用启动** | `App.axaml.cs` → `OnFrameworkInitializationCompleted()` | DI、DB 初始化、MainWindow 创建 |
| **页面导航** | `MainWindowViewModel.cs` | ViewModel 缓存 + `NavigateTo()` 路由 |
| **视图定位** | `ViewLocator.cs` | 命名约定 `XxxViewModel` → `XxxView`（字符串替换） |
| **数据库** | `BookkeepingDbContext.cs` | 5 个 DbSet，Code First 无迁移，`EnsureCreatedAsync()` |
| **账单解析** | `MoneyMap.Import/Parsers/` | 支付宝 CSV (CsvHelper) / 微信 Excel (MiniExcel) |
| **智能分类** | `AlipayParser.cs` 的 `DetectTransferOrIncome()` | 3 层分类：方向 → 理财收益 → 转账覆盖 |
| **导入流程** | `ImportViewModel.cs` (2000+ 行) | 检测→解析→预览→映射弹窗→确认→入库 |
| **分类自动匹配** | `CategoryService.cs` | 3 层：精确名→SourceCategoryMapping→AutoMatchPattern 正则 |
| **DI 注册** | `ServiceCollectionExtensions.cs` (Data/Import) | 扩展方法隔离层注册 |
| **Toast 通知** | `App.ToastService` | 全局静态访问，自动消失动画 |

## 关键文件

| 文件 | 作用 |
|------|------|
| `App.axaml.cs` | DI 配置、DB 初始化、`App.Services`/`App.ToastService` 静态暴露 |
| `MainWindowViewModel.cs` | 8 个页面 ViewModel 缓存、导航路由 |
| `ImportViewModel.cs` | 最大文件（2000+行），导入全流程 + 筛选 + 映射 |
| `ImportOrchestrationService.cs` | 导入管道：验证→去重→保存→ImportRecord |
| `Transaction.cs` | `AccountId` 和 `ImportRecordId` 是非空外键 |
| `Category.cs` | `SourceCategoryMapping`(逗号分隔) + `AutoMatchPattern`(正则) 自动分类 |
| `MappingModels.cs` | 映射 UI 模型：分类/支付方式/字段映射 |
| `FilterOption<T>` (ImportViewModel.cs) | 多选筛选组件 |

## CONVENTIONS

- **MVVM**: `CommunityToolkit.Mvvm` — `[ObservableProperty]` 生成属性（`_camelCase` → `PascalCase`），`[RelayCommand]` 生成命令
- **partial class**: 所有 ViewModel 和 View 必须 `partial`（源码生成器要求）
- **异步命令**: 命令方法名**不带** `Async` 后缀（`SelectFileAsync()` → `SelectFileCommand`）
- **DB 访问**: 每次操作 `using var scope = _scopeFactory.CreateScope()` 创建作用域
- **服务定位器**: `App.Services.GetRequiredService<T>()` 和 `App.ToastService.ShowError()` 全局静态访问
- **Avalonia 样式**: `Styles/` 分层（Colors → Typography → Controls），`{StaticResource Conv}` 引用转换器
- **无 .editorconfig** — 依赖 Rider/VS 默认格式

## ANTI-PATTERNS (THIS PROJECT)

- **async void** 仅限事件处理器和生命周期回调（`OnBecameCurrent()`）
- **ImportRecord.HttpPath** 空路径 — 导入时必须设置 `FilePath`
- `Transaction.AccountId` 和 `ImportRecordId` 是**非空** FK — 入库前必须赋值
- **不使用迁移** — Code First `EnsureCreatedAsync()` 仅适用开发；生产需手动处理 schema 变更
- DB 重置需 `SqliteConnection.ClearAllPools()` 释放连接池

## CONVERTERS

在 `App.axaml` 中注册为 `{StaticResource XxxConverter}`：

| Converter | 用途 |
|-----------|------|
| `TransactionTypeBrushConverter` | 类型 → 画刷 |
| `TransactionTypeNameConverter` | 类型 → 中文（支出/收入/转账） |
| `TransactionTypeColorConverter` | 类型 → 颜色字符串 |
| `TransactionTypeAmountColorConverter` | 类型 → 金额颜色 |
| `TransactionStatusNameConverter` | 状态 → 中文（已完成/已退款/待处理） |
| `DataSourceIconConverter` | 数据源 → 图标字符 |
| `DataSourceNameConverter` | 数据源 → 中文（支付宝/微信支付） |

## NOTES

- **Avalonia 12** DataGrid 需单独引用包 `Avalonia.Controls.DataGrid v12.0.0`
- 拖拽文件: `DataFormat.File` + `e.DataTransfer.TryGetFiles()`
- 文件对话框: `TopLevel.StorageProvider.OpenFilePickerAsync()`
- DB 路径: `%LocalAppData%/MoneyMap/moneymap.db`
- 种子数据: 20 个默认分类（9 支出/6 收入/3 转账/2 其他）+ 17 个转换器
- 微信账单使用 `MiniExcel` 按列索引解析（非列名），注意列顺序
- 支付宝 CSV 编码检测: UTF-8 → GBK 回退
- 理财智能分类: 余额宝/零钱通 + 收益 → Income，蚂蚁财富 + 买入 → Transfer
- 存在一个测试项目但引用已过时（`Bookkeeping.Data.Tests` 引用已更名的项目）
- 预览表格使用 **ListBox + 自定义列 Grid** 实现，不使用 DataGrid（即使已引用包）
